using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Searching.Events;

/// <summary>
/// A supplier returned a price.
/// </summary>
public sealed record QuoteReceived(
    string SupplierName,
    Money Price,
    TimeSpan ResponseTime) : SearchEvent;
