namespace PriceHunt.Application.Searching.Events;

/// <summary>
/// First event of every search. <paramref name="SearchId"/> lets the client
/// discard late events from a previous search; <paramref name="SupplierCount"/>
/// is the denominator for its progress indicator.
/// </summary>
public sealed record SearchStarted(Guid SearchId, int SupplierCount) : SearchEvent;
