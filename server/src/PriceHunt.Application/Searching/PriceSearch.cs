using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Persistence;
using PriceHunt.Application.Searching.Events;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;

namespace PriceHunt.Application.Searching;

/// <summary>
/// Composes the <see cref="Search"/> aggregate, the supplier fan-out and the
/// repository into one streamed use case. Every outcome is recorded on the
/// aggregate and saved before it is yielded, so what the client sees is
/// always already on disk.
/// </summary>
public sealed partial class PriceSearch : IPriceSearch
{
    private readonly ISupplierCatalog _catalog;
    private readonly ISupplierFanOut _fanOut;
    private readonly ISearchRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PriceSearch> _logger;
    private readonly TimeSpan _timeout;

    public PriceSearch(
        ISupplierCatalog catalog,
        ISupplierFanOut fanOut,
        ISearchRepository repository,
        TimeProvider timeProvider,
        SearchOptions options,
        ILogger<PriceSearch> logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(fanOut);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            options.Timeout, TimeSpan.Zero, nameof(options));

        _catalog = catalog;
        _fanOut = fanOut;
        _repository = repository;
        _timeProvider = timeProvider;
        _timeout = options.Timeout;
        _logger = logger;
    }

    public async IAsyncEnumerable<SearchEvent> RunAsync(
        SearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var suppliers = ResolveSuppliers(request.SupplierNames);
        var search = Search.Start(
            request.Route, request.Dates, request.SupplierNames, _timeProvider.GetUtcNow());
        await _repository.AddAsync(search, cancellationToken).ConfigureAwait(false);
        LogStarted(search.Id, suppliers.Count);

        try
        {
            yield return new SearchStarted(search.Id, suppliers.Count);

            var priceRequest = new PriceRequest(request.Route, request.Dates);
            var outcomes = _fanOut.QueryAsync(
                suppliers, priceRequest, _timeout, cancellationToken);

            await foreach (var outcome in outcomes.ConfigureAwait(false))
            {
                yield return await RecordAsync(search, outcome, cancellationToken)
                    .ConfigureAwait(false);
            }

            yield return await EndAsync(search, FinalStatusOf(search)).ConfigureAwait(false);
        }
        finally
        {
            // Still running here means the stream did not reach its normal end:
            // the caller cancelled (the fan-out threw OperationCanceledException,
            // which propagates) or stopped consuming. Either way: cancelled.
            if (search.Status == SearchStatus.Running)
            {
                await EndAsync(search, SearchStatus.Cancelled).ConfigureAwait(false);
            }
        }
    }

    private List<ISupplier> ResolveSuppliers(IReadOnlyCollection<string> names)
    {
        return names
            .Select(name => _catalog.Find(name)
                ?? throw new ArgumentException($"Unknown supplier '{name}'.", nameof(names)))
            .ToList();
    }

    private async Task<SearchEvent> RecordAsync(
        Search search,
        SupplierOutcome outcome,
        CancellationToken cancellationToken)
    {
        var searchEvent = Record(search, outcome);
        await _repository.SaveAsync(search, cancellationToken).ConfigureAwait(false);

        return searchEvent;
    }

    private SearchEvent Record(Search search, SupplierOutcome outcome)
    {
        var now = _timeProvider.GetUtcNow();

        return outcome switch
        {
            SupplierPriced priced => RecordPrice(search, priced, now),
            SupplierFailure failure => RecordFailure(search, failure, now),
            _ => throw new InvalidOperationException(
                $"Unexpected outcome type {outcome.GetType().Name}."),
        };
    }

    private static PriceReceived RecordPrice(Search search, SupplierPriced priced, DateTimeOffset now)
    {
        var response = search.RecordPrice(priced.SupplierName, priced.Price, now);

        return new PriceReceived(response.SupplierName, priced.Price, response.ResponseTime);
    }

    private static SupplierFailed RecordFailure(
        Search search,
        SupplierFailure failure,
        DateTimeOffset now)
    {
        var response = search.RecordFailure(failure.SupplierName, failure.Reason, now);

        return new SupplierFailed(response.SupplierName, failure.Reason, response.ResponseTime);
    }

    private static SearchStatus FinalStatusOf(Search search)
    {
        return search.Responses.Count == search.RequestedSuppliers.Count
            ? SearchStatus.Completed
            : SearchStatus.TimedOut;
    }

    private async Task<SearchEnded> EndAsync(Search search, SearchStatus status)
    {
        var respondedCount = search.Responses.Count;
        search.End(status, _timeProvider.GetUtcNow());

        // The caller's token may already be cancelled; the final status must
        // be written regardless, so this save deliberately ignores it.
        await _repository.SaveAsync(search, CancellationToken.None).ConfigureAwait(false);
        LogEnded(search.Id, status, respondedCount);

        return new SearchEnded(status, respondedCount);
    }

    [LoggerMessage(LogLevel.Information, "Search {SearchId} started for {SupplierCount} suppliers.")]
    private partial void LogStarted(Guid searchId, int supplierCount);

    [LoggerMessage(LogLevel.Information, "Search {SearchId} ended {Status} with {RespondedCount} responses.")]
    private partial void LogEnded(Guid searchId, SearchStatus status, int respondedCount);
}
