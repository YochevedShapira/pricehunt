namespace PriceHunt.Api.Searching;

/// <summary>
/// The raw query string of a search, as bound by ASP.NET Core. Everything
/// is optional here; <see cref="SearchQueryParser"/> decides what is valid.
/// </summary>
public sealed record SearchQuery
{
    public string? FromLocation { get; init; }

    public string? ToLocation { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    /// <summary>
    /// Supplier names to query. Absent or empty means every supplier.
    /// </summary>
    public string[]? Suppliers { get; init; }
}
