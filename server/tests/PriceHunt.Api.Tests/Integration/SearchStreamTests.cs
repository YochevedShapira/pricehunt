using System.Net;
using Microsoft.EntityFrameworkCore;
using PriceHunt.Domain.Entities;
using PriceHunt.Domain.Enums;

namespace PriceHunt.Api.Tests.Integration;

public class SearchStreamTests
{
    private const string VALID_QUERY =
        "fromLocation=Haifa&toLocation=Rotterdam&fromDate=2026-03-01&toDate=2026-03-10";
    private const string STREAM_URL = "/api/searches/stream?" + VALID_QUERY;

    private PriceHuntApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new PriceHuntApiFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Stream_RespondsWithTheEventStreamContentType()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
    }

    [Test]
    public async Task Stream_SendsStartedFirst_WithTheSupplierCount()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        var events = await SseReader.ReadAllAsync(response);

        Assert.That((events[0].Type, events[0].Number("supplierCount")), Is.EqualTo(("started", 4)));
    }

    [Test]
    public async Task Stream_SendsPricesInArrivalOrder()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        var events = await SseReader.ReadAllAsync(response);

        Assert.That(
            events.Where(e => e.Type == "price").Select(e => e.Text("supplierName")),
            Is.EqualTo(new[] { PriceHuntApiFactory.FAST, PriceHuntApiFactory.SLOW }));
    }

    [Test]
    public async Task Stream_SendsAFailedEvent_ForTheBrokenSupplier()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        var events = await SseReader.ReadAllAsync(response);

        var failed = events.Single(e => e.Type == "failed");
        Assert.That(failed.Text("supplierName"), Is.EqualTo(PriceHuntApiFactory.BROKEN));
    }

    [Test]
    public async Task Stream_EndsTimedOut_WhenASupplierNeverAnswers()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        var events = await SseReader.ReadAllAsync(response);

        Assert.That((events[^1].Type, events[^1].Text("status")), Is.EqualTo(("ended", "TimedOut")));
    }

    [Test]
    public async Task Stream_EndsCompleted_WhenEveryRequestedSupplierAnswers()
    {
        var url = $"{STREAM_URL}&suppliers={PriceHuntApiFactory.FAST}&suppliers={PriceHuntApiFactory.BROKEN}";
        using var response = await GetStreamAsync(url);

        var events = await SseReader.ReadAllAsync(response);

        Assert.That((events[^1].Type, events[^1].Text("status")), Is.EqualTo(("ended", "Completed")));
    }

    [Test]
    public async Task Stream_QueriesOnlyTheRequestedSuppliers()
    {
        using var response = await GetStreamAsync($"{STREAM_URL}&suppliers={PriceHuntApiFactory.SLOW}");

        var events = await SseReader.ReadAllAsync(response);

        Assert.That(
            events.Where(e => e.Type is "price" or "failed").Select(e => e.Text("supplierName")),
            Is.EqualTo(new[] { PriceHuntApiFactory.SLOW }));
    }

    [Test]
    public async Task Stream_ReportsHowManySuppliersResponded()
    {
        using var response = await GetStreamAsync(STREAM_URL);

        var events = await SseReader.ReadAllAsync(response);

        Assert.That(events[^1].Number("respondedCount"), Is.EqualTo(3));
    }

    [Test]
    public async Task Stream_PersistsTheSearchWithOneResponsePerSupplier()
    {
        using var response = await GetStreamAsync(STREAM_URL);
        var events = await SseReader.ReadAllAsync(response);

        var search = await LoadSearchAsync(events[0].Text("searchId")!);

        Assert.That(search.Responses, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task Stream_PersistsTheSilentSupplierAsNoResponse()
    {
        using var response = await GetStreamAsync(STREAM_URL);
        var events = await SseReader.ReadAllAsync(response);

        var search = await LoadSearchAsync(events[0].Text("searchId")!);

        var silent = search.Responses.Single(r => r.SupplierName == PriceHuntApiFactory.SILENT);
        Assert.That(silent.Outcome, Is.EqualTo(ResponseOutcome.NoResponse));
    }

    [Test]
    public async Task Stream_RecordsTheSearchAsCancelled_WhenTheClientDisconnects()
    {
        using var disconnect = new CancellationTokenSource();
        using var response = await _client.GetAsync(
            STREAM_URL, HttpCompletionOption.ResponseHeadersRead, disconnect.Token);
        var started = await SseReader.ReadFirstAsync(response, disconnect.Token);

        await disconnect.CancelAsync();
        response.Dispose();

        var search = await WaitForEndedSearchAsync(started.Text("searchId")!);
        Assert.That(search.Status, Is.EqualTo(SearchStatus.Cancelled));
    }

    [Test]
    public async Task Stream_Returns400_OnAnInvalidQuery()
    {
        using var response = await _client.GetAsync("/api/searches/stream?fromLocation=Haifa");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Stream_NamesTheInvalidParameters_InTheProblemDetails()
    {
        using var response = await _client.GetAsync("/api/searches/stream?fromLocation=Haifa");

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("toLocation").And.Contain("fromDate").And.Contain("toDate"));
    }

    private Task<HttpResponseMessage> GetStreamAsync(string url)
    {
        return _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    }

    private async Task<Search> LoadSearchAsync(string searchId)
    {
        using var db = _factory.CreateDbContext();

        return await db.Searches
            .Include(s => s.Responses)
            .SingleAsync(s => s.Id == Guid.Parse(searchId));
    }

    /// <summary>
    /// The server records the cancellation shortly after the disconnect;
    /// polls until the search is no longer Running. If cancellation did not
    /// propagate, the search would end TimedOut after the one-second timeout
    /// instead, so the assertion still distinguishes the two.
    /// </summary>
    private async Task<Search> WaitForEndedSearchAsync(string searchId)
    {
        var deadline = DateTime.UtcNow + PriceHuntApiFactory.SEARCH_TIMEOUT * 3;
        while (DateTime.UtcNow < deadline)
        {
            var search = await LoadSearchAsync(searchId);
            if (search.Status != SearchStatus.Running)
            {
                return search;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        throw new TimeoutException("The search never left the Running state.");
    }
}
