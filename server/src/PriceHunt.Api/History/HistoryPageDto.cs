namespace PriceHunt.Api.History;

public sealed record HistoryPageDto(
    IReadOnlyList<HistoryEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
