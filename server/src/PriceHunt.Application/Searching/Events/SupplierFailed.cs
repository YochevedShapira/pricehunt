namespace PriceHunt.Application.Searching.Events;

/// <summary>
/// A supplier answered with an error instead of a price.
/// </summary>
public sealed record SupplierFailed(
    string SupplierName,
    string Reason,
    TimeSpan ResponseTime) : SearchEvent;
