using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Entities;

/// <summary>
/// Aggregate root for one price search. Owns the supplier responses and
/// guarantees that, once ended, every requested supplier has exactly one
/// response.
/// </summary>
public sealed class Search
{
    private readonly List<string> _requestedSuppliers;
    private readonly List<SupplierResponse> _responses = [];

    private Search(
        Guid id,
        Route route,
        DateRange dates,
        List<string> requestedSuppliers,
        DateTimeOffset startedAt)
    {
        Id = id;
        Route = route;
        Dates = dates;
        _requestedSuppliers = requestedSuppliers;
        StartedAt = startedAt;
        Status = SearchStatus.Running;
    }

    /// <summary>
    /// Parameterless constructor for the persistence layer only.
    /// </summary>
    private Search()
    {
        Route = null!;
        Dates = null!;
        _requestedSuppliers = [];
    }

    public Guid Id { get; private set; }

    public Route Route { get; private set; }

    public DateRange Dates { get; private set; }

    public IReadOnlyList<string> RequestedSuppliers => _requestedSuppliers;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public SearchStatus Status { get; private set; }

    public IReadOnlyCollection<SupplierResponse> Responses => _responses;

    public static Search Start(
        Route route,
        DateRange dates,
        IReadOnlyCollection<string> requestedSuppliers,
        DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(requestedSuppliers);
        ValidateSupplierList(requestedSuppliers);

        return new Search(
            Guid.CreateVersion7(),
            route,
            dates,
            [.. requestedSuppliers],
            startedAt);
    }

    public SupplierResponse RecordPrice(
        string supplierName,
        Money price,
        DateTimeOffset recordedAt)
    {
        EnsureCanRecord(supplierName);

        return Add(SupplierResponse.Priced(
            Id, supplierName, price, ElapsedAt(recordedAt), recordedAt));
    }

    public SupplierResponse RecordFailure(
        string supplierName,
        string failureReason,
        DateTimeOffset recordedAt)
    {
        EnsureCanRecord(supplierName);

        return Add(SupplierResponse.Failed(
            Id, supplierName, failureReason, ElapsedAt(recordedAt), recordedAt));
    }

    /// <summary>
    /// Closes the search with its final status and records
    /// <see cref="ResponseOutcome.NoResponse"/> for every supplier that has
    /// not answered yet.
    /// </summary>
    public void End(SearchStatus finalStatus, DateTimeOffset endedAt)
    {
        if (finalStatus == SearchStatus.Running)
        {
            throw new ArgumentException(
                "A search cannot end in the Running state.",
                nameof(finalStatus));
        }

        EnsureRunning();
        var duration = ElapsedAt(endedAt);

        foreach (var supplier in UnansweredSuppliers())
        {
            Add(SupplierResponse.NoResponse(Id, supplier, duration, endedAt));
        }

        Status = finalStatus;
        EndedAt = endedAt;
    }

    private static void ValidateSupplierList(IReadOnlyCollection<string> suppliers)
    {
        if (suppliers.Count == 0)
        {
            throw new ArgumentException(
                "At least one supplier must be requested.",
                nameof(suppliers));
        }

        if (suppliers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Supplier names must not be blank.",
                nameof(suppliers));
        }

        if (suppliers.Distinct(StringComparer.Ordinal).Count() != suppliers.Count)
        {
            throw new ArgumentException(
                "The same supplier cannot be requested twice.",
                nameof(suppliers));
        }
    }

    private void EnsureCanRecord(string supplierName)
    {
        EnsureRunning();

        if (!_requestedSuppliers.Contains(supplierName))
        {
            throw new ArgumentException(
                $"Supplier '{supplierName}' was not requested in this search.",
                nameof(supplierName));
        }

        if (_responses.Any(r => r.SupplierName == supplierName))
        {
            throw new InvalidOperationException(
                $"Supplier '{supplierName}' has already responded to this search.");
        }
    }

    private void EnsureRunning()
    {
        if (Status != SearchStatus.Running)
        {
            throw new InvalidOperationException(
                $"The search has already ended with status {Status}.");
        }
    }

    private TimeSpan ElapsedAt(DateTimeOffset moment)
    {
        var elapsed = moment - StartedAt;
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero, nameof(moment));

        return elapsed;
    }

    private List<string> UnansweredSuppliers()
    {
        var answered = _responses.Select(r => r.SupplierName).ToHashSet();

        return _requestedSuppliers.Where(s => !answered.Contains(s)).ToList();
    }

    private SupplierResponse Add(SupplierResponse response)
    {
        _responses.Add(response);

        return response;
    }
}
