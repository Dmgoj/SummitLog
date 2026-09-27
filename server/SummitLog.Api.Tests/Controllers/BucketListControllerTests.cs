using SummitLog.Api.Controllers;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace SummitLog.Api.Tests.Controllers;

public class BucketListControllerTests
{
    private static BucketListController CreateController(AppDbContext db, string userId = "user-1")
    {
        var controller = new BucketListController(db);
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
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "user-1");
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IReadOnlyList<BucketListEntryDto>>(ok.Value);
        Assert.Single(entries);
    }

    [Fact]
    public async Task Create_UnknownPeak_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateBucketListEntryRequest(999));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_DuplicateEntry_ReturnsConflict()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db);

        var result = await controller.Create(new CreateBucketListEntryRequest(1));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_Success_PersistsEntryForCallingUser()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1, "Everest"));
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Create(new CreateBucketListEntryRequest(1));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<BucketListEntryDto>(created.Value);
        Assert.Equal("Everest", dto.PeakName);

        var stored = Assert.Single(db.BucketListEntries);
        Assert.Equal("user-1", stored.UserId);
        Assert.Equal(1, stored.PeakId);
    }

    [Fact]
    public async Task Delete_EntryBelongingToAnotherUser_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-2", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(db.BucketListEntries);
    }

    [Fact]
    public async Task Delete_OwnEntry_RemovesItAndReturnsNoContent()
    {
        var db = TestHelpers.CreateDbContext();
        db.Peaks.Add(MakePeak(1));
        db.BucketListEntries.Add(new BucketListEntry { UserId = "user-1", PeakId = 1, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = CreateController(db, "user-1");

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.BucketListEntries);
    }
}
