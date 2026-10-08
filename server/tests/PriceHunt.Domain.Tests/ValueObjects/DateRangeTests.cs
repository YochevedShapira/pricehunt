using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    private static readonly DateOnly MARCH_1 = new(2026, 3, 1);
    private static readonly DateOnly MARCH_10 = new(2026, 3, 10);

    [Test]
    public void Constructor_AcceptsRange_WhenFromIsBeforeTo()
    {
        var range = new DateRange(MARCH_1, MARCH_10);

        Assert.That((range.From, range.To), Is.EqualTo((MARCH_1, MARCH_10)));
    }

    [Test]
    public void Constructor_AcceptsRange_WhenFromEqualsTo()
    {
        var range = new DateRange(MARCH_1, MARCH_1);

        Assert.That(range.From, Is.EqualTo(range.To));
    }

    [Test]
    public void Constructor_Throws_WhenFromIsAfterTo()
    {
        Assert.That(
            () => new DateRange(MARCH_10, MARCH_1),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Equals_ReturnsTrue_OnSameDates()
    {
        var first = new DateRange(MARCH_1, MARCH_10);
        var second = new DateRange(MARCH_1, MARCH_10);

        Assert.That(first, Is.EqualTo(second));
    }
}
