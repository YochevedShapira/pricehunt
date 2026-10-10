using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.History;

/// <summary>
/// One row of the history table: a supplier's response within a search,
/// flattened with the search's route.
/// </summary>
public sealed record HistoryEntry(
    Guid SearchId,
    DateTimeOffset RecordedAt,
    string FromLocation,
    string ToLocation,
    string SupplierName,
    ResponseOutcome Outcome,
    Money? Price,
    TimeSpan ResponseTime);
