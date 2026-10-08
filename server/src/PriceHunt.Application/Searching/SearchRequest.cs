using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Searching;

/// <summary>
/// A validated request to search prices: where, when and which suppliers.
/// </summary>
public sealed record SearchRequest(
    Route Route,
    DateRange Dates,
    IReadOnlyCollection<string> SupplierNames);
