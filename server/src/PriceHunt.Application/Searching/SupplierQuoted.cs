using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Searching;

/// <summary>
/// The supplier returned a price.
/// </summary>
public sealed record SupplierQuoted(
    string SupplierName,
    Money Price,
    TimeSpan ResponseTime) : SupplierOutcome(SupplierName, ResponseTime);
