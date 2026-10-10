namespace PriceHunt.Api.Searching.Events;

/// <summary>
/// Payload of the <c>price</c> SSE event.
/// </summary>
public sealed record PriceReceivedDto(
    string SupplierName,
    decimal Amount,
    string Currency,
    long ResponseTimeMs);
