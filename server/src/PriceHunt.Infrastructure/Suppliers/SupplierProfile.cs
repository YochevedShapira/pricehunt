namespace PriceHunt.Infrastructure.Suppliers;

/// <summary>
/// Describes how one simulated supplier behaves: how long it takes, what it
/// charges, how often it fails and whether it answers at all.
/// </summary>
public sealed record SupplierProfile
{
    public SupplierProfile(
        string name,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        decimal minPrice,
        decimal maxPrice,
        double failureRate = 0,
        bool neverResponds = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(minDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, minDelay);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minPrice, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPrice, minPrice);
        ArgumentOutOfRangeException.ThrowIfLessThan(failureRate, 0d);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureRate, 1d);

        Name = name;
        MinDelay = minDelay;
        MaxDelay = maxDelay;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        FailureRate = failureRate;
        NeverResponds = neverResponds;
    }

    public string Name { get; }

    public TimeSpan MinDelay { get; }

    public TimeSpan MaxDelay { get; }

    public decimal MinPrice { get; }

    public decimal MaxPrice { get; }

    /// <summary>
    /// Probability, from 0 to 1, that a call fails after its delay.
    /// </summary>
    public double FailureRate { get; }

    /// <summary>
    /// When true the supplier waits until cancelled and never answers.
    /// </summary>
    public bool NeverResponds { get; }
}
