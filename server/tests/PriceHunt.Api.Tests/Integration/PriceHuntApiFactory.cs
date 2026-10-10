using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Api.Tests.Integration;

/// <summary>
/// Hosts the real API in process with two substitutions: an in-memory
/// SQLite database (still created by the real migrations) and scripted
/// suppliers instead of the random simulated ones. The search timeout is
/// shortened through configuration, which also proves the appsettings
/// binding works.
/// </summary>
public sealed class PriceHuntApiFactory : WebApplicationFactory<Program>
{
    public const string FAST = "Fast";
    public const string SLOW = "Slow";
    public const string BROKEN = "Broken";
    public const string SILENT = "Silent";

    public static readonly TimeSpan SEARCH_TIMEOUT = TimeSpan.FromSeconds(1);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PriceHuntApiFactory()
    {
        _connection.Open();
    }

    public PriceHuntDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Search:Timeout", SEARCH_TIMEOUT.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PriceHuntDbContext>>();
            services.AddDbContext<PriceHuntDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<ISupplier>();
            services.AddSingleton<ISupplier>(
                ScriptedSupplier.Answering(FAST, TimeSpan.FromMilliseconds(50), 1000m));
            services.AddSingleton<ISupplier>(
                ScriptedSupplier.Answering(SLOW, TimeSpan.FromMilliseconds(200), 500m));
            services.AddSingleton<ISupplier>(
                ScriptedSupplier.Failing(BROKEN, TimeSpan.FromMilliseconds(100)));
            services.AddSingleton<ISupplier>(ScriptedSupplier.Silent(SILENT));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
