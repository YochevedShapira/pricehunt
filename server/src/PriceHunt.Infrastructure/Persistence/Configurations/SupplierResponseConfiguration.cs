using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain.Entities;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="SupplierResponse"/> to the <c>SupplierResponses</c> table.
/// </summary>
public sealed class SupplierResponseConfiguration : IEntityTypeConfiguration<SupplierResponse>
{
    private const int SUPPLIER_NAME_MAX_LENGTH = 100;
    private const int FAILURE_REASON_MAX_LENGTH = 500;
    private const int CURRENCY_CODE_LENGTH = 3;

    public void Configure(EntityTypeBuilder<SupplierResponse> builder)
    {
        builder.ToTable("SupplierResponses");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SupplierName).HasMaxLength(SUPPLIER_NAME_MAX_LENGTH);
        builder.Property(r => r.Outcome).HasConversion<int>();
        builder.Property(r => r.FailureReason).HasMaxLength(FAILURE_REASON_MAX_LENGTH);

        // SQLite stores decimals as text, which does not sort numerically.
        // Money is kept in integer minor units (cents) so SQL ordering works
        // and no precision is lost.
        builder.ComplexProperty(r => r.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("PriceMinorUnits")
                .HasConversion(MinorUnitsConverter.Instance);
            price.Property(p => p.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(CURRENCY_CODE_LENGTH);
        });

        // Same reasoning for TimeSpan: milliseconds as an integer sort correctly.
        builder.Property(r => r.ResponseTime)
            .HasColumnName("ResponseTimeMs")
            .HasConversion(MillisecondsConverter.Instance);

        builder.Property(r => r.RecordedAt).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasIndex(r => r.SearchId);
        builder.HasIndex(r => new { r.SupplierName, r.RecordedAt });
    }
}
