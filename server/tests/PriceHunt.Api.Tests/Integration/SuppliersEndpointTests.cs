using System.Net.Http.Json;

namespace PriceHunt.Api.Tests.Integration;

public class SuppliersEndpointTests
{
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
    public async Task List_ReturnsEveryRegisteredSupplierName()
    {
        var suppliers = await _client.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers");

        Assert.That(
            suppliers!.Select(s => s.Name),
            Is.EqualTo(new[]
            {
                PriceHuntApiFactory.FAST,
                PriceHuntApiFactory.SLOW,
                PriceHuntApiFactory.BROKEN,
                PriceHuntApiFactory.SILENT,
            }));
    }

    private sealed record SupplierDto(string Name);
}
