namespace SummitLog.Api.Dtos;

public record TripDto(
    int Id,
    int PeakId,
    string PeakName,
    string CreatorUserId,
    string? CreatorFirstName,
    string? CreatorLastName,
    string? CreatorEmail,
    string? Notes,
    DateTime CreatedAt,
    string? CallerStatus,
    IReadOnlyList<TripParticipantDto> Participants);
