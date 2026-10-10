using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PriceHunt.Api.Tests.Integration;

public class HistoryEndpointTests
{
    private const string STREAM_URL =
        "/api/searches/stream?fromLocation=Haifa&toLocation=Rotterdam&fromDate=2026-03-01&toDate=2026-03-10"
        + "&suppliers=" + PriceHuntApiFactory.FAST + "&suppliers=" + PriceHuntApiFactory.BROKEN;

    private PriceHuntApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new PriceHuntApiFactory();
        _client = _factory.CreateClient();
        await RunOneSearchAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Read_ReturnsOneRowPerSupplierResponse()
    {
        var page = await ReadAsync("/api/history");

        Assert.That(page.GetProperty("totalCount").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public async Task Read_ExposesTheRouteAndPriceOnEachRow()
    {
        var page = await ReadAsync($"/api/history?suppliers={PriceHuntApiFactory.FAST}");

        var row = page.GetProperty("items")[0];
        Assert.That(
            (row.GetProperty("fromLocation").GetString(),
                row.GetProperty("toLocation").GetString(),
                row.GetProperty("amount").GetDecimal(),
                row.GetProperty("currency").GetString()),
            Is.EqualTo(("Haifa", "Rotterdam", 1000m, "USD")));
    }

    [Test]
    public async Task Read_ExposesAFailureWithoutAPrice()
    {
        var page = await ReadAsync($"/api/history?suppliers={PriceHuntApiFactory.BROKEN}");

        var row = page.GetProperty("items")[0];
        Assert.That(
            (row.GetProperty("outcome").GetString(), row.GetProperty("amount").ValueKind),
            Is.EqualTo(("Failed", JsonValueKind.Null)));
    }

    [Test]
    public async Task Read_SortsAndPages()
    {
        var page = await ReadAsync("/api/history?sortBy=supplier&sortDirection=asc&pageSize=1&page=2");

        var row = page.GetProperty("items").EnumerateArray().Single();
        Assert.That(row.GetProperty("supplierName").GetString(), Is.EqualTo(PriceHuntApiFactory.FAST));
    }

    [Test]
    public async Task Read_Returns400_OnAnUnknownSortColumn()
    {
        using var response = await _client.GetAsync("/api/history?sortBy=colour");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<JsonElement> ReadAsync(string url)
    {
        return await _client.GetFromJsonAsync<JsonElement>(url);
    }

    private async Task RunOneSearchAsync()
    {
        using var response = await _client.GetAsync(STREAM_URL, HttpCompletionOption.ResponseHeadersRead);
        await SseReader.ReadAllAsync(response);
    }
}
