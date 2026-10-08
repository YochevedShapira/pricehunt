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
    /// always <see cref="SearchStarted"/>; the last is <see cref="SearchEnded"/>
    /// unless the caller cancels, in which case the stream simply ends after
    /// the search has been recorded as cancelled.
    /// </summary>
    IAsyncEnumerable<SearchEvent> RunAsync(
        SearchRequest request,
        CancellationToken cancellationToken);
}
