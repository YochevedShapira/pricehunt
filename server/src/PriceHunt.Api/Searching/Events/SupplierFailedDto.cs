namespace PriceHunt.Api.Searching.Events;

/// <summary>
/// Payload of the <c>failed</c> SSE event.
/// </summary>
public sealed record SupplierFailedDto(string SupplierName, string Reason, long ResponseTimeMs);
