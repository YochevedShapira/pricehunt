using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Persistence;
using PriceHunt.Domain.Entities;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// <see cref="ISearchRepository"/> over EF Core. The context tracks the
/// aggregate after <see cref="AddAsync"/>, so <see cref="SaveAsync"/> only
/// has to flush whatever the aggregate changed since the last save.
/// </summary>
public sealed class SearchRepository : ISearchRepository
{
    private readonly PriceHuntDbContext _db;

    public SearchRepository(PriceHuntDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task AddAsync(Search search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        _db.Searches.Add(search);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(Search search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        if (_db.Entry(search).State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                $"Search {search.Id} is not tracked; add it before saving changes.");
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
