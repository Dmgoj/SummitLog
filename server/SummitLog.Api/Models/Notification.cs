namespace SummitLog.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public NotificationType Type { get; set; }
    public int? TripId { get; set; }
    public Trip? Trip { get; set; }
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
