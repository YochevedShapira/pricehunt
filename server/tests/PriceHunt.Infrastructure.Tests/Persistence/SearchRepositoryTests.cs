using Microsoft.EntityFrameworkCore;
using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public class SearchRepositoryTests
{
    private const string OCEANIC = "Oceanic";
    private const string BLUEWAVE = "BlueWave";

    private static readonly Route ROUTE = new("Haifa", "Rotterdam");
    private static readonly DateRange DATES =
        new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10));
    private static readonly DateTimeOffset STARTED_AT =
        new(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(2));
    private static readonly Money PRICE = new(1234.56m, "USD");

    private SqliteTestDatabase _database = null!;
    private PriceHuntDbContext _db = null!;
    private SearchRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new SqliteTestDatabase();
        _db = _database.CreateContext();
        _repository = new SearchRepository(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _database.Dispose();
    }

    [Test]
    public async Task AddAsync_StoresTheSearch()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That(await ReloadAsync(search.Id), Is.Not.Null);
    }

    [Test]
    public async Task AddAsync_RoundTripsTheRoute()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).Route, Is.EqualTo(ROUTE));
    }

    [Test]
    public async Task AddAsync_RoundTripsTheDates()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).Dates, Is.EqualTo(DATES));
    }

    [Test]
    public async Task AddAsync_RoundTripsTheRequestedSuppliers()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That(
            (await ReloadAsync(search.Id)).RequestedSuppliers,
            Is.EqualTo(new[] { OCEANIC, BLUEWAVE }));
    }

    [Test]
    public async Task AddAsync_RoundTripsTheStartTimeAsTheSameInstant()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).StartedAt, Is.EqualTo(STARTED_AT));
    }

    [Test]
    public async Task AddAsync_StoresTheSearchAsRunning()
    {
        var search = NewSearch();

        await _repository.AddAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).Status, Is.EqualTo(SearchStatus.Running));
    }

    [Test]
    public async Task SaveAsync_PersistsARecordedPrice()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.RecordPrice(OCEANIC, PRICE, STARTED_AT.AddSeconds(1.5));

        await _repository.SaveAsync(search, CancellationToken.None);

        var response = (await ReloadAsync(search.Id)).Responses.Single();
        Assert.That(response.Price, Is.EqualTo(PRICE));
    }

    [Test]
    public async Task SaveAsync_PersistsTheResponseTime()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.RecordPrice(OCEANIC, PRICE, STARTED_AT.AddSeconds(1.5));

        await _repository.SaveAsync(search, CancellationToken.None);

        var response = (await ReloadAsync(search.Id)).Responses.Single();
        Assert.That(response.ResponseTime, Is.EqualTo(TimeSpan.FromSeconds(1.5)));
    }

    [Test]
    public async Task SaveAsync_PersistsARecordedFailure()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.RecordFailure(OCEANIC, "Down", STARTED_AT.AddSeconds(1));

        await _repository.SaveAsync(search, CancellationToken.None);

        var response = (await ReloadAsync(search.Id)).Responses.Single();
        Assert.That((response.Outcome, response.FailureReason, response.Price),
            Is.EqualTo((ResponseOutcome.Failed, "Down", (Money?)null)));
    }

    [Test]
    public async Task SaveAsync_PersistsTheFinalStatus()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.End(SearchStatus.TimedOut, STARTED_AT.AddSeconds(6));

        await _repository.SaveAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).Status, Is.EqualTo(SearchStatus.TimedOut));
    }

    [Test]
    public async Task SaveAsync_PersistsTheEndTime()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.End(SearchStatus.Completed, STARTED_AT.AddSeconds(6));

        await _repository.SaveAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).EndedAt, Is.EqualTo(STARTED_AT.AddSeconds(6)));
    }

    [Test]
    public async Task SaveAsync_PersistsTheNoResponseRowsCreatedAtTheEnd()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.End(SearchStatus.TimedOut, STARTED_AT.AddSeconds(6));

        await _repository.SaveAsync(search, CancellationToken.None);

        Assert.That(
            (await ReloadAsync(search.Id)).Responses.Select(r => r.Outcome),
            Is.EqualTo(new[] { ResponseOutcome.NoResponse, ResponseOutcome.NoResponse }));
    }

    [Test]
    public async Task SaveAsync_PersistsIncrementally_AcrossSeveralSaves()
    {
        var search = NewSearch();
        await _repository.AddAsync(search, CancellationToken.None);
        search.RecordPrice(OCEANIC, PRICE, STARTED_AT.AddSeconds(1));
        await _repository.SaveAsync(search, CancellationToken.None);
        search.RecordFailure(BLUEWAVE, "Down", STARTED_AT.AddSeconds(2));

        await _repository.SaveAsync(search, CancellationToken.None);

        Assert.That((await ReloadAsync(search.Id)).Responses, Has.Count.EqualTo(2));
    }

    [Test]
    public void SaveAsync_Throws_WhenTheSearchWasNeverAdded()
    {
        Assert.That(
            () => _repository.SaveAsync(NewSearch(), CancellationToken.None),
            Throws.InvalidOperationException);
    }

    private static Search NewSearch()
    {
        return Search.Start(ROUTE, DATES, [OCEANIC, BLUEWAVE], STARTED_AT);
    }

    private async Task<Search> ReloadAsync(Guid id)
    {
        using var freshContext = _database.CreateContext();

        return await freshContext.Searches
            .Include(s => s.Responses)
            .SingleAsync(s => s.Id == id);
    }
}
