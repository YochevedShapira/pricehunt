namespace PriceHunt.Application.History;

/// <summary>
/// A validated request for one page of price history. Dates are inclusive
/// calendar days; an empty supplier list means every supplier; location
/// filters match anywhere in the location name.
/// </summary>
public sealed record HistoryQuery
{
    public const int MaxPageSize = 100;

    public HistoryQuery(
        DateOnly? startDate,
        DateOnly? endDate,
        IReadOnlyCollection<string> supplierNames,
        string? fromLocation,
        string? toLocation,
        HistorySortColumn sortBy,
        SortDirection sortDirection,
        int page,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(supplierNames);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);
        if (startDate > endDate)
        {
            throw new ArgumentException(
                $"The start date {startDate} is after the end date {endDate}.",
                nameof(startDate));
        }

        StartDate = startDate;
        EndDate = endDate;
        SupplierNames = supplierNames;
        FromLocation = fromLocation;
        ToLocation = toLocation;
        SortBy = sortBy;
        SortDirection = sortDirection;
        Page = page;
        PageSize = pageSize;
    }

    public DateOnly? StartDate { get; }

    public DateOnly? EndDate { get; }

    public IReadOnlyCollection<string> SupplierNames { get; }

    public string? FromLocation { get; }

    public string? ToLocation { get; }

    public HistorySortColumn SortBy { get; }

    public SortDirection SortDirection { get; }

    /// <summary>
    /// One-based page number.
    /// </summary>
    public int Page { get; }

    public int PageSize { get; }
}
