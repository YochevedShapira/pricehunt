namespace PriceHunt.Application.Searching;

/// <summary>
/// The supplier answered with an error instead of a price.
/// </summary>
public sealed record SupplierFailure(string SupplierName, string Reason)
    : SupplierOutcome(SupplierName);
