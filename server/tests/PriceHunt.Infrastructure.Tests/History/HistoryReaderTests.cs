using PriceHunt.Application.History;
using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.History;
using PriceHunt.Infrastructure.Tests.Persistence;

namespace PriceHunt.Infrastructure.Tests.History;

public class HistoryReaderTests
{
    private const string OCEANIC = "Oceanic";
    private const string BLUEWAVE = "BlueWave";

    private static readonly DateRange DATES =
        new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 10));
    private static readonly DateTimeOffset MARCH_1 =
        new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MARCH_5 =
        new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    private SqliteTestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new SqliteTestDatabase();
        await SeedAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task ReadAsync_ReturnsEveryResponse_WithoutFilters()
    {
        var page = await ReadAsync(Query());

        Assert.That(page.TotalCount, Is.EqualTo(4));
    }

    [Test]
    public async Task ReadAsync_FlattensTheRouteIntoEachEntry()
    {
        var page = await ReadAsync(Query(fromLocation: "Ashdod"));

        Assert.That(
            page.Items.Select(e => (e.FromLocation, e.ToLocation)).Distinct().Single(),
            Is.EqualTo(("Ashdod", "Hamburg")));
    }

    [Test]
    public async Task ReadAsync_FiltersBySupplier()
    {
        var page = await ReadAsync(Query(suppliers: [BLUEWAVE]));

        Assert.That(page.Items.Select(e => e.SupplierName), Has.All.EqualTo(BLUEWAVE));
    }

    [Test]
    public async Task ReadAsync_FiltersByStartDate()
    {
        var page = await ReadAsync(Query(startDate: new DateOnly(2026, 3, 5)));

        Assert.That(page.Items.Select(e => e.RecordedAt.Date), Has.All.EqualTo(MARCH_5.Date));
    }

    [Test]
    public async Task ReadAsync_FiltersByEndDate_Inclusively()
    {
        var page = await ReadAsync(Query(endDate: new DateOnly(2026, 3, 1)));

        Assert.That(page.Items.Select(e => e.RecordedAt.Date), Has.All.EqualTo(MARCH_1.Date));
    }

    [Test]
    public async Task ReadAsync_FiltersByFromLocation_IgnoringCaseAndMatchingAnywhere()
    {
        var page = await ReadAsync(Query(fromLocation: "SHDO"));

        Assert.That(page.Items.Select(e => e.FromLocation), Has.All.EqualTo("Ashdod"));
    }

    [Test]
    public async Task ReadAsync_FiltersByToLocation()
    {
        var page = await ReadAsync(Query(toLocation: "Rotterdam"));

        Assert.That(page.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ReadAsync_SortsByPriceDescending_WithUnpricedRowsLast()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.Price, direction: SortDirection.Descending));

        Assert.That(
            page.Items.Select(e => e.Price?.Amount),
            Is.EqualTo(new decimal?[] { 2000m, 1000m, 500m, null }));
    }

    [Test]
    public async Task ReadAsync_SortsByPriceAscending_WithUnpricedRowsStillLast()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.Price, direction: SortDirection.Ascending));

        Assert.That(
            page.Items.Select(e => e.Price?.Amount),
            Is.EqualTo(new decimal?[] { 500m, 1000m, 2000m, null }));
    }

    [Test]
    public async Task ReadAsync_SortsByDateAscending()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.Date, direction: SortDirection.Ascending));

        Assert.That(page.Items.Select(e => e.RecordedAt), Is.Ordered.Ascending);
    }

    [Test]
    public async Task ReadAsync_SortsByResponseTime()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.ResponseTime, direction: SortDirection.Ascending));

        Assert.That(page.Items.Select(e => e.ResponseTime), Is.Ordered.Ascending);
    }

    [Test]
    public async Task ReadAsync_SortsBySupplier()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.Supplier, direction: SortDirection.Ascending));

        Assert.That(page.Items.Select(e => e.SupplierName), Is.Ordered.Ascending);
    }

    [Test]
    public async Task ReadAsync_SortsByRoute()
    {
        var page = await ReadAsync(Query(sortBy: HistorySortColumn.Route, direction: SortDirection.Ascending));

        Assert.That(page.Items.Select(e => e.FromLocation), Is.Ordered.Ascending);
    }

    [Test]
    public async Task ReadAsync_ReturnsTheRequestedPage()
    {
        var page = await ReadAsync(Query(page: 2, pageSize: 3));

        Assert.That((page.Items.Count, page.TotalCount, page.Page), Is.EqualTo((1, 4, 2)));
    }

    [Test]
    public async Task ReadAsync_KeepsTheOutcome()
    {
        var page = await ReadAsync(Query(toLocation: "Rotterdam", suppliers: [BLUEWAVE]));

        Assert.That(page.Items.Single().Outcome, Is.EqualTo(ResponseOutcome.Failed));
    }

    private static HistoryQuery Query(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        IReadOnlyCollection<string>? suppliers = null,
        string? fromLocation = null,
        string? toLocation = null,
        HistorySortColumn sortBy = HistorySortColumn.Date,
        SortDirection direction = SortDirection.Descending,
        int page = 1,
        int pageSize = 20)
    {
        return new HistoryQuery(
            startDate, endDate, suppliers ?? [], fromLocation, toLocation,
            sortBy, direction, page, pageSize);
    }

    private async Task<HistoryPage> ReadAsync(HistoryQuery query)
    {
        using var db = _database.CreateContext();

        return await new HistoryReader(db).ReadAsync(query, CancellationToken.None);
    }

    /// <summary>
    /// Two searches, four responses:
    /// Haifa→Rotterdam on March 1: Oceanic 1000 (1 s), BlueWave failed (2 s).
    /// Ashdod→Hamburg on March 5: Oceanic 500 (3 s), BlueWave 2000 (1 s).
    /// </summary>
    private async Task SeedAsync()
    {
        var first = Search.Start(new Route("Haifa", "Rotterdam"), DATES, [OCEANIC, BLUEWAVE], MARCH_1);
        first.RecordPrice(OCEANIC, new Money(1000m, "USD"), MARCH_1.AddSeconds(1));
        first.RecordFailure(BLUEWAVE, "Down", MARCH_1.AddSeconds(2));
        first.End(SearchStatus.Completed, MARCH_1.AddSeconds(2));

        var second = Search.Start(new Route("Ashdod", "Hamburg"), DATES, [OCEANIC, BLUEWAVE], MARCH_5);
        second.RecordPrice(BLUEWAVE, new Money(2000m, "USD"), MARCH_5.AddSeconds(1));
        second.RecordPrice(OCEANIC, new Money(500m, "USD"), MARCH_5.AddSeconds(3));
        second.End(SearchStatus.Completed, MARCH_5.AddSeconds(3));

        using var db = _database.CreateContext();
        db.Searches.AddRange(first, second);
        await db.SaveChangesAsync();
    }
}
