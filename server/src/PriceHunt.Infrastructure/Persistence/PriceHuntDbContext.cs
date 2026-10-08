using Microsoft.EntityFrameworkCore;
using PriceHunt.Domain.Entities;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// EF Core context over the search history. Mapping lives in the
/// configuration classes next to it, one per aggregate member, so the
/// domain entities stay free of persistence attributes.
/// </summary>
public sealed class PriceHuntDbContext : DbContext
{
    public PriceHuntDbContext(DbContextOptions<PriceHuntDbContext> options)
        : base(options)
    {
    }

    public DbSet<Search> Searches => Set<Search>();

    public DbSet<SupplierResponse> SupplierResponses => Set<SupplierResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PriceHuntDbContext).Assembly);
    }
}
