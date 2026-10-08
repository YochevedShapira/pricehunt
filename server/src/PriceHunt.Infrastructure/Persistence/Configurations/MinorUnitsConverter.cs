using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

/// <summary>
/// Stores a money amount as an integer number of minor units (cents).
/// </summary>
internal static class MinorUnitsConverter
{
    private const int MINOR_UNITS_PER_MAJOR = 100;

    public static readonly ValueConverter<decimal, long> Instance = new(
        amount => decimal.ToInt64(decimal.Round(amount * MINOR_UNITS_PER_MAJOR)),
        minorUnits => minorUnits / (decimal)MINOR_UNITS_PER_MAJOR);
}
