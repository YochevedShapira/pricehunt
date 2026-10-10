namespace PriceHunt.Application.History;

/// <summary>
/// The columns the history table can be sorted by. A closed set, so the
/// API can never be asked to order by an arbitrary expression.
/// </summary>
public enum HistorySortColumn
{
    Date = 0,
    Route = 1,
    Supplier = 2,
    Price = 3,
    ResponseTime = 4,
}
