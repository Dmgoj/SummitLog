namespace SummitLog.Api.Dtos;

public record PeakSearchResultDto(IReadOnlyList<PeakSummaryDto> Items, int Page, int PageSize, int TotalCount);
