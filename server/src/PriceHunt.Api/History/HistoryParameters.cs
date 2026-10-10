namespace PriceHunt.Api.History;

/// <summary>
/// The raw query string of a history request, as bound by ASP.NET Core.
/// Everything is optional; <see cref="HistoryQueryParser"/> applies defaults
/// and validation.
/// </summary>
public sealed record HistoryParameters
{
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>
    /// Supplier names to include. Absent or empty means every supplier.
    /// </summary>
    public string[]? Suppliers { get; init; }

    public string? FromLocation { get; init; }

    public string? ToLocation { get; init; }

    /// <summary>
    /// One of date, route, supplier, price, responseTime. Defaults to date.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// asc or desc. Defaults to desc.
    /// </summary>
    public string? SortDirection { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
