using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores a <see cref="DateTimeOffset"/> as a UTC <see cref="DateTime"/>.
/// SQLite has no native date type and the provider cannot translate
/// <see cref="DateTimeOffset"/> comparisons, while UTC <see cref="DateTime"/>
/// text sorts and compares correctly.
/// </summary>
internal static class UtcDateTimeConverter
{
    public static readonly ValueConverter<DateTimeOffset, DateTime> Instance = new(
        moment => moment.UtcDateTime,
        stored => new DateTimeOffset(stored, TimeSpan.Zero));

    public static readonly ValueConverter<DateTimeOffset?, DateTime?> NullableInstance = new(
        moment => moment.HasValue ? moment.Value.UtcDateTime : null,
        stored => stored.HasValue ? new DateTimeOffset(stored.Value, TimeSpan.Zero) : null);
}
