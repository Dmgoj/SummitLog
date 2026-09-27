using System.Net.Http.Headers;
using System.Text.Json;
using SummitLog.Api.Data;
using SummitLog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.GeoNamesIngest;

// Cross-references GeoNames mountain-range entries against Wikidata's P610 ("highest point")
// relation, so a range's stored elevation reflects its actual named summit rather than wherever
// GeoNames happened to pin a single point for the range (see CREDITS.md / project notes on the
// Makran Coast Range case). Source data is CC0 (Wikidata) - no scraping, no ToS concerns.
public static class WikidataElevationImporter
{
    private const string SparqlEndpoint = "https://query.wikidata.org/sparql";

    private const string Query = """
        SELECT ?geonamesId ?peakLabel ?elevation WHERE {
          ?range wdt:P610 ?peak ;
                 wdt:P1566 ?geonamesId .
          ?peak wdt:P2044 ?elevation .
          SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
        }
        """;

    public static async Task RunAsync(AppDbContext db)
    {
        Console.WriteLine("Querying Wikidata for range highest-point elevations...");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SummitLog", "1.0"));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sparql-results+json"));

        var url = $"{SparqlEndpoint}?query={Uri.EscapeDataString(Query)}";
        var json = await http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var bindings = doc.RootElement.GetProperty("results").GetProperty("bindings");

        var matches = new List<(long GeoNameId, int Elevation, string Label)>();

        foreach (var row in bindings.EnumerateArray())
        {
            if (!row.TryGetProperty("geonamesId", out var geonamesIdProp)) continue;
            if (!row.TryGetProperty("elevation", out var elevationProp)) continue;
            if (!long.TryParse(geonamesIdProp.GetProperty("value").GetString(), out var geoNameId)) continue;
            if (!double.TryParse(elevationProp.GetProperty("value").GetString(), out var elevationRaw)) continue;

            var label = row.TryGetProperty("peakLabel", out var labelProp)
                ? labelProp.GetProperty("value").GetString() ?? "unknown peak"
                : "unknown peak";

            matches.Add((geoNameId, (int)Math.Round(elevationRaw), label));
        }

        Console.WriteLine($"Wikidata returned {matches.Count:N0} range-to-summit matches.");

        var geoNameIds = matches.Select(m => m.GeoNameId).ToList();
        var peaksByGeoNameId = await db.Peaks
            .Where(p => geoNameIds.Contains(p.GeoNameId))
            .ToDictionaryAsync(p => p.GeoNameId);

        var existingOverrides = await db.ElevationOverrides
            .Where(e => peaksByGeoNameId.Values.Select(p => p.Id).Contains(e.PeakId))
            .ToDictionaryAsync(e => e.PeakId);

        var applied = 0;
        var skippedNoMatch = 0;
        var skippedLowerOrEqual = 0;
        var skippedAlreadyManual = 0;

        foreach (var (geoNameId, elevation, label) in matches)
        {
            if (!peaksByGeoNameId.TryGetValue(geoNameId, out var peak))
            {
                skippedNoMatch++;
                continue;
            }

            // Reject if the Wikidata "highest point" link produced a value that isn't actually
            // higher than what GeoNames already had - likely a bad link (e.g. to a pass, not a summit).
            if (peak.ElevationMeters.HasValue && elevation <= peak.ElevationMeters.Value)
            {
                skippedLowerOrEqual++;
                continue;
            }

            if (existingOverrides.TryGetValue(peak.Id, out var existing))
            {
                if (!existing.Source.StartsWith("Wikidata", StringComparison.OrdinalIgnoreCase))
                {
                    // Never clobber a hand-verified manual override with an automated one.
                    skippedAlreadyManual++;
                    continue;
                }

                existing.ElevationMeters = elevation;
                existing.Note = $"Highest point: {label}";
            }
            else
            {
                db.ElevationOverrides.Add(new ElevationOverride
                {
                    PeakId = peak.Id,
                    ElevationMeters = elevation,
                    Source = "Wikidata P610",
                    Note = $"Highest point: {label}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            applied++;
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"Done. Applied {applied:N0} overrides. " +
            $"Skipped: {skippedNoMatch:N0} no GeoNames match, {skippedLowerOrEqual:N0} not higher than existing value, " +
            $"{skippedAlreadyManual:N0} already have a manual override.");
    }
}
