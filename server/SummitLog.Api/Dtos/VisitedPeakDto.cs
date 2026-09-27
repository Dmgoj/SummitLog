namespace SummitLog.Api.Dtos;

public record VisitedPeakDto(
    int PeakId,
    string PeakName,
    double Latitude,
    double Longitude,
    int? ElevationMeters,
    string CountryCode,
    DateOnly VisitedOn,
    string? Notes);
