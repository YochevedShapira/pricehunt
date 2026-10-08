using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores a <see cref="TimeSpan"/> as whole milliseconds.
/// </summary>
internal static class MillisecondsConverter
{
    public static readonly ValueConverter<TimeSpan, long> Instance = new(
        duration => (long)duration.TotalMilliseconds,
        milliseconds => TimeSpan.FromMilliseconds(milliseconds));
}
