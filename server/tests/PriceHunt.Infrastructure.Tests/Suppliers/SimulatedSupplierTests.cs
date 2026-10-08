using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.Suppliers;

namespace PriceHunt.Infrastructure.Tests.Suppliers;

public class SimulatedSupplierTests
{
    private const string NAME = "Oceanic";
    private const double FAILURE_RATE = 0.3;
    private const decimal MIN_PRICE = 1000m;
    private const decimal MAX_PRICE = 3000m;

    private static readonly TimeSpan MIN_DELAY = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MAX_DELAY = TimeSpan.FromSeconds(5);
    private static readonly PriceRequest REQUEST = new(
        new Route("Haifa", "Rotterdam"),
        new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10)));

    private FakeTimeProvider _time = null!;
    private Random _random = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _random = Substitute.For<Random>();
    }

    [Test]
    public void Name_ComesFromTheProfile()
    {
        var supplier = CreateSupplier(Responding());

        Assert.That(supplier.Name, Is.EqualTo(NAME));
    }

    [Test]
    public void GetPriceAsync_DoesNotAnswerBeforeTheDrawnDelay()
    {
        DrawsDelayThenFailureThenPrice(delay: 0.5, failure: 0.9, price: 0.5);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(TimeSpan.FromSeconds(2.9));

        Assert.That(pricing.IsCompleted, Is.False);
    }

    [Test]
    public async Task GetPriceAsync_AnswersAtTheDrawnDelay()
    {
        DrawsDelayThenFailureThenPrice(delay: 0.5, failure: 0.9, price: 0.5);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(TimeSpan.FromSeconds(3));
        await pricing;

        Assert.That(pricing.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task GetPriceAsync_ReturnsPriceInterpolatedInsideTheProfileRange()
    {
        DrawsDelayThenFailureThenPrice(delay: 0, failure: 0.9, price: 0.25);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(MIN_DELAY);
        var price = await pricing;

        Assert.That(price.Amount, Is.EqualTo(1500m));
    }

    [Test]
    public async Task GetPriceAsync_ReturnsPriceInUsDollars()
    {
        DrawsDelayThenFailureThenPrice(delay: 0, failure: 0.9, price: 0.25);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(MIN_DELAY);
        var price = await pricing;

        Assert.That(price.Currency, Is.EqualTo("USD"));
    }

    [Test]
    public void GetPriceAsync_ThrowsSupplierUnavailable_WhenTheFailureDrawIsBelowTheRate()
    {
        DrawsDelayThenFailureThenPrice(delay: 0, failure: 0.29, price: 0.5);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(MIN_DELAY);

        Assert.That(
            async () => await pricing,
            Throws.InstanceOf<SupplierUnavailableException>());
    }

    [Test]
    public async Task GetPriceAsync_Answers_WhenTheFailureDrawIsAtTheRate()
    {
        DrawsDelayThenFailureThenPrice(delay: 0, failure: 0.3, price: 0.5);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(MIN_DELAY);
        await pricing;

        Assert.That(pricing.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public void GetPriceAsync_ThrowsOperationCanceled_WhenCancelledDuringTheDelay()
    {
        using var cancellation = new CancellationTokenSource();
        DrawsDelayThenFailureThenPrice(delay: 0.5, failure: 0.9, price: 0.5);
        var pricing = CreateSupplier(Responding()).GetPriceAsync(REQUEST, cancellation.Token);

        cancellation.Cancel();

        Assert.That(
            async () => await pricing,
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void GetPriceAsync_NeverAnswers_WhenTheProfileNeverResponds()
    {
        var pricing = CreateSupplier(NeverResponding()).GetPriceAsync(REQUEST, CancellationToken.None);

        _time.Advance(TimeSpan.FromHours(1));

        Assert.That(pricing.IsCompleted, Is.False);
    }

    [Test]
    public void GetPriceAsync_ThrowsOperationCanceled_WhenCancelledWhileNeverResponding()
    {
        using var cancellation = new CancellationTokenSource();
        var pricing = CreateSupplier(NeverResponding()).GetPriceAsync(REQUEST, cancellation.Token);

        cancellation.Cancel();

        Assert.That(
            async () => await pricing,
            Throws.InstanceOf<OperationCanceledException>());
    }

    private void DrawsDelayThenFailureThenPrice(double delay, double failure, double price)
    {
        _random.NextDouble().Returns(delay, failure, price);
    }

    private SimulatedSupplier CreateSupplier(SupplierProfile profile)
    {
        return new SimulatedSupplier(profile, _time, _random);
    }

    private static SupplierProfile Responding()
    {
        return new SupplierProfile(NAME, MIN_DELAY, MAX_DELAY, MIN_PRICE, MAX_PRICE, FAILURE_RATE);
    }

    private static SupplierProfile NeverResponding()
    {
        return new SupplierProfile(
            NAME, MIN_DELAY, MAX_DELAY, MIN_PRICE, MAX_PRICE, neverResponds: true);
    }
}
