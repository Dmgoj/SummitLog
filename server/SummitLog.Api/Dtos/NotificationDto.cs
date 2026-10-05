namespace SummitLog.Api.Dtos;

public record NotificationDto(
    int Id,
    string Type,
    int? TripId,
    string Message,
    bool IsRead,
    DateTime CreatedAt);
