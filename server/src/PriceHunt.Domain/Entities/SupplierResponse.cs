using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Entities;

/// <summary>
/// The outcome of asking one supplier for a price within one search.
/// Created through the factory methods so that each outcome carries exactly
/// the data that makes sense for it.
/// </summary>
public sealed class SupplierResponse
{
    private SupplierResponse(
        Guid searchId,
        string supplierName,
        ResponseOutcome outcome,
        Money? price,
        string? failureReason,
        TimeSpan responseTime,
        DateTimeOffset recordedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierName);
        ArgumentOutOfRangeException.ThrowIfLessThan(responseTime, TimeSpan.Zero);

        SearchId = searchId;
        SupplierName = supplierName;
        Outcome = outcome;
        Price = price;
        FailureReason = failureReason;
        ResponseTime = responseTime;
        RecordedAt = recordedAt;
    }

    /// <summary>
    /// Parameterless constructor for the persistence layer only.
    /// </summary>
    private SupplierResponse()
    {
        SupplierName = null!;
    }

    public long Id { get; private set; }

    public Guid SearchId { get; private set; }

    public string SupplierName { get; private set; }

    public ResponseOutcome Outcome { get; private set; }

    public Money? Price { get; private set; }

    public string? FailureReason { get; private set; }

    public TimeSpan ResponseTime { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public static SupplierResponse Quoted(
        Guid searchId,
        string supplierName,
        Money price,
        TimeSpan responseTime,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(price);

        return new SupplierResponse(
            searchId,
            supplierName,
            ResponseOutcome.Quoted,
            price,
            failureReason: null,
            responseTime,
            recordedAt);
    }

    public static SupplierResponse Failed(
        Guid searchId,
        string supplierName,
        string failureReason,
        TimeSpan responseTime,
        DateTimeOffset recordedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        return new SupplierResponse(
            searchId,
            supplierName,
            ResponseOutcome.Failed,
            price: null,
            failureReason,
            responseTime,
            recordedAt);
    }

    public static SupplierResponse NoResponse(
        Guid searchId,
        string supplierName,
        TimeSpan responseTime,
        DateTimeOffset recordedAt)
    {
        return new SupplierResponse(
            searchId,
            supplierName,
            ResponseOutcome.NoResponse,
            price: null,
            failureReason: null,
            responseTime,
            recordedAt);
    }
}
