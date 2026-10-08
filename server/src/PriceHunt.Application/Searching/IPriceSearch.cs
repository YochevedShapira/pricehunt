using PriceHunt.Application.Searching.Events;

namespace PriceHunt.Application.Searching;

/// <summary>
/// The "search prices" use case: runs the suppliers, records everything,
/// and streams the progress to the caller.
/// </summary>
public interface IPriceSearch
{
    /// <summary>
    /// Streams <see cref="SearchEvent"/>s for one search. The first event is
    /// always <see cref="SearchStarted"/> and the last <see cref="SearchEnded"/>.
    /// If the caller cancels, the search is recorded as cancelled and the
    /// stream ends with an <see cref="OperationCanceledException"/>.
    /// </summary>
    IAsyncEnumerable<SearchEvent> RunAsync(
        SearchRequest request,
        CancellationToken cancellationToken);
}
