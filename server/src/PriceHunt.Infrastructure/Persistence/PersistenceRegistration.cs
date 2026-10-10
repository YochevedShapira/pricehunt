using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.History;
using PriceHunt.Application.Persistence;
using PriceHunt.Infrastructure.History;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Registers the SQLite-backed persistence.
/// </summary>
public static class PersistenceRegistration
{
    public static IServiceCollection AddSqlitePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PriceHuntDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IHistoryReader, HistoryReader>();

        return services;
    }

    /// <summary>
    /// Creates the database on first run and applies any pending migrations.
    /// </summary>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
