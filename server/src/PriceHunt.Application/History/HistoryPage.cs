namespace PriceHunt.Application.History;

/// <summary>
/// One page of history plus the total number of matching rows, so the
/// client can render a paginator.
/// </summary>
public sealed record HistoryPage(
    IReadOnlyList<HistoryEntry> Items,
    int TotalCount,
    int Page,
    int PageSize);
