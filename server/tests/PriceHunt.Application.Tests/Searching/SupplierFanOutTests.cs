using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PriceHunt.Application.Searching;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Tests.Searching;

public class SupplierFanOutTests
{
    private const string FAST = "Fast";
    private const string SLOW = "Slow";
    private const string BROKEN = "Broken";
    private const string SILENT = "Silent";

    private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan ONE_SECOND = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TWO_SECONDS = TimeSpan.FromSeconds(2);
    private static readonly Money FAST_PRICE = new(1000m, "USD");
    private static readonly Money SLOW_PRICE = new(2000m, "USD");
    private static readonly PriceRequest REQUEST = new(
        new Route("Haifa", "Rotterdam"),
        new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10)));

    private FakeTimeProvider _time = null!;
    private SupplierFanOut _fanOut = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _fanOut = new SupplierFanOut(_time, NullLogger<SupplierFanOut>.Instance);
    }

    [Test]
    public async Task QueryAsync_YieldsPrice_WhenSupplierAnswers()
    {
        var collecting = CollectAsync(AnswersAfter(FAST, ONE_SECOND, FAST_PRICE));

        _time.Advance(ONE_SECOND);
        var outcomes = await collecting;

        Assert.That(outcomes.Single(), Is.EqualTo(new SupplierPriced(FAST, FAST_PRICE)));
    }

    [Test]
    public async Task QueryAsync_YieldsOutcomesInArrivalOrder_NotInSupplierOrder()
    {
        var collecting = CollectAsync(
            AnswersAfter(SLOW, TWO_SECONDS, SLOW_PRICE),
            AnswersAfter(FAST, ONE_SECOND, FAST_PRICE));

        _time.Advance(ONE_SECOND);
        _time.Advance(ONE_SECOND);
        var outcomes = await collecting;

        Assert.That(
            outcomes.Select(o => o.SupplierName),
            Is.EqualTo(new[] { FAST, SLOW }));
    }

    [Test]
    public async Task QueryAsync_YieldsFailure_WhenSupplierIsUnavailable()
    {
        var collecting = CollectAsync(FailsAfter(
            BROKEN, ONE_SECOND, new SupplierUnavailableException(BROKEN, "Down")));

        _time.Advance(ONE_SECOND);
        var outcomes = await collecting;

        Assert.That(outcomes.Single(), Is.EqualTo(new SupplierFailure(BROKEN, "Down")));
    }

    [Test]
    public async Task QueryAsync_YieldsFailure_WhenSupplierThrowsUnexpectedly()
    {
        var collecting = CollectAsync(FailsAfter(
            BROKEN, ONE_SECOND, new InvalidOperationException("bug")));

        _time.Advance(ONE_SECOND);
        var outcomes = await collecting;

        Assert.That(outcomes.Single(), Is.InstanceOf<SupplierFailure>());
    }

    [Test]
    public async Task QueryAsync_StillYieldsOtherSuppliers_WhenOneFails()
    {
        var collecting = CollectAsync(
            FailsAfter(BROKEN, ONE_SECOND, new SupplierUnavailableException(BROKEN, "Down")),
            AnswersAfter(SLOW, TWO_SECONDS, SLOW_PRICE));

        _time.Advance(TWO_SECONDS);
        var outcomes = await collecting;

        Assert.That(
            outcomes.Select(o => o.SupplierName),
            Is.EquivalentTo(new[] { BROKEN, SLOW }));
    }

    [Test]
    public async Task QueryAsync_EndsWithoutTheSilentSupplier_WhenTimeoutElapses()
    {
        var collecting = CollectAsync(
            AnswersAfter(FAST, ONE_SECOND, FAST_PRICE),
            NeverAnswers(SILENT));

        _time.Advance(TIMEOUT);
        var outcomes = await collecting;

        Assert.That(outcomes.Select(o => o.SupplierName), Is.EqualTo(new[] { FAST }));
    }

    [Test]
    public async Task QueryAsync_CancelsTheSupplierCall_WhenTimeoutElapses()
    {
        var silent = NeverAnswers(SILENT);
        var collecting = CollectAsync(silent);

        _time.Advance(TIMEOUT);
        await collecting;

        Assert.That(TokenGivenTo(silent).IsCancellationRequested, Is.True);
    }

    [Test]
    public async Task QueryAsync_DropsOutcome_ThatArrivesAfterTimeout()
    {
        var collecting = CollectAsync(IgnoresCancellationAndAnswersAfter(
            SLOW, TIMEOUT + ONE_SECOND, SLOW_PRICE));

        _time.Advance(TIMEOUT);
        _time.Advance(ONE_SECOND);
        var outcomes = await collecting;

        Assert.That(outcomes, Is.Empty);
    }

    [Test]
    public void QueryAsync_ThrowsOperationCanceled_WhenCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        var collecting = CollectAsync(cancellation.Token, NeverAnswers(SILENT));

        cancellation.Cancel();

        Assert.That(
            async () => await collecting,
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task QueryAsync_CancelsTheSupplierCall_WhenCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        var silent = NeverAnswers(SILENT);
        var collecting = CollectAsync(cancellation.Token, silent);

        cancellation.Cancel();
        await SwallowCancellation(collecting);

        Assert.That(TokenGivenTo(silent).IsCancellationRequested, Is.True);
    }

    [Test]
    public async Task QueryAsync_CancelsRemainingSupplierCalls_WhenConsumerStopsEarly()
    {
        var silent = NeverAnswers(SILENT);
        var stream = _fanOut.QueryAsync(
            [AnswersAfter(FAST, ONE_SECOND, FAST_PRICE), silent],
            REQUEST,
            TIMEOUT,
            CancellationToken.None);
        var takingFirst = TakeFirstAsync(stream);

        _time.Advance(ONE_SECOND);
        await takingFirst;

        Assert.That(TokenGivenTo(silent).IsCancellationRequested, Is.True);
    }

    [Test]
    public void QueryAsync_Throws_OnNonPositiveTimeout()
    {
        Assert.That(
            async () => await CollectAsync(TimeSpan.Zero, CancellationToken.None),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task QueryAsync_YieldsNothing_OnEmptySupplierList()
    {
        var outcomes = await CollectAsync();

        Assert.That(outcomes, Is.Empty);
    }

    private Task<List<SupplierOutcome>> CollectAsync(params ISupplier[] suppliers)
    {
        return CollectAsync(CancellationToken.None, suppliers);
    }

    private Task<List<SupplierOutcome>> CollectAsync(
        CancellationToken cancellationToken,
        params ISupplier[] suppliers)
    {
        return CollectAsync(TIMEOUT, cancellationToken, suppliers);
    }

    private async Task<List<SupplierOutcome>> CollectAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken,
        params ISupplier[] suppliers)
    {
        var outcomes = new List<SupplierOutcome>();
        await foreach (var outcome in _fanOut.QueryAsync(
            suppliers, REQUEST, timeout, cancellationToken))
        {
            outcomes.Add(outcome);
        }

        return outcomes;
    }

    private static async Task<SupplierOutcome> TakeFirstAsync(
        IAsyncEnumerable<SupplierOutcome> stream)
    {
        await foreach (var outcome in stream)
        {
            return outcome;
        }

        throw new InvalidOperationException("The stream ended without an outcome.");
    }

    private static async Task SwallowCancellation(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static CancellationToken TokenGivenTo(ISupplier supplier)
    {
        return supplier.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(ISupplier.GetPriceAsync))
            .GetArguments()
            .OfType<CancellationToken>()
            .Single();
    }

    private ISupplier AnswersAfter(string name, TimeSpan delay, Money price)
    {
        var supplier = Named(name);
        supplier.GetPriceAsync(Arg.Any<PriceRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(delay, _time, call.Arg<CancellationToken>());
                return price;
            });

        return supplier;
    }

    private ISupplier IgnoresCancellationAndAnswersAfter(string name, TimeSpan delay, Money price)
    {
        var supplier = Named(name);
        supplier.GetPriceAsync(Arg.Any<PriceRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(delay, _time, CancellationToken.None);
                return price;
            });

        return supplier;
    }

    private ISupplier FailsAfter(string name, TimeSpan delay, Exception exception)
    {
        var supplier = Named(name);
        supplier.GetPriceAsync(Arg.Any<PriceRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<Money>>(async call =>
            {
                await Task.Delay(delay, _time, call.Arg<CancellationToken>());
                throw exception;
            });

        return supplier;
    }

    private ISupplier NeverAnswers(string name)
    {
        var supplier = Named(name);
        supplier.GetPriceAsync(Arg.Any<PriceRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, _time, call.Arg<CancellationToken>());
                return FAST_PRICE;
            });

        return supplier;
    }

    private static ISupplier Named(string name)
    {
        var supplier = Substitute.For<ISupplier>();
        supplier.Name.Returns(name);

        return supplier;
    }
}
