using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Tests.ValueObjects;

public class RouteTests
{
    [Test]
    public void Constructor_TrimsLocations_OnPaddedInput()
    {
        var route = new Route("  Haifa ", " Rotterdam  ");

        Assert.That(
            (route.FromLocation, route.ToLocation),
            Is.EqualTo(("Haifa", "Rotterdam")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Throws_OnBlankFromLocation(string? from)
    {
        Assert.That(
            () => new Route(from!, "Rotterdam"),
            Throws.InstanceOf<ArgumentException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Throws_OnBlankToLocation(string? to)
    {
        Assert.That(
            () => new Route("Haifa", to!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_Throws_WhenFromAndToAreTheSameIgnoringCase()
    {
        Assert.That(
            () => new Route("Haifa", "haifa"),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Equals_ReturnsTrue_OnSameLocations()
    {
        var first = new Route("Haifa", "Rotterdam");
        var second = new Route("Haifa", "Rotterdam");

        Assert.That(first, Is.EqualTo(second));
    }
}
