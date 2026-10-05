namespace SummitLog.Api.Models;

public class Trip
{
    public int Id { get; set; }
    public int PeakId { get; set; }
    public Peak Peak { get; set; } = default!;
    public string CreatorUserId { get; set; } = default!;
    public ApplicationUser CreatorUser { get; set; } = default!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<TripParticipant> Participants { get; set; } = new List<TripParticipant>();
}
