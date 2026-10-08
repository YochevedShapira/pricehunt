using PriceHunt.Infrastructure.Suppliers;

namespace PriceHunt.Infrastructure.Tests.Suppliers;

public class SupplierProfileTests
{
    private static readonly TimeSpan HALF_SECOND = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan FIVE_SECONDS = TimeSpan.FromSeconds(5);

    [Test]
    public void Constructor_AcceptsValidProfile()
    {
        var profile = new SupplierProfile("Oceanic", HALF_SECOND, FIVE_SECONDS, 500m, 5000m);

        Assert.That(profile.Name, Is.EqualTo("Oceanic"));
    }

    [Test]
    public void Constructor_DefaultsToNeverFailingAndAlwaysResponding()
    {
        var profile = new SupplierProfile("Oceanic", HALF_SECOND, FIVE_SECONDS, 500m, 5000m);

        Assert.That((profile.FailureRate, profile.NeverResponds), Is.EqualTo((0d, false)));
    }

    [TestCase(null)]
    [TestCase(" ")]
    public void Constructor_Throws_OnBlankName(string? name)
    {
        Assert.That(
            () => new SupplierProfile(name!, HALF_SECOND, FIVE_SECONDS, 500m, 5000m),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_Throws_OnNegativeMinDelay()
    {
        Assert.That(
            () => new SupplierProfile("Oceanic", TimeSpan.FromSeconds(-1), FIVE_SECONDS, 500m, 5000m),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_Throws_WhenMaxDelayIsBelowMinDelay()
    {
        Assert.That(
            () => new SupplierProfile("Oceanic", FIVE_SECONDS, HALF_SECOND, 500m, 5000m),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_Throws_OnNonPositiveMinPrice()
    {
        Assert.That(
            () => new SupplierProfile("Oceanic", HALF_SECOND, FIVE_SECONDS, 0m, 5000m),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_Throws_WhenMaxPriceIsBelowMinPrice()
    {
        Assert.That(
            () => new SupplierProfile("Oceanic", HALF_SECOND, FIVE_SECONDS, 5000m, 500m),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    public void Constructor_Throws_OnFailureRateOutsideZeroToOne(double failureRate)
    {
        Assert.That(
            () => new SupplierProfile(
                "Oceanic", HALF_SECOND, FIVE_SECONDS, 500m, 5000m, failureRate),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
