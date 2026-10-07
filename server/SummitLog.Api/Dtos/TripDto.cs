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
    DateOnly? ProposedDate,
    DateTime CreatedAt,
    string? CallerStatus,
    IReadOnlyList<TripParticipantDto> Participants);
