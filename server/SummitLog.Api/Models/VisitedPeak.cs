namespace SummitLog.Api.Models;

public class VisitedPeak
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public int PeakId { get; set; }
    public Peak Peak { get; set; } = default!;
    public DateOnly VisitedOn { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
