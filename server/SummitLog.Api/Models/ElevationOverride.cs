namespace SummitLog.Api.Models;

public class ElevationOverride
{
    public int Id { get; set; }
    public int PeakId { get; set; }
    public Peak Peak { get; set; } = default!;
    public int ElevationMeters { get; set; }
    public string Source { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
