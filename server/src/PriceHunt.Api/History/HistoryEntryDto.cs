namespace PriceHunt.Api.History;

/// <summary>
/// One history row as exposed to the client. <paramref name="Amount"/> and
/// <paramref name="Currency"/> are null unless the outcome is Priced.
/// </summary>
public sealed record HistoryEntryDto(
    Guid SearchId,
    DateTimeOffset Date,
    string FromLocation,
    string ToLocation,
    string SupplierName,
    string Outcome,
    decimal? Amount,
    string? Currency,
    long ResponseTimeMs);
