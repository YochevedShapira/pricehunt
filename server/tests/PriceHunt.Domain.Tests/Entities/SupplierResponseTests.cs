using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Tests.Entities;

public class SupplierResponseTests
{
    private const string SUPPLIER = "Maersk";
    private static readonly Guid SEARCH_ID = Guid.NewGuid();
    private static readonly Money PRICE = new(1200m, "USD");
    private static readonly TimeSpan RESPONSE_TIME = TimeSpan.FromSeconds(1.5);
    private static readonly DateTimeOffset RECORDED_AT =
        new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void Quoted_SetsOutcomeToQuoted()
    {
        var response = SupplierResponse.Quoted(
            SEARCH_ID, SUPPLIER, PRICE, RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Outcome, Is.EqualTo(ResponseOutcome.Quoted));
    }

    [Test]
    public void Quoted_KeepsPrice()
    {
        var response = SupplierResponse.Quoted(
            SEARCH_ID, SUPPLIER, PRICE, RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Price, Is.EqualTo(PRICE));
    }

    [Test]
    public void Quoted_Throws_OnNullPrice()
    {
        Assert.That(
            () => SupplierResponse.Quoted(
                SEARCH_ID, SUPPLIER, null!, RESPONSE_TIME, RECORDED_AT),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Failed_SetsOutcomeToFailed()
    {
        var response = SupplierResponse.Failed(
            SEARCH_ID, SUPPLIER, "Timeout", RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Outcome, Is.EqualTo(ResponseOutcome.Failed));
    }

    [Test]
    public void Failed_KeepsFailureReason()
    {
        var response = SupplierResponse.Failed(
            SEARCH_ID, SUPPLIER, "Timeout", RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.FailureReason, Is.EqualTo("Timeout"));
    }

    [Test]
    public void Failed_HasNoPrice()
    {
        var response = SupplierResponse.Failed(
            SEARCH_ID, SUPPLIER, "Timeout", RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Price, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Failed_Throws_OnBlankReason(string? reason)
    {
        Assert.That(
            () => SupplierResponse.Failed(
                SEARCH_ID, SUPPLIER, reason!, RESPONSE_TIME, RECORDED_AT),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void NoResponse_SetsOutcomeToNoResponse()
    {
        var response = SupplierResponse.NoResponse(
            SEARCH_ID, SUPPLIER, RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Outcome, Is.EqualTo(ResponseOutcome.NoResponse));
    }

    [Test]
    public void NoResponse_HasNoPrice()
    {
        var response = SupplierResponse.NoResponse(
            SEARCH_ID, SUPPLIER, RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.Price, Is.Null);
    }

    [Test]
    public void NoResponse_HasNoFailureReason()
    {
        var response = SupplierResponse.NoResponse(
            SEARCH_ID, SUPPLIER, RESPONSE_TIME, RECORDED_AT);

        Assert.That(response.FailureReason, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Quoted_Throws_OnBlankSupplierName(string? supplier)
    {
        Assert.That(
            () => SupplierResponse.Quoted(
                SEARCH_ID, supplier!, PRICE, RESPONSE_TIME, RECORDED_AT),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Quoted_Throws_OnNegativeResponseTime()
    {
        Assert.That(
            () => SupplierResponse.Quoted(
                SEARCH_ID, SUPPLIER, PRICE, TimeSpan.FromSeconds(-1), RECORDED_AT),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
