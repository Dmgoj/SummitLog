using SummitLog.Api.Controllers;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace SummitLog.Api.Tests.Controllers;

public class TripsControllerTests
{
    private static TripsController CreateController(AppDbContext db, FakeEmailSender emailSender, string userId = "user-1")
    {
        var tripEmailService = new TripEmailService(emailSender, "http://localhost:5173");
        var controller = new TripsController(db, tripEmailService);
        controller.SetUser(userId);
        return controller;
    }

    private static Peak MakePeak(int id, string name = "Some Peak")
    {
        return new Peak
        {
            Id = id,
            GeoNameId = 1000 + id,
            Name = name,
            Latitude = 1,
            Longitude = 2,
            ElevationMeters = 1000,
            CountryCode = "US",
            FeatureCode = "MT",
            SourceModifiedAt = DateTime.UtcNow,
            IngestedAt = DateTime.UtcNow
        };
    }

    private static ApplicationUser MakeUser(string id, string email, double? lat = null, double? lon = null)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            LastLatitude = lat,
            LastLongitude = lon
        };
    }

    [Fact]
    public async Task GetMyTrips_ReturnsTripsCreatedOrInvitedTo_NewestFirst()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1, "Everest"));
        db.Peaks.Add(MakePeak(2, "Denali"));
        db.Peaks.Add(MakePeak(3, "K2"));
        db.Users.Add(MakeUser("user-1", "me@example.com"));
        db.Users.Add(MakeUser("user-2", "other@example.com"));

        var createdByMe = new Trip { PeakId = 1, CreatorUserId = "user-1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var invitedToMe = new Trip { PeakId = 2, CreatorUserId = "user-2", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var unrelated = new Trip { PeakId = 3, CreatorUserId = "user-2", CreatedAt = DateTime.UtcNow };
        db.Trips.AddRange(createdByMe, invitedToMe, unrelated);
        await db.SaveChangesAsync();

        db.TripParticipants.Add(new TripParticipant { TripId = invitedToMe.Id, UserId = "user-1", Status = TripParticipantStatus.Invited, InvitedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-1");

        var result = await controller.GetMyTrips();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var summaries = Assert.IsAssignableFrom<IReadOnlyList<TripSummaryDto>>(ok.Value);
        Assert.Equal(2, summaries.Count);
        Assert.Equal(invitedToMe.Id, summaries[0].Id);
        Assert.False(summaries[0].IsCreator);
        Assert.Equal("Invited", summaries[0].CallerStatus);
        Assert.Equal(createdByMe.Id, summaries[1].Id);
        Assert.True(summaries[1].IsCreator);
        Assert.Null(summaries[1].CallerStatus);
    }

    [Fact]
    public async Task GetInterestedHikers_ExcludesCallerAndSortsByDistance()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        // Caller at (0,0). Near user ~111km away at (1,0). Far user ~2200km away at (20,0).
        db.Users.Add(MakeUser("user-1", "caller@example.com", 0, 0));
        db.Users.Add(MakeUser("user-2", "near@example.com", 1, 0));
        db.Users.Add(MakeUser("user-3", "far@example.com", 20, 0));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-3", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-1");

        var result = await controller.GetInterestedHikers(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var hikers = Assert.IsAssignableFrom<IReadOnlyList<InterestedHikerDto>>(ok.Value);
        Assert.Equal(2, hikers.Count);
        Assert.Equal("user-2", hikers[0].UserId);
        Assert.Equal("user-3", hikers[1].UserId);
        Assert.True(hikers[0].DistanceKm < hikers[1].DistanceKm);
    }

    [Fact]
    public async Task GetInterestedHikers_UnknownDistance_SortsAfterKnown()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.Users.Add(MakeUser("user-1", "caller@example.com", 0, 0));
        db.Users.Add(MakeUser("user-2", "nolocation@example.com"));
        db.Users.Add(MakeUser("user-3", "near@example.com", 1, 0));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-3", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-1");

        var result = await controller.GetInterestedHikers(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var hikers = Assert.IsAssignableFrom<IReadOnlyList<InterestedHikerDto>>(ok.Value);
        Assert.Equal("user-3", hikers[0].UserId);
        Assert.Equal("user-2", hikers[1].UserId);
        Assert.Null(hikers[1].DistanceKm);
    }

    [Fact]
    public async Task CreateTrip_InvitesAllInterestedUsersAndSendsEmails()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1, "Everest"));
        db.Users.Add(MakeUser("user-1", "creator@example.com"));
        db.Users.Add(MakeUser("user-2", "invitee1@example.com"));
        db.Users.Add(MakeUser("user-3", "invitee2@example.com"));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-3", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var controller = CreateController(db, emailSender, "user-1");

        var proposedDate = new DateOnly(2026, 6, 14);
        var result = await controller.CreateTrip(new CreateTripRequest(1, "Let's go in June", proposedDate));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TripDto>(created.Value);
        Assert.Equal(proposedDate, dto.ProposedDate);
        Assert.Equal(2, dto.Participants.Count);
        Assert.All(dto.Participants, p => Assert.Equal("Invited", p.Status));

        Assert.Equal(2, db.TripParticipants.Count());
        Assert.Equal(2, db.Notifications.Count());
        Assert.Equal(2, emailSender.SentEmails.Count);
        Assert.Contains(emailSender.SentEmails, e => e.ToEmail == "invitee1@example.com");
        Assert.Contains(emailSender.SentEmails, e => e.ToEmail == "invitee2@example.com");
    }

    [Fact]
    public async Task CreateTrip_DoesNotInviteCreatorEvenIfOnOwnBucketList()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.Users.Add(MakeUser("user-1", "creator@example.com"));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-1");

        var result = await controller.CreateTrip(new CreateTripRequest(1, null, null));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TripDto>(created.Value);
        Assert.Empty(dto.Participants);
    }

    [Fact]
    public async Task Respond_Join_RevealsEmailsToOtherJoinedMembers()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.Users.Add(MakeUser("user-1", "creator@example.com"));
        db.Users.Add(MakeUser("user-2", "joiner@example.com"));
        db.Users.Add(MakeUser("user-3", "pending@example.com"));
        var trip = new Trip { PeakId = 1, CreatorUserId = "user-1", CreatedAt = DateTime.UtcNow };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        db.TripParticipants.Add(new TripParticipant { TripId = trip.Id, UserId = "user-2", Status = TripParticipantStatus.Invited, InvitedAt = DateTime.UtcNow });
        db.TripParticipants.Add(new TripParticipant { TripId = trip.Id, UserId = "user-3", Status = TripParticipantStatus.Invited, InvitedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var joinController = CreateController(db, new FakeEmailSender(), "user-2");
        var joinResult = await joinController.Respond(trip.Id, new RespondToTripRequest("Joined"));
        var joinOk = Assert.IsType<OkObjectResult>(joinResult.Result);
        var joinedDto = Assert.IsType<TripDto>(joinOk.Value);

        Assert.Equal("creator@example.com", joinedDto.CreatorEmail);
        var selfEntry = joinedDto.Participants.Single(p => p.UserId == "user-2");
        Assert.Equal("joiner@example.com", selfEntry.Email);
        var pendingEntry = joinedDto.Participants.Single(p => p.UserId == "user-3");
        Assert.Null(pendingEntry.Email);
    }

    [Fact]
    public async Task Respond_NotInvited_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.Users.Add(MakeUser("user-1", "creator@example.com"));
        var trip = new Trip { PeakId = 1, CreatorUserId = "user-1", CreatedAt = DateTime.UtcNow };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-99");

        var result = await controller.Respond(trip.Id, new RespondToTripRequest("Joined"));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Respond_InvalidStatus_ReturnsBadRequest()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.Users.Add(MakeUser("user-1", "creator@example.com"));
        db.Users.Add(MakeUser("user-2", "invitee@example.com"));
        var trip = new Trip { PeakId = 1, CreatorUserId = "user-1", CreatedAt = DateTime.UtcNow };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        db.TripParticipants.Add(new TripParticipant { TripId = trip.Id, UserId = "user-2", Status = TripParticipantStatus.Invited, InvitedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeEmailSender(), "user-2");

        var result = await controller.Respond(trip.Id, new RespondToTripRequest("Invited"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
