using SummitLog.Api.Controllers;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace SummitLog.Api.Tests.Controllers;

public class VisitedPeaksControllerTests
{
    private static VisitedPeaksController CreateController(AppDbContext db, string userId = "user-1")
    {
        var controller = new VisitedPeaksController(db);
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

    [Fact]
    public async Task GetAll_OnlyReturnsCallingUsersEntries()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-1", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-2", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "user-1");
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IReadOnlyList<VisitedPeakDto>>(ok.Value);
        Assert.Single(entries);
    }

    [Fact]
    public async Task Create_UnknownPeak_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateVisitedPeakRequest(999, new DateOnly(2026, 1, 1), null));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_Duplicate_ReturnsConflict()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-1", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateVisitedPeakRequest(1, new DateOnly(2026, 1, 2), null));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_RemovesMatchingBucketListEntryForSameUser()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1, "Everest"));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        await controller.Create(new CreateVisitedPeakRequest(1, new DateOnly(2026, 1, 1), "Great climb"));

        Assert.Empty(db.BucketListEntries);
        Assert.Single(db.VisitedPeaks);
    }

    [Fact]
    public async Task Create_DoesNotRemoveOtherUsersBucketListEntry()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1, "Everest"));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        await controller.Create(new CreateVisitedPeakRequest(1, new DateOnly(2026, 1, 1), null));

        Assert.Single(db.BucketListEntries);
    }

    [Fact]
    public async Task Update_OwnEntry_UpdatesFields()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-1", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), Notes = "old", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Update(1, new UpdateVisitedPeakRequest(new DateOnly(2026, 2, 2), "updated"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitedPeakDto>(ok.Value);
        Assert.Equal(new DateOnly(2026, 2, 2), dto.VisitedOn);
        Assert.Equal("updated", dto.Notes);
    }

    [Fact]
    public async Task Update_AnotherUsersEntry_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-2", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Update(1, new UpdateVisitedPeakRequest(new DateOnly(2026, 2, 2), null));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_AnotherUsersEntry_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-2", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(db.VisitedPeaks);
    }

    [Fact]
    public async Task Delete_OwnEntry_RemovesItAndReturnsNoContent()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.VisitedPeaks.Add(new VisitedPeak { UserId = "user-1", PeakId = 1, VisitedOn = new DateOnly(2026, 1, 1), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.VisitedPeaks);
    }
}
