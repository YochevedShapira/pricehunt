namespace PriceHunt.Infrastructure.Suppliers;

/// <summary>
/// The seven suppliers from the brief. All answer after 0.5 to 5 seconds;
/// one fails about 30% of the time and one never responds.
/// </summary>
public static class SimulatedSupplierProfiles
{
    private static readonly TimeSpan MIN_DELAY = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan MAX_DELAY = TimeSpan.FromSeconds(5);
    private const decimal MIN_PRICE = 800m;
    private const decimal MAX_PRICE = 4000m;
    private const double FLAKY_FAILURE_RATE = 0.3;

    public static IReadOnlyList<SupplierProfile> All { get; } =
    [
        Reliable("Oceanic Freight"),
        Reliable("BlueWave Shipping"),
        Reliable("Nordic Cargo Lines"),
        Reliable("TransGlobal Logistics"),
        Reliable("Atlas Sea Freight"),
        new("Harbor Express", MIN_DELAY, MAX_DELAY, MIN_PRICE, MAX_PRICE, FLAKY_FAILURE_RATE),
        new("Meridian Carriers", MIN_DELAY, MAX_DELAY, MIN_PRICE, MAX_PRICE, neverResponds: true),
    ];

    private static SupplierProfile Reliable(string name)
    {
        return new SupplierProfile(name, MIN_DELAY, MAX_DELAY, MIN_PRICE, MAX_PRICE);
    }
}
