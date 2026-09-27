using System.Security.Claims;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api/bucket-list")]
[Authorize]
public class BucketListController(AppDbContext db) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim is missing.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BucketListEntryDto>>> GetAll()
    {
        var userId = CurrentUserId;

        var entries = await db.BucketListEntries
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Peak).ThenInclude(p => p.ElevationOverride)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BucketListEntryDto(
                b.PeakId,
                b.Peak.Name,
                b.Peak.Latitude,
                b.Peak.Longitude,
                b.Peak.ElevationOverride != null ? b.Peak.ElevationOverride.ElevationMeters : b.Peak.ElevationMeters,
                b.Peak.CountryCode))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<BucketListEntryDto>> Create(CreateBucketListEntryRequest request)
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

        var alreadyExists = await db.BucketListEntries
            .AnyAsync(b => b.UserId == userId && b.PeakId == request.PeakId);
        if (alreadyExists)
        {
            return Conflict("Peak is already on the bucket list.");
        }

        var entry = new BucketListEntry
        {
            UserId = userId,
            PeakId = request.PeakId,
            CreatedAt = DateTime.UtcNow
        };

        db.BucketListEntries.Add(entry);
        await db.SaveChangesAsync();

        var dto = new BucketListEntryDto(
            peak.Id, peak.Name, peak.Latitude, peak.Longitude,
            peak.ElevationOverride?.ElevationMeters ?? peak.ElevationMeters, peak.CountryCode);

        return CreatedAtAction(nameof(GetAll), dto);
    }

    [HttpDelete("{peakId:int}")]
    public async Task<IActionResult> Delete(int peakId)
    {
        var userId = CurrentUserId;

        var entry = await db.BucketListEntries
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PeakId == peakId);

        if (entry is null)
        {
            return NotFound();
        }

        db.BucketListEntries.Remove(entry);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
