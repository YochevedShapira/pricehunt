using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Lets the <c>dotnet ef</c> tool build the context when generating
/// migrations, without starting the web host. Never used at runtime.
/// </summary>
public sealed class PriceHuntDbContextFactory : IDesignTimeDbContextFactory<PriceHuntDbContext>
{
    private const string DESIGN_TIME_CONNECTION = "Data Source=pricehunt-design.db";

    public PriceHuntDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PriceHuntDbContext>()
            .UseSqlite(DESIGN_TIME_CONNECTION)
            .Options;

        return new PriceHuntDbContext(options);
    }
}
