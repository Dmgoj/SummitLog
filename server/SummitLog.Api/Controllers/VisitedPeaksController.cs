using System.Security.Claims;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api/visited-peaks")]
[Authorize]
public class VisitedPeaksController(AppDbContext db) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim is missing.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VisitedPeakDto>>> GetAll()
    {
        var userId = CurrentUserId;

        var visited = await db.VisitedPeaks
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .Include(v => v.Peak).ThenInclude(p => p.ElevationOverride)
            .OrderByDescending(v => v.VisitedOn)
            .Select(v => new VisitedPeakDto(
                v.PeakId,
                v.Peak.Name,
                v.Peak.Latitude,
                v.Peak.Longitude,
                v.Peak.ElevationOverride != null ? v.Peak.ElevationOverride.ElevationMeters : v.Peak.ElevationMeters,
                v.Peak.CountryCode,
                v.VisitedOn,
                v.Notes))
            .ToListAsync();

        return Ok(visited);
    }

    [HttpPost]
    public async Task<ActionResult<VisitedPeakDto>> Create(CreateVisitedPeakRequest request)
    {
        var userId = CurrentUserId;

        var peak = await db.Peaks
            .AsNoTracking()
            .Include(p => p.ElevationOverride)
            .FirstOrDefaultAsync(p => p.Id == request.PeakId);
        if (peak is null)
        {
            return NotFound("Peak not found.");
        }

        var alreadyExists = await db.VisitedPeaks
            .AnyAsync(v => v.UserId == userId && v.PeakId == request.PeakId);
        if (alreadyExists)
        {
            return Conflict("Peak is already marked as visited.");
        }

        var visitedPeak = new VisitedPeak
        {
            UserId = userId,
            PeakId = request.PeakId,
            VisitedOn = request.VisitedOn,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        db.VisitedPeaks.Add(visitedPeak);

        var bucketListEntry = await db.BucketListEntries
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PeakId == request.PeakId);
        if (bucketListEntry is not null)
        {
            db.BucketListEntries.Remove(bucketListEntry);
        }

        await db.SaveChangesAsync();

        var dto = new VisitedPeakDto(
            peak.Id, peak.Name, peak.Latitude, peak.Longitude,
            peak.ElevationOverride?.ElevationMeters ?? peak.ElevationMeters,
            peak.CountryCode, visitedPeak.VisitedOn, visitedPeak.Notes);

        return CreatedAtAction(nameof(GetAll), dto);
    }

    [HttpPut("{peakId:int}")]
    public async Task<ActionResult<VisitedPeakDto>> Update(int peakId, UpdateVisitedPeakRequest request)
    {
        var userId = CurrentUserId;

        var visitedPeak = await db.VisitedPeaks
            .Include(v => v.Peak).ThenInclude(p => p.ElevationOverride)
            .FirstOrDefaultAsync(v => v.UserId == userId && v.PeakId == peakId);

        if (visitedPeak is null)
        {
            return NotFound();
        }

        visitedPeak.VisitedOn = request.VisitedOn;
        visitedPeak.Notes = request.Notes;
        await db.SaveChangesAsync();

        var dto = new VisitedPeakDto(
            visitedPeak.Peak.Id, visitedPeak.Peak.Name, visitedPeak.Peak.Latitude, visitedPeak.Peak.Longitude,
            visitedPeak.Peak.ElevationOverride?.ElevationMeters ?? visitedPeak.Peak.ElevationMeters,
            visitedPeak.Peak.CountryCode, visitedPeak.VisitedOn, visitedPeak.Notes);

        return Ok(dto);
    }

    [HttpDelete("{peakId:int}")]
    public async Task<IActionResult> Delete(int peakId)
    {
        var userId = CurrentUserId;

        var visitedPeak = await db.VisitedPeaks
            .FirstOrDefaultAsync(v => v.UserId == userId && v.PeakId == peakId);

        if (visitedPeak is null)
        {
            return NotFound();
        }

        db.VisitedPeaks.Remove(visitedPeak);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
