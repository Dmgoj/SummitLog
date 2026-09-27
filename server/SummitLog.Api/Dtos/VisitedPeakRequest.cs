using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos;

public record CreateVisitedPeakRequest(
    int PeakId,
    DateOnly VisitedOn,
    [StringLength(2000)] string? Notes);

public record UpdateVisitedPeakRequest(
    DateOnly VisitedOn,
    [StringLength(2000)] string? Notes);
