namespace PriceHunt.Application.Searching;

/// <summary>
/// What one supplier produced within the time budget of a search. Base of a
/// closed set: <see cref="SupplierQuoted"/> or <see cref="SupplierFailure"/>.
/// A supplier that never answers produces no outcome at all.
/// </summary>
public abstract record SupplierOutcome(string SupplierName, TimeSpan ResponseTime);
