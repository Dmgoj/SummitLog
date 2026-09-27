namespace SummitLog.Api.Models;

public class Peak
{
    public int Id { get; set; }
    public long GeoNameId { get; set; }
    public string Name { get; set; } = default!;
    public string? AlternateNames { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? ElevationMeters { get; set; }
    public string FeatureCode { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public string? Timezone { get; set; }
    public DateTime SourceModifiedAt { get; set; }
    public DateTime IngestedAt { get; set; }

    public ICollection<VisitedPeak> VisitedByUsers { get; set; } = new List<VisitedPeak>();
    public ICollection<BucketListEntry> BucketListedByUsers { get; set; } = new List<BucketListEntry>();
    public ElevationOverride? ElevationOverride { get; set; }
}
