using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Tests.Entities;

public class SearchTests
{
    private const string MAERSK = "Maersk";
    private const string MSC = "MSC";
    private const string UNKNOWN = "Unknown";

    private static readonly Route ROUTE = new("Haifa", "Rotterdam");
    private static readonly DateRange DATES =
        new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10));
    private static readonly DateTimeOffset STARTED_AT =
        new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ONE_SECOND_LATER =
        STARTED_AT.AddSeconds(1);
    private static readonly DateTimeOffset SIX_SECONDS_LATER =
        STARTED_AT.AddSeconds(6);
    private static readonly Money PRICE = new(1200m, "USD");

    private Search _search = null!;

    [SetUp]
    public void SetUp()
    {
        _search = Search.Start(ROUTE, DATES, [MAERSK, MSC], STARTED_AT);
    }

    [Test]
    public void Start_AssignsNonEmptyId()
    {
        Assert.That(_search.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Start_SetsStatusToRunning()
    {
        Assert.That(_search.Status, Is.EqualTo(SearchStatus.Running));
    }

    [Test]
    public void Start_HasNoEndTime()
    {
        Assert.That(_search.EndedAt, Is.Null);
    }

    [Test]
    public void Start_KeepsRequestedSuppliers()
    {
        Assert.That(_search.RequestedSuppliers, Is.EqualTo(new[] { MAERSK, MSC }));
    }

    [Test]
    public void Start_Throws_OnNullRoute()
    {
        Assert.That(
            () => Search.Start(null!, DATES, [MAERSK], STARTED_AT),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Start_Throws_OnNullDates()
    {
        Assert.That(
            () => Search.Start(ROUTE, null!, [MAERSK], STARTED_AT),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Start_Throws_OnEmptySupplierList()
    {
        Assert.That(
            () => Search.Start(ROUTE, DATES, [], STARTED_AT),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Start_Throws_OnDuplicateSupplier()
    {
        Assert.That(
            () => Search.Start(ROUTE, DATES, [MAERSK, MAERSK], STARTED_AT),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Start_Throws_OnBlankSupplierName()
    {
        Assert.That(
            () => Search.Start(ROUTE, DATES, [MAERSK, " "], STARTED_AT),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void RecordPrice_AddsPricedResponse()
    {
        _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        Assert.That(
            _search.Responses.Single().Outcome,
            Is.EqualTo(ResponseOutcome.Priced));
    }

    [Test]
    public void RecordPrice_ComputesResponseTimeFromStart()
    {
        var response = _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        Assert.That(response.ResponseTime, Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void RecordPrice_LinksResponseToSearch()
    {
        var response = _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        Assert.That(response.SearchId, Is.EqualTo(_search.Id));
    }

    [Test]
    public void RecordPrice_Throws_OnSupplierThatWasNotRequested()
    {
        Assert.That(
            () => _search.RecordPrice(UNKNOWN, PRICE, ONE_SECOND_LATER),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void RecordPrice_Throws_WhenSupplierAlreadyResponded()
    {
        _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        Assert.That(
            () => _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER),
            Throws.InvalidOperationException);
    }

    [Test]
    public void RecordPrice_Throws_WhenSearchHasEnded()
    {
        _search.End(SearchStatus.Completed, SIX_SECONDS_LATER);

        Assert.That(
            () => _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER),
            Throws.InvalidOperationException);
    }

    [Test]
    public void RecordFailure_AddsFailedResponse()
    {
        _search.RecordFailure(MAERSK, "Unavailable", ONE_SECOND_LATER);

        Assert.That(
            _search.Responses.Single().Outcome,
            Is.EqualTo(ResponseOutcome.Failed));
    }

    [Test]
    public void RecordFailure_Throws_OnSupplierThatWasNotRequested()
    {
        Assert.That(
            () => _search.RecordFailure(UNKNOWN, "Unavailable", ONE_SECOND_LATER),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void End_SetsFinalStatus()
    {
        _search.End(SearchStatus.TimedOut, SIX_SECONDS_LATER);

        Assert.That(_search.Status, Is.EqualTo(SearchStatus.TimedOut));
    }

    [Test]
    public void End_SetsEndTime()
    {
        _search.End(SearchStatus.Completed, SIX_SECONDS_LATER);

        Assert.That(_search.EndedAt, Is.EqualTo(SIX_SECONDS_LATER));
    }

    [Test]
    public void End_RecordsNoResponseForEveryUnansweredSupplier()
    {
        _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        _search.End(SearchStatus.TimedOut, SIX_SECONDS_LATER);

        var unanswered = _search.Responses.Single(r => r.SupplierName == MSC);
        Assert.That(unanswered.Outcome, Is.EqualTo(ResponseOutcome.NoResponse));
    }

    [Test]
    public void End_GivesUnansweredSuppliersTheFullSearchDuration()
    {
        _search.End(SearchStatus.TimedOut, SIX_SECONDS_LATER);

        Assert.That(
            _search.Responses.Select(r => r.ResponseTime),
            Has.All.EqualTo(TimeSpan.FromSeconds(6)));
    }

    [Test]
    public void End_LeavesOneResponsePerRequestedSupplier()
    {
        _search.RecordPrice(MAERSK, PRICE, ONE_SECOND_LATER);

        _search.End(SearchStatus.Completed, SIX_SECONDS_LATER);

        Assert.That(
            _search.Responses.Select(r => r.SupplierName),
            Is.EquivalentTo(new[] { MAERSK, MSC }));
    }

    [Test]
    public void End_Throws_WhenFinalStatusIsRunning()
    {
        Assert.That(
            () => _search.End(SearchStatus.Running, SIX_SECONDS_LATER),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void End_Throws_WhenAlreadyEnded()
    {
        _search.End(SearchStatus.Completed, SIX_SECONDS_LATER);

        Assert.That(
            () => _search.End(SearchStatus.Cancelled, SIX_SECONDS_LATER),
            Throws.InvalidOperationException);
    }

    [Test]
    public void End_Throws_WhenEndTimeIsBeforeStart()
    {
        Assert.That(
            () => _search.End(SearchStatus.Completed, STARTED_AT.AddSeconds(-1)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
