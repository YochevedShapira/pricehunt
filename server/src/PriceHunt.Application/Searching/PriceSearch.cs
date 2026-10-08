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
    /// <summary>
    /// Maximum duration of a search, from the brief.
    /// </summary>
    private static readonly TimeSpan SEARCH_TIMEOUT = TimeSpan.FromSeconds(6);

    private readonly ISupplierCatalog _catalog;
    private readonly ISupplierFanOut _fanOut;
    private readonly ISearchRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PriceSearch> _logger;

    public PriceSearch(
        ISupplierCatalog catalog,
        ISupplierFanOut fanOut,
        ISearchRepository repository,
        TimeProvider timeProvider,
        ILogger<PriceSearch> logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(fanOut);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _catalog = catalog;
        _fanOut = fanOut;
        _repository = repository;
        _timeProvider = timeProvider;
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
            await using var outcomes = _fanOut
                .QueryAsync(suppliers, priceRequest, SEARCH_TIMEOUT, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            while (await MovedToNextAsync(outcomes, cancellationToken).ConfigureAwait(false))
            {
                yield return await RecordAsync(search, outcomes.Current, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                yield return await EndAsync(search, FinalStatusOf(search)).ConfigureAwait(false);
            }
        }
        finally
        {
            // Reached with the search still running when the caller cancelled
            // or stopped consuming the stream: either way it was cancelled.
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

    /// <summary>
    /// <c>yield return</c> cannot sit inside a <c>try</c>/<c>catch</c>, so the
    /// fan-out's cancellation is observed here and reported as "no more
    /// outcomes"; the caller then checks the token.
    /// </summary>
    private static async Task<bool> MovedToNextAsync(
        IAsyncEnumerator<SupplierOutcome> outcomes,
        CancellationToken cancellationToken)
    {
        try
        {
            return await outcomes.MoveNextAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
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
