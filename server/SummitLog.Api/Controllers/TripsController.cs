using System.Security.Claims;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class TripsController(AppDbContext db, TripEmailService tripEmailService) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim is missing.");

    [HttpGet("peaks/{peakId:int}/interested")]
    public async Task<ActionResult<IReadOnlyList<InterestedHikerDto>>> GetInterestedHikers(int peakId)
    {
        var userId = CurrentUserId;

        var caller = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (caller is null)
        {
            return NotFound();
        }

        var interested = await db.BucketListEntries
            .AsNoTracking()
            .Where(b => b.PeakId == peakId && b.UserId != userId)
            .Include(b => b.User)
            .Select(b => b.User)
            .ToListAsync();

        var results = interested
            .Select(u => new InterestedHikerDto(
                u.Id,
                u.FirstName,
                u.LastName,
                caller.LastLatitude.HasValue && caller.LastLongitude.HasValue && u.LastLatitude.HasValue && u.LastLongitude.HasValue
                    ? GeoDistance.HaversineKm(caller.LastLatitude.Value, caller.LastLongitude.Value, u.LastLatitude.Value, u.LastLongitude.Value)
                    : (double?)null))
            .OrderBy(h => h.DistanceKm.HasValue ? 0 : 1)
            .ThenBy(h => h.DistanceKm)
            .ToList();

        return Ok(results);
    }

    [HttpPost("trips")]
    public async Task<ActionResult<TripDto>> CreateTrip(CreateTripRequest request)
    {
        var userId = CurrentUserId;

        var creator = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (creator is null)
        {
            return NotFound();
        }

        var peak = await db.Peaks.FirstOrDefaultAsync(p => p.Id == request.PeakId);
        if (peak is null)
        {
            return NotFound("Peak not found.");
        }

        var interestedUserIds = await db.BucketListEntries
            .Where(b => b.PeakId == request.PeakId && b.UserId != userId)
            .Select(b => b.UserId)
            .ToListAsync();

        var trip = new Trip
        {
            PeakId = request.PeakId,
            CreatorUserId = userId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        if (interestedUserIds.Count > 0)
        {
            var now = DateTime.UtcNow;
            var participants = interestedUserIds.Select(invitedUserId => new TripParticipant
            {
                TripId = trip.Id,
                UserId = invitedUserId,
                Status = TripParticipantStatus.Invited,
                InvitedAt = now
            }).ToList();
            db.TripParticipants.AddRange(participants);

            var message = $"{DisplayName(creator)} is organizing a trip to {peak.Name}.";
            var notifications = interestedUserIds.Select(invitedUserId => new Notification
            {
                UserId = invitedUserId,
                Type = NotificationType.TripInvite,
                TripId = trip.Id,
                Message = message,
                IsRead = false,
                CreatedAt = now
            }).ToList();
            db.Notifications.AddRange(notifications);

            await db.SaveChangesAsync();

            var invitees = await db.Users.Where(u => interestedUserIds.Contains(u.Id)).ToListAsync();
            foreach (var invitee in invitees)
            {
                await tripEmailService.SendTripInviteEmailAsync(invitee, creator, peak, trip);
            }
        }

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, await BuildTripDto(trip.Id, userId));
    }

    [HttpGet("trips/{id:int}")]
    public async Task<ActionResult<TripDto>> GetTrip(int id)
    {
        var dto = await BuildTripDto(id, CurrentUserId);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpPost("trips/{id:int}/respond")]
    public async Task<ActionResult<TripDto>> Respond(int id, RespondToTripRequest request)
    {
        var userId = CurrentUserId;

        if (!Enum.TryParse<TripParticipantStatus>(request.Status, ignoreCase: true, out var status)
            || status == TripParticipantStatus.Invited)
        {
            return BadRequest("Status must be 'Joined' or 'Declined'.");
        }

        var participant = await db.TripParticipants
            .FirstOrDefaultAsync(p => p.TripId == id && p.UserId == userId);
        if (participant is null)
        {
            return NotFound();
        }

        participant.Status = status;
        participant.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(await BuildTripDto(id, userId));
    }

    private static string DisplayName(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.FirstName) ? user.Email! : user.FirstName;

    private async Task<TripDto?> BuildTripDto(int tripId, string callerUserId)
    {
        var trip = await db.Trips
            .AsNoTracking()
            .Include(t => t.Peak)
            .Include(t => t.CreatorUser)
            .Include(t => t.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip is null)
        {
            return null;
        }

        var isCallerCreator = trip.CreatorUserId == callerUserId;
        var callerParticipant = trip.Participants.FirstOrDefault(p => p.UserId == callerUserId);
        var callerIsJoinedOrCreator = isCallerCreator || callerParticipant?.Status == TripParticipantStatus.Joined;

        var participantDtos = trip.Participants
            .Select(p => new TripParticipantDto(
                p.UserId,
                p.User.FirstName,
                p.User.LastName,
                p.Status.ToString(),
                callerIsJoinedOrCreator && p.Status == TripParticipantStatus.Joined ? p.User.Email : null))
            .ToList();

        return new TripDto(
            trip.Id,
            trip.PeakId,
            trip.Peak.Name,
            trip.CreatorUserId,
            trip.CreatorUser.FirstName,
            trip.CreatorUser.LastName,
            callerIsJoinedOrCreator ? trip.CreatorUser.Email : null,
            trip.Notes,
            trip.CreatedAt,
            isCallerCreator ? null : callerParticipant?.Status.ToString(),
            participantDtos);
    }
}
