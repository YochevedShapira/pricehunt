using PriceHunt.Domain.Entities;

namespace PriceHunt.Application.Persistence;

/// <summary>
/// Port for storing <see cref="Search"/> aggregates. The aggregate is
/// written once when the search starts and saved again each time it
/// changes, so a crash mid-search still leaves the responses received so
/// far on disk.
/// </summary>
public interface ISearchRepository
{
    /// <summary>
    /// Stores a newly started search.
    /// </summary>
    Task AddAsync(Search search, CancellationToken cancellationToken);

    /// <summary>
    /// Persists the changes made to a search that was previously added.
    /// </summary>
    Task SaveAsync(Search search, CancellationToken cancellationToken);
}
