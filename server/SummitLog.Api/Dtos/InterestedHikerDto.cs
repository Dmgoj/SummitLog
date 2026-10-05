namespace SummitLog.Api.Dtos;

public record InterestedHikerDto(
    string UserId,
    string? FirstName,
    string? LastName,
    double? DistanceKm);
