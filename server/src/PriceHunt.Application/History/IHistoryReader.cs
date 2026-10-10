namespace PriceHunt.Application.History;

/// <summary>
/// Read-side port for the price history. Filtering, sorting and paging
/// happen in the store, never in memory, because history grows without
/// bound.
/// </summary>
public interface IHistoryReader
{
    Task<HistoryPage> ReadAsync(HistoryQuery query, CancellationToken cancellationToken);
}
