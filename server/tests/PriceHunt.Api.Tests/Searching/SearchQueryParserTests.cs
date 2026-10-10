using NSubstitute;
using PriceHunt.Api.Searching;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Api.Tests.Searching;

public class SearchQueryParserTests
{
    private const string OCEANIC = "Oceanic";
    private const string BLUEWAVE = "BlueWave";

    private static readonly DateOnly MARCH_1 = new(2026, 3, 1);
    private static readonly DateOnly MARCH_10 = new(2026, 3, 10);

    private SearchQueryParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        var catalog = Substitute.For<ISupplierCatalog>();
        var oceanic = SupplierNamed(OCEANIC);
        var bluewave = SupplierNamed(BLUEWAVE);
        catalog.All.Returns([oceanic, bluewave]);
        catalog.Find(OCEANIC).Returns(oceanic);
        catalog.Find(BLUEWAVE).Returns(bluewave);
        catalog.Find(Arg.Is<string>(n => n != OCEANIC && n != BLUEWAVE)).Returns((ISupplier?)null);

        _parser = new SearchQueryParser(catalog);
    }

    [Test]
    public void Parse_IsValid_OnACompleteQuery()
    {
        var result = _parser.Parse(ValidQuery());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Parse_BuildsTheRoute()
    {
        var result = _parser.Parse(ValidQuery());

        Assert.That(result.Request!.Route, Is.EqualTo(new Route("Haifa", "Rotterdam")));
    }

    [Test]
    public void Parse_BuildsTheDateRange()
    {
        var result = _parser.Parse(ValidQuery());

        Assert.That(result.Request!.Dates, Is.EqualTo(new DateRange(MARCH_1, MARCH_10)));
    }

    [Test]
    public void Parse_KeepsTheRequestedSuppliers()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = [BLUEWAVE] });

        Assert.That(result.Request!.SupplierNames, Is.EqualTo(new[] { BLUEWAVE }));
    }

    [Test]
    public void Parse_SelectsEverySupplier_WhenNoneIsGiven()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = null });

        Assert.That(result.Request!.SupplierNames, Is.EqualTo(new[] { OCEANIC, BLUEWAVE }));
    }

    [Test]
    public void Parse_SelectsEverySupplier_WhenTheListIsEmpty()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = [] });

        Assert.That(result.Request!.SupplierNames, Is.EqualTo(new[] { OCEANIC, BLUEWAVE }));
    }

    [TestCase(null)]
    [TestCase("  ")]
    public void Parse_ReportsFromLocation_WhenMissing(string? fromLocation)
    {
        var result = _parser.Parse(ValidQuery() with { FromLocation = fromLocation });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "fromLocation" }));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Parse_ReportsToLocation_WhenMissing(string? toLocation)
    {
        var result = _parser.Parse(ValidQuery() with { ToLocation = toLocation });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "toLocation" }));
    }

    [Test]
    public void Parse_ReportsToLocation_WhenItEqualsFromLocation()
    {
        var result = _parser.Parse(ValidQuery() with { ToLocation = " haifa " });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "toLocation" }));
    }

    [Test]
    public void Parse_ReportsFromDate_WhenMissing()
    {
        var result = _parser.Parse(ValidQuery() with { FromDate = null });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "fromDate" }));
    }

    [Test]
    public void Parse_ReportsToDate_WhenMissing()
    {
        var result = _parser.Parse(ValidQuery() with { ToDate = null });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "toDate" }));
    }

    [Test]
    public void Parse_ReportsToDate_WhenBeforeFromDate()
    {
        var result = _parser.Parse(ValidQuery() with { FromDate = MARCH_10, ToDate = MARCH_1 });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "toDate" }));
    }

    [Test]
    public void Parse_ReportsSuppliers_OnAnUnknownName()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = [OCEANIC, "Nobody"] });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "suppliers" }));
    }

    [Test]
    public void Parse_NamesTheUnknownSupplierInTheMessage()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = ["Nobody"] });

        Assert.That(result.Errors["suppliers"].Single(), Does.Contain("Nobody"));
    }

    [Test]
    public void Parse_ReportsSuppliers_OnADuplicateName()
    {
        var result = _parser.Parse(ValidQuery() with { Suppliers = [OCEANIC, OCEANIC] });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "suppliers" }));
    }

    [Test]
    public void Parse_CollectsEveryError_InOnePass()
    {
        var result = _parser.Parse(new SearchQuery());

        Assert.That(
            result.Errors.Keys,
            Is.EquivalentTo(new[] { "fromLocation", "toLocation", "fromDate", "toDate" }));
    }

    [Test]
    public void Parse_HasNoRequest_WhenInvalid()
    {
        var result = _parser.Parse(new SearchQuery());

        Assert.That(result.Request, Is.Null);
    }

    private static SearchQuery ValidQuery()
    {
        return new SearchQuery
        {
            FromLocation = "Haifa",
            ToLocation = "Rotterdam",
            FromDate = MARCH_1,
            ToDate = MARCH_10,
            Suppliers = [OCEANIC, BLUEWAVE],
        };
    }

    private static ISupplier SupplierNamed(string name)
    {
        var supplier = Substitute.For<ISupplier>();
        supplier.Name.Returns(name);

        return supplier;
    }
}
