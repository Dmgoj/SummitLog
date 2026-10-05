namespace SummitLog.Api.Models;

public class TripParticipant
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public TripParticipantStatus Status { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
