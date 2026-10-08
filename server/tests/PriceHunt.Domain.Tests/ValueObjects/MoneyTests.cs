using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Test]
    public void Constructor_NormalisesCurrencyToUpperCase_OnLowerCaseInput()
    {
        var money = new Money(100m, " usd ");

        Assert.That(money.Currency, Is.EqualTo("USD"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_Throws_OnNonPositiveAmount(decimal amount)
    {
        Assert.That(
            () => new Money(amount, "USD"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("US")]
    [TestCase("USDX")]
    public void Constructor_Throws_OnInvalidCurrencyCode(string? currency)
    {
        Assert.That(
            () => new Money(100m, currency!),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Equals_ReturnsTrue_OnSameAmountAndCurrency()
    {
        var first = new Money(100m, "USD");
        var second = new Money(100m, "usd");

        Assert.That(first, Is.EqualTo(second));
    }
}
