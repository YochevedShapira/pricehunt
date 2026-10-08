using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PriceHunt.Application.Persistence;
using PriceHunt.Application.Searching;
using PriceHunt.Application.Searching.Events;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Tests.Searching;

public class PriceSearchTests
{
    private const string MAERSK = "Maersk";
    private const string MSC = "MSC";
    private const string UNKNOWN = "Unknown";

    private static readonly Route ROUTE = new("Haifa", "Rotterdam");
    private static readonly DateRange DATES =
        new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10));
    private static readonly SearchRequest REQUEST = new(ROUTE, DATES, [MAERSK, MSC]);
    private static readonly Money PRICE = new(1200m, "USD");
    private static readonly SupplierPriced MAERSK_PRICED = new(MAERSK, PRICE);
    private static readonly SupplierFailure MSC_FAILED = new(MSC, "Down");
    private static readonly TimeSpan CONFIGURED_TIMEOUT = TimeSpan.FromSeconds(4);

    private ISupplierCatalog _catalog = null!;
    private ISupplierFanOut _fanOut = null!;
    private ISearchRepository _repository = null!;
    private FakeTimeProvider _time = null!;
    private PriceSearch _priceSearch = null!;
    private Search? _storedSearch;

    [SetUp]
    public void SetUp()
    {
        var maersk = SupplierNamed(MAERSK);
        var msc = SupplierNamed(MSC);
        _catalog = Substitute.For<ISupplierCatalog>();
        _catalog.Find(MAERSK).Returns(maersk);
        _catalog.Find(MSC).Returns(msc);
        _catalog.Find(UNKNOWN).Returns((ISupplier?)null);

        _fanOut = Substitute.For<ISupplierFanOut>();
        _repository = Substitute.For<ISearchRepository>();
        _repository.AddAsync(Arg.Do<Search>(s => _storedSearch = s), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _time = new FakeTimeProvider();
        _priceSearch = CreatePriceSearch(new SearchOptions { Timeout = CONFIGURED_TIMEOUT });
    }

    [Test]
    public async Task RunAsync_YieldsSearchStartedFirst_WithTheSupplierCount()
    {
        FanOutYields();

        var events = await CollectAsync();

        Assert.That(events[0], Is.EqualTo(new SearchStarted(_storedSearch!.Id, 2)));
    }

    [Test]
    public async Task RunAsync_StoresTheSearch_BeforeQueryingSuppliers()
    {
        FanOutYields();

        await CollectAsync();

        Received.InOrder(() =>
        {
            _repository.AddAsync(Arg.Any<Search>(), Arg.Any<CancellationToken>());
            _fanOut.QueryAsync(
                Arg.Any<IReadOnlyCollection<ISupplier>>(),
                Arg.Any<PriceRequest>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task RunAsync_ResolvesRequestedSuppliersFromTheCatalog()
    {
        FanOutYields();

        await CollectAsync();

        _ = _fanOut.Received().QueryAsync(
            Arg.Is<IReadOnlyCollection<ISupplier>>(
                s => s.Select(x => x.Name).SequenceEqual(new[] { MAERSK, MSC })),
            Arg.Any<PriceRequest>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_QueriesSuppliersWithTheConfiguredTimeout()
    {
        FanOutYields();

        await CollectAsync();

        _ = _fanOut.Received().QueryAsync(
            Arg.Any<IReadOnlyCollection<ISupplier>>(),
            Arg.Any<PriceRequest>(),
            CONFIGURED_TIMEOUT,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Constructor_Throws_OnNonPositiveTimeout()
    {
        Assert.That(
            () => CreatePriceSearch(new SearchOptions { Timeout = TimeSpan.Zero }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task RunAsync_YieldsPriceReceived_ForAPricedOutcome()
    {
        FanOutYields(MAERSK_PRICED);

        var events = await CollectAsync();

        Assert.That(events[1], Is.EqualTo(new PriceReceived(MAERSK, PRICE, TimeSpan.Zero)));
    }

    [Test]
    public async Task RunAsync_YieldsSupplierFailed_ForAFailureOutcome()
    {
        FanOutYields(MSC_FAILED);

        var events = await CollectAsync();

        Assert.That(events[1], Is.EqualTo(new SupplierFailed(MSC, "Down", TimeSpan.Zero)));
    }

    [Test]
    public async Task RunAsync_RecordsEachOutcomeOnTheSearch()
    {
        FanOutYields(MAERSK_PRICED, MSC_FAILED);

        await CollectAsync();

        Assert.That(
            _storedSearch!.Responses.Select(r => r.Outcome),
            Is.EqualTo(new[] { ResponseOutcome.Priced, ResponseOutcome.Failed }));
    }

    [Test]
    public async Task RunAsync_SavesTheSearch_AfterEachOutcomeAndAtTheEnd()
    {
        FanOutYields(MAERSK_PRICED, MSC_FAILED);

        await CollectAsync();

        await _repository.Received(3).SaveAsync(_storedSearch!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_EndsCompleted_WhenEverySupplierAnswered()
    {
        FanOutYields(MAERSK_PRICED, MSC_FAILED);

        var events = await CollectAsync();

        Assert.That(events[^1], Is.EqualTo(new SearchEnded(SearchStatus.Completed, 2)));
    }

    [Test]
    public async Task RunAsync_EndsTimedOut_WhenASupplierDidNotAnswer()
    {
        FanOutYields(MAERSK_PRICED);

        var events = await CollectAsync();

        Assert.That(events[^1], Is.EqualTo(new SearchEnded(SearchStatus.TimedOut, 1)));
    }

    [Test]
    public async Task RunAsync_PersistsTheFinalStatus()
    {
        FanOutYields(MAERSK_PRICED);

        await CollectAsync();

        Assert.That(_storedSearch!.Status, Is.EqualTo(SearchStatus.TimedOut));
    }

    [Test]
    public async Task RunAsync_SavesTheFinalStatusWithoutTheCallerToken()
    {
        using var cancellation = new CancellationTokenSource();
        FanOutYields(MAERSK_PRICED, MSC_FAILED);

        await CollectAsync(cancellation.Token);

        await _repository.Received(1).SaveAsync(Arg.Any<Search>(), CancellationToken.None);
    }

    [Test]
    public async Task RunAsync_EndsCancelled_WhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        FanOutYieldsThenCancels(cancellation, MAERSK_PRICED);

        await SwallowCancellation(CollectAsync(cancellation.Token));

        Assert.That(_storedSearch!.Status, Is.EqualTo(SearchStatus.Cancelled));
    }

    [Test]
    public void RunAsync_ThrowsOperationCanceled_WhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        FanOutYieldsThenCancels(cancellation, MAERSK_PRICED);

        Assert.That(
            async () => await CollectAsync(cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task RunAsync_EndsCancelled_WhenTheConsumerStopsEarly()
    {
        FanOutYields(MAERSK_PRICED, MSC_FAILED);

        await TakeFirstAsync(_priceSearch.RunAsync(REQUEST, CancellationToken.None));

        Assert.That(_storedSearch!.Status, Is.EqualTo(SearchStatus.Cancelled));
    }

    [Test]
    public void RunAsync_Throws_OnUnknownSupplier()
    {
        var request = new SearchRequest(ROUTE, DATES, [MAERSK, UNKNOWN]);

        Assert.That(
            async () => await CollectAsync(request, CancellationToken.None),
            Throws.InstanceOf<ArgumentException>());
    }

    private void FanOutYields(params SupplierOutcome[] outcomes)
    {
        _fanOut.QueryAsync(
                Arg.Any<IReadOnlyCollection<ISupplier>>(),
                Arg.Any<PriceRequest>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Stream(outcomes));
    }

    private void FanOutYieldsThenCancels(
        CancellationTokenSource cancellation,
        params SupplierOutcome[] outcomes)
    {
        _fanOut.QueryAsync(
                Arg.Any<IReadOnlyCollection<ISupplier>>(),
                Arg.Any<PriceRequest>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamThenCancel(cancellation, outcomes));
    }

    private static async IAsyncEnumerable<SupplierOutcome> Stream(SupplierOutcome[] outcomes)
    {
        foreach (var outcome in outcomes)
        {
            await Task.Yield();
            yield return outcome;
        }
    }

    private static async IAsyncEnumerable<SupplierOutcome> StreamThenCancel(
        CancellationTokenSource cancellation,
        SupplierOutcome[] outcomes)
    {
        foreach (var outcome in outcomes)
        {
            await Task.Yield();
            yield return outcome;
        }

        await cancellation.CancelAsync();
        throw new OperationCanceledException(cancellation.Token);
    }

    private Task<List<SearchEvent>> CollectAsync()
    {
        return CollectAsync(REQUEST, CancellationToken.None);
    }

    private Task<List<SearchEvent>> CollectAsync(CancellationToken cancellationToken)
    {
        return CollectAsync(REQUEST, cancellationToken);
    }

    private async Task<List<SearchEvent>> CollectAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        var events = new List<SearchEvent>();
        await foreach (var searchEvent in _priceSearch.RunAsync(request, cancellationToken))
        {
            events.Add(searchEvent);
        }

        return events;
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

    private static async Task<SearchEvent> TakeFirstAsync(IAsyncEnumerable<SearchEvent> stream)
    {
        await foreach (var searchEvent in stream)
        {
            return searchEvent;
        }

        throw new InvalidOperationException("The stream ended without an event.");
    }

    private PriceSearch CreatePriceSearch(SearchOptions options)
    {
        return new PriceSearch(
            _catalog, _fanOut, _repository, _time, options, NullLogger<PriceSearch>.Instance);
    }

    private static ISupplier SupplierNamed(string name)
    {
        var supplier = Substitute.For<ISupplier>();
        supplier.Name.Returns(name);

        return supplier;
    }
}
