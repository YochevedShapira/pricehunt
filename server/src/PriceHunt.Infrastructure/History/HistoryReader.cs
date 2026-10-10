using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.History;
using PriceHunt.Domain.Entities;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.History;

/// <summary>
/// <see cref="IHistoryReader"/> over EF Core. Builds one query that filters,
/// counts, sorts and pages in SQL; only the requested page is materialised.
/// </summary>
public sealed class HistoryReader : IHistoryReader
{
    private readonly PriceHuntDbContext _db;

    public HistoryReader(PriceHuntDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<HistoryPage> ReadAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = Filter(AllRows(), query);
        var totalCount = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await Sort(rows, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new HistoryEntry(
                row.Search.Id,
                row.Response.RecordedAt,
                row.Search.Route.FromLocation,
                row.Search.Route.ToLocation,
                row.Response.SupplierName,
                row.Response.Outcome,
                row.Response.Price,
                row.Response.ResponseTime))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new HistoryPage(page, totalCount, query.Page, query.PageSize);
    }

    private IQueryable<HistoryRow> AllRows()
    {
        return _db.Searches.SelectMany(
            search => search.Responses,
            (search, response) => new HistoryRow { Search = search, Response = response });
    }

    private static IQueryable<HistoryRow> Filter(IQueryable<HistoryRow> rows, HistoryQuery query)
    {
        if (query.StartDate is { } startDate)
        {
            var startOfDay = AtStartOfDay(startDate);
            rows = rows.Where(row => row.Response.RecordedAt >= startOfDay);
        }

        if (query.EndDate is { } endDate)
        {
            var startOfNextDay = AtStartOfDay(endDate.AddDays(1));
            rows = rows.Where(row => row.Response.RecordedAt < startOfNextDay);
        }

        if (query.SupplierNames.Count > 0)
        {
            rows = rows.Where(row => query.SupplierNames.Contains(row.Response.SupplierName));
        }

        if (!string.IsNullOrWhiteSpace(query.FromLocation))
        {
            var pattern = ContainsPattern(query.FromLocation);
            rows = rows.Where(row => EF.Functions.Like(row.Search.Route.FromLocation, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.ToLocation))
        {
            var pattern = ContainsPattern(query.ToLocation);
            rows = rows.Where(row => EF.Functions.Like(row.Search.Route.ToLocation, pattern));
        }

        return rows;
    }

    /// <summary>
    /// Orders by the requested column, then by response id so that paging
    /// is stable when several rows share the same sort value.
    /// </summary>
    private static IOrderedQueryable<HistoryRow> Sort(IQueryable<HistoryRow> rows, HistoryQuery query)
    {
        var descending = query.SortDirection == SortDirection.Descending;

        var sorted = query.SortBy switch
        {
            HistorySortColumn.Date => OrderBy(rows, row => row.Response.RecordedAt, descending),
            HistorySortColumn.Route => OrderBy(rows, row => row.Search.Route.FromLocation, descending)
                .ThenBy(row => row.Search.Route.ToLocation, descending),
            HistorySortColumn.Supplier => OrderBy(rows, row => row.Response.SupplierName, descending),
            HistorySortColumn.Price => OrderBy(rows, row => row.Response.Price!.Amount, descending),
            HistorySortColumn.ResponseTime => OrderBy(rows, row => row.Response.ResponseTime, descending),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.SortBy, "Unknown sort column."),
        };

        return sorted.ThenBy(row => row.Response.Id);
    }

    private static IOrderedQueryable<HistoryRow> OrderBy<TKey>(
        IQueryable<HistoryRow> rows,
        System.Linq.Expressions.Expression<Func<HistoryRow, TKey>> key,
        bool descending)
    {
        return descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
    }

    private static DateTimeOffset AtStartOfDay(DateOnly day)
    {
        return new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static string ContainsPattern(string term)
    {
        return $"%{term.Trim()}%";
    }

    /// <summary>
    /// Intermediate shape of the query. Object-initialiser syntax (not a
    /// constructor) so EF Core can translate later member access.
    /// </summary>
    private sealed class HistoryRow
    {
        public required Search Search { get; init; }

        public required SupplierResponse Response { get; init; }
    }
}
