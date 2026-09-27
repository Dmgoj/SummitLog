using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api/peaks")]
public class PeaksController(AppDbContext db, IMemoryCache cache) : ControllerBase
{
    private const string CountriesCacheKey = "peaks:countries";

    [HttpGet("search")]
    public async Task<ActionResult<PeakSearchResultDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? country,
        [FromQuery] int? minElevation,
        [FromQuery] int? maxElevation,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var hasNameQuery = !string.IsNullOrWhiteSpace(q);
        var hasFilter = !string.IsNullOrWhiteSpace(country) || minElevation.HasValue || maxElevation.HasValue;

        if (!hasNameQuery && !hasFilter)
        {
            return BadRequest("Provide a search query, a country, or an elevation filter.");
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Peaks.AsNoTracking().Include(p => p.ElevationOverride).AsQueryable();

        if (hasNameQuery)
        {
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{q}%"));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(p => p.CountryCode == country);
        }

        if (minElevation.HasValue)
        {
            query = query.Where(p =>
                (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters) != null &&
                (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters) >= minElevation.Value);
        }

        if (maxElevation.HasValue)
        {
            query = query.Where(p =>
                (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters) != null &&
                (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters) < maxElevation.Value);
        }

        var totalCount = await query.CountAsync();

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy.ToLowerInvariant() switch
        {
            "elevation" => descending
                ? query.OrderByDescending(p => (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters).HasValue)
                    .ThenByDescending(p => p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters)
                : query.OrderByDescending(p => (p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters).HasValue)
                    .ThenBy(p => p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters),
            _ => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PeakSummaryDto(
                p.Id,
                p.Name,
                p.CountryCode,
                p.ElevationOverride != null ? p.ElevationOverride.ElevationMeters : p.ElevationMeters))
            .ToListAsync();

        return Ok(new PeakSearchResultDto(items, page, pageSize, totalCount));
    }

    [HttpGet("countries")]
    public async Task<ActionResult<IReadOnlyList<CountryOptionDto>>> GetCountries()
    {
        if (cache.TryGetValue(CountriesCacheKey, out List<CountryOptionDto>? cached))
        {
            return Ok(cached);
        }

        var codes = await db.Peaks
            .AsNoTracking()
            .Where(p => p.CountryCode != "")
            .Select(p => p.CountryCode)
            .Distinct()
            .ToListAsync();

        var countries = codes
            .Select(code => new CountryOptionDto(code, CountryNames.ByCode.GetValueOrDefault(code, code)))
            .OrderBy(c => c.Name)
            .ToList();

        // The peaks table only changes via a manual ingestion run, never at request time,
        // so a long cache duration is safe - a restart also clears it if ingestion ran meanwhile.
        cache.Set(CountriesCacheKey, countries, TimeSpan.FromHours(6));

        return Ok(countries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PeakDto>> GetById(int id)
    {
        var peak = await db.Peaks
            .AsNoTracking()
            .Include(p => p.ElevationOverride)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (peak is null)
        {
            return NotFound();
        }

        var effectiveElevation = peak.ElevationOverride?.ElevationMeters ?? peak.ElevationMeters;

        return Ok(new PeakDto(
            peak.Id,
            peak.GeoNameId,
            peak.Name,
            peak.AlternateNames,
            peak.Latitude,
            peak.Longitude,
            effectiveElevation,
            peak.ElevationOverride is not null,
            peak.ElevationOverride?.Source,
            peak.CountryCode,
            CountryNames.ByCode.GetValueOrDefault(peak.CountryCode, peak.CountryCode),
            peak.FeatureCode));
    }
}
