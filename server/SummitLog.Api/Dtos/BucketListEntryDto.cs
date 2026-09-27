namespace SummitLog.Api.Dtos;

public record BucketListEntryDto(
    int PeakId,
    string PeakName,
    double Latitude,
    double Longitude,
    int? ElevationMeters,
    string CountryCode);
