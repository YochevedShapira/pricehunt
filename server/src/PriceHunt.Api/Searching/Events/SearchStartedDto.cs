namespace PriceHunt.Api.Searching.Events;

/// <summary>
/// Payload of the <c>started</c> SSE event.
/// </summary>
public sealed record SearchStartedDto(Guid SearchId, int SupplierCount);
