namespace SummitLog.Api.Dtos;

public record TripParticipantDto(
    string UserId,
    string? FirstName,
    string? LastName,
    string Status,
    string? Email);
