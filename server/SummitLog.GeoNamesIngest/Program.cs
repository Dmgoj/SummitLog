using System.IO.Compression;
using SummitLog.Api.Data;
using SummitLog.Api.Models;
using SummitLog.GeoNamesIngest;
using Microsoft.EntityFrameworkCore;

const string DownloadUrl = "https://download.geonames.org/export/dump/allCountries.zip";
const int BatchSize = 2000;

var dataDir = GetArgValue(args, "--data-dir") ?? "./data";
var force = args.Contains("--force");
var connectionString = GetArgValue(args, "--connection-string")
    ?? Environment.GetEnvironmentVariable("SUMMITLOG_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "No connection string provided. Pass --connection-string \"...\" or set the SUMMITLOG_CONNECTION_STRING environment variable.");
var mode = GetArgValue(args, "--mode") ?? "geonames";

if (mode == "wikidata-elevations")
{
    var wikidataOptionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    wikidataOptionsBuilder.UseNpgsql(connectionString);
    await using var wikidataDb = new AppDbContext(wikidataOptionsBuilder.Options);
    await wikidataDb.Database.MigrateAsync();
    await WikidataElevationImporter.RunAsync(wikidataDb);
    return;
}

Directory.CreateDirectory(dataDir);
var zipPath = Path.Combine(dataDir, "allCountries.zip");

if (force || !File.Exists(zipPath))
{
    Console.WriteLine($"Downloading {DownloadUrl} ...");
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
    await using var httpStream = await http.GetStreamAsync(DownloadUrl);
    await using var fileStream = File.Create(zipPath);
    await httpStream.CopyToAsync(fileStream);
    Console.WriteLine("Download complete.");
}
else
{
    Console.WriteLine($"Using cached file: {zipPath} (pass --force to re-download)");
}

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString);

await using var db = new AppDbContext(optionsBuilder.Options);
await db.Database.MigrateAsync();

using var archive = ZipFile.OpenRead(zipPath);
var entry = archive.Entries.First(e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

await using var entryStream = entry.Open();
using var reader = new StreamReader(entryStream);

var batch = new List<GeoNamesRecord>(BatchSize);
long linesRead = 0;
long upserted = 0;
long inserted = 0;
long updated = 0;

while (await reader.ReadLineAsync() is { } line)
{
    linesRead++;

    var record = GeoNamesLineParser.TryParseMountainLine(line);
    if (record is not null)
    {
        batch.Add(record);
    }

    if (batch.Count >= BatchSize)
    {
        var (ins, upd) = await UpsertBatchAsync(db, batch);
        inserted += ins;
        updated += upd;
        upserted += batch.Count;
        batch.Clear();
    }

    if (linesRead % 500_000 == 0)
    {
        Console.WriteLine($"Processed {linesRead:N0} lines, upserted {upserted:N0} peaks so far...");
    }
}

if (batch.Count > 0)
{
    var (ins, upd) = await UpsertBatchAsync(db, batch);
    inserted += ins;
    updated += upd;
    upserted += batch.Count;
}

Console.WriteLine($"Done. Total lines read: {linesRead:N0}. Peaks upserted: {upserted:N0} (inserted: {inserted:N0}, updated: {updated:N0}).");

static string? GetArgValue(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static async Task<(int Inserted, int Updated)> UpsertBatchAsync(AppDbContext db, List<GeoNamesRecord> batch)
{
    var geoNameIds = batch.Select(r => r.GeoNameId).ToList();

    var existing = await db.Peaks
        .Where(p => geoNameIds.Contains(p.GeoNameId))
        .ToDictionaryAsync(p => p.GeoNameId);

    var inserted = 0;
    var updated = 0;
    var now = DateTime.UtcNow;

    await using var transaction = await db.Database.BeginTransactionAsync();

    foreach (var record in batch)
    {
        if (existing.TryGetValue(record.GeoNameId, out var peak))
        {
            if (peak.SourceModifiedAt >= record.ModifiedAt)
            {
                continue;
            }

            peak.Name = record.Name;
            peak.AlternateNames = record.AlternateNames;
            peak.Latitude = record.Latitude;
            peak.Longitude = record.Longitude;
            peak.ElevationMeters = record.ElevationMeters;
            peak.FeatureCode = record.FeatureCode;
            peak.CountryCode = record.CountryCode;
            peak.Timezone = record.Timezone;
            peak.SourceModifiedAt = record.ModifiedAt;
            peak.IngestedAt = now;
            updated++;
        }
        else
        {
            db.Peaks.Add(new Peak
            {
                GeoNameId = record.GeoNameId,
                Name = record.Name,
                AlternateNames = record.AlternateNames,
                Latitude = record.Latitude,
                Longitude = record.Longitude,
                ElevationMeters = record.ElevationMeters,
                FeatureCode = record.FeatureCode,
                CountryCode = record.CountryCode,
                Timezone = record.Timezone,
                SourceModifiedAt = record.ModifiedAt,
                IngestedAt = now
            });
            inserted++;
        }
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    db.ChangeTracker.Clear();

    return (inserted, updated);
}
