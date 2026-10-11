using System.Net;

namespace PriceHunt.Api.Tests.Integration;

public class CrossCuttingTests
{
    private const string ANGULAR_ORIGIN = "http://localhost:4200";

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
    public async Task Health_ReportsHealthy()
    {
        using var response = await _client.GetAsync("/health");

        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task Cors_AllowsTheAngularDevOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/suppliers");
        request.Headers.Add("Origin", ANGULAR_ORIGIN);

        using var response = await _client.SendAsync(request);

        Assert.That(
            response.Headers.GetValues("Access-Control-Allow-Origin").Single(),
            Is.EqualTo(ANGULAR_ORIGIN));
    }

    [Test]
    public async Task Cors_RejectsAnUnknownOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/suppliers");
        request.Headers.Add("Origin", "http://evil.example");

        using var response = await _client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }

    [Test]
    public async Task OpenApi_IsServedInDevelopment()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task OpenApi_DescribesTheHistoryEndpoint()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.That(document, Does.Contain("/api/history"));
    }
}
