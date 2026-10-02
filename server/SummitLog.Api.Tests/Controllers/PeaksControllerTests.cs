using SummitLog.Api.Controllers;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SummitLog.Api.Tests.Controllers;

public class PeaksControllerTests
{
    private static PeaksController CreateController(out Data.AppDbContext db)
    {
        db = TestHelpers.CreateDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new PeaksController(db, cache);
    }

    private static Peak MakePeak(int id, string name, string country, int? elevation, string featureCode = "MT")
    {
        return new Peak
        {
            Id = id,
            GeoNameId = 1000 + id,
            Name = name,
            Latitude = 10 + id,
            Longitude = 20 + id,
            ElevationMeters = elevation,
            CountryCode = country,
            FeatureCode = featureCode,
            SourceModifiedAt = DateTime.UtcNow,
            IngestedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Search_NoQueryOrFilter_ReturnsBadRequest()
    {
        var controller = CreateController(out _);

        var result = await controller.Search(q: null, country: null, minElevation: null, maxElevation: null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_ByName_ReturnsMatchingPeaks()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "Mount Everest", "NP", 8849),
            MakePeak(2, "Mount Kilimanjaro", "TZ", 5895),
            MakePeak(3, "Denali", "US", 6190));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: "Mount", country: null, minElevation: null, maxElevation: null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Equal(2, dto.TotalCount);
        Assert.All(dto.Items, i => Assert.Contains("Mount", i.Name));
    }

    [Fact]
    public async Task Search_ByName_IsCaseInsensitive()
    {
        var controller = CreateController(out var db);
        db.Peaks.Add(MakePeak(1, "Mount Everest", "NP", 8849));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: "everest", country: null, minElevation: null, maxElevation: null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Single(dto.Items);
    }

    [Fact]
    public async Task Search_ByCountry_FiltersCorrectly()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "Everest", "NP", 8849),
            MakePeak(2, "K2", "PK", 8611));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: null, country: "NP", minElevation: null, maxElevation: null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Single(dto.Items);
        Assert.Equal("Everest", dto.Items[0].Name);
    }

    [Fact]
    public async Task Search_ByElevationRange_FiltersCorrectly()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "Low Hill", "US", 500),
            MakePeak(2, "Mid Peak", "US", 3000),
            MakePeak(3, "High Peak", "US", 7000));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: null, country: "US", minElevation: 1000, maxElevation: 5000);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Single(dto.Items);
        Assert.Equal("Mid Peak", dto.Items[0].Name);
    }

    [Fact]
    public async Task Search_SortByElevationAscending_KeepsKnownElevationsBeforeNulls()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "Unknown Elevation", "US", null),
            MakePeak(2, "Low", "US", 100),
            MakePeak(3, "High", "US", 5000));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: null, country: "US", minElevation: null, maxElevation: null, sortBy: "elevation", sortDir: "asc");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Equal(["Low", "High", "Unknown Elevation"], dto.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Search_SortByElevationDescending_KeepsKnownElevationsBeforeNulls()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "Unknown Elevation", "US", null),
            MakePeak(2, "Low", "US", 100),
            MakePeak(3, "High", "US", 5000));
        await db.SaveChangesAsync();

        var result = await controller.Search(q: null, country: "US", minElevation: null, maxElevation: null, sortBy: "elevation", sortDir: "desc");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Equal(["High", "Low", "Unknown Elevation"], dto.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Search_Pagination_ReturnsCorrectSlice()
    {
        var controller = CreateController(out var db);
        for (var i = 1; i <= 5; i++)
        {
            db.Peaks.Add(MakePeak(i, $"Peak {i:00}", "US", 1000 + i));
        }
        await db.SaveChangesAsync();

        var result = await controller.Search(q: null, country: "US", minElevation: null, maxElevation: null, page: 2, pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakSearchResultDto>(ok.Value);
        Assert.Equal(5, dto.TotalCount);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(["Peak 03", "Peak 04"], dto.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsPeakDto()
    {
        var controller = CreateController(out var db);
        db.Peaks.Add(MakePeak(1, "Everest", "NP", 8849));
        await db.SaveChangesAsync();

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PeakDto>(ok.Value);
        Assert.Equal("Everest", dto.Name);
        Assert.Equal("Nepal", dto.CountryName);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController(out _);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCountries_ReturnsDistinctCodesWithDisplayNames()
    {
        var controller = CreateController(out var db);
        db.Peaks.AddRange(
            MakePeak(1, "A", "NP", 100),
            MakePeak(2, "B", "NP", 200),
            MakePeak(3, "C", "US", 300));
        await db.SaveChangesAsync();

        var result = await controller.GetCountries();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var countries = Assert.IsAssignableFrom<IReadOnlyList<CountryOptionDto>>(ok.Value);
        Assert.Equal(2, countries.Count);
        Assert.Contains(countries, c => c.Code == "NP" && c.Name == "Nepal");
    }
}
