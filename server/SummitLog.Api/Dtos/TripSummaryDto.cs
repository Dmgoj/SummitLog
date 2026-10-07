namespace SummitLog.Api.Dtos;

public record TripSummaryDto(
    int Id,
    int PeakId,
    string PeakName,
    bool IsCreator,
    string? CallerStatus,
    int ParticipantCount,
    DateTime CreatedAt);
