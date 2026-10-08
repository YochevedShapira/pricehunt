using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain.Entities;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Search"/> aggregate root to the <c>Searches</c> table.
/// </summary>
public sealed class SearchConfiguration : IEntityTypeConfiguration<Search>
{
    private const int LOCATION_MAX_LENGTH = 100;

    public void Configure(EntityTypeBuilder<Search> builder)
    {
        builder.ToTable("Searches");
        builder.HasKey(s => s.Id);

        // Value objects become columns on the same row (complex types).
        builder.ComplexProperty(s => s.Route, route =>
        {
            route.Property(r => r.FromLocation)
                .HasColumnName("FromLocation")
                .HasMaxLength(LOCATION_MAX_LENGTH);
            route.Property(r => r.ToLocation)
                .HasColumnName("ToLocation")
                .HasMaxLength(LOCATION_MAX_LENGTH);
        });

        builder.ComplexProperty(s => s.Dates, dates =>
        {
            dates.Property(d => d.From).HasColumnName("FromDate");
            dates.Property(d => d.To).HasColumnName("ToDate");
        });

        // The requested names are an attribute of the search, never queried
        // on their own, so a JSON primitive collection beats a join table.
        builder.PrimitiveCollection(s => s.RequestedSuppliers)
            .HasField("_requestedSuppliers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(s => s.StartedAt).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(s => s.EndedAt).HasConversion(UtcDateTimeConverter.NullableInstance);
        builder.Property(s => s.Status).HasConversion<int>();

        builder.HasMany(s => s.Responses)
            .WithOne()
            .HasForeignKey(r => r.SearchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Responses)
            .HasField("_responses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.StartedAt);
    }
}
