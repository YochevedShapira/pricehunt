using PriceHunt.Api.History;
using PriceHunt.Application.History;

namespace PriceHunt.Api.Tests.History;

public class HistoryQueryParserTests
{
    private static readonly DateOnly MARCH_1 = new(2026, 3, 1);
    private static readonly DateOnly MARCH_10 = new(2026, 3, 10);

    private HistoryQueryParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new HistoryQueryParser();
    }

    [Test]
    public void Parse_IsValid_OnEmptyParameters()
    {
        var result = _parser.Parse(new HistoryParameters());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Parse_DefaultsToNewestFirst()
    {
        var result = _parser.Parse(new HistoryParameters());

        Assert.That(
            (result.Value!.SortBy, result.Value.SortDirection),
            Is.EqualTo((HistorySortColumn.Date, SortDirection.Descending)));
    }

    [Test]
    public void Parse_DefaultsToTheFirstPageOfTwenty()
    {
        var result = _parser.Parse(new HistoryParameters());

        Assert.That((result.Value!.Page, result.Value.PageSize), Is.EqualTo((1, 20)));
    }

    [Test]
    public void Parse_DefaultsToEverySupplier()
    {
        var result = _parser.Parse(new HistoryParameters());

        Assert.That(result.Value!.SupplierNames, Is.Empty);
    }

    [Test]
    public void Parse_KeepsTheFilters()
    {
        var result = _parser.Parse(new HistoryParameters
        {
            StartDate = MARCH_1,
            EndDate = MARCH_10,
            Suppliers = ["Oceanic"],
            FromLocation = "Haifa",
            ToLocation = "Rotterdam",
        });

        Assert.That(
            (result.Value!.StartDate, result.Value.EndDate, result.Value.SupplierNames.Single(),
                result.Value.FromLocation, result.Value.ToLocation),
            Is.EqualTo((MARCH_1, MARCH_10, "Oceanic", "Haifa", "Rotterdam")));
    }

    [TestCase("price", HistorySortColumn.Price)]
    [TestCase("ResponseTime", HistorySortColumn.ResponseTime)]
    [TestCase("SUPPLIER", HistorySortColumn.Supplier)]
    public void Parse_ReadsTheSortColumnIgnoringCase(string sortBy, HistorySortColumn expected)
    {
        var result = _parser.Parse(new HistoryParameters { SortBy = sortBy });

        Assert.That(result.Value!.SortBy, Is.EqualTo(expected));
    }

    [TestCase("asc", SortDirection.Ascending)]
    [TestCase("DESC", SortDirection.Descending)]
    public void Parse_ReadsTheSortDirectionIgnoringCase(string direction, SortDirection expected)
    {
        var result = _parser.Parse(new HistoryParameters { SortDirection = direction });

        Assert.That(result.Value!.SortDirection, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_ReportsSortBy_OnAnUnknownColumn()
    {
        var result = _parser.Parse(new HistoryParameters { SortBy = "colour" });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "sortBy" }));
    }

    [Test]
    public void Parse_ReportsSortDirection_OnAnUnknownDirection()
    {
        var result = _parser.Parse(new HistoryParameters { SortDirection = "sideways" });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "sortDirection" }));
    }

    [Test]
    public void Parse_ReportsEndDate_WhenBeforeStartDate()
    {
        var result = _parser.Parse(new HistoryParameters { StartDate = MARCH_10, EndDate = MARCH_1 });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "endDate" }));
    }

    [Test]
    public void Parse_ReportsPage_WhenBelowOne()
    {
        var result = _parser.Parse(new HistoryParameters { Page = 0 });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "page" }));
    }

    [TestCase(0)]
    [TestCase(101)]
    public void Parse_ReportsPageSize_WhenOutOfRange(int pageSize)
    {
        var result = _parser.Parse(new HistoryParameters { PageSize = pageSize });

        Assert.That(result.Errors.Keys, Is.EqualTo(new[] { "pageSize" }));
    }

    [Test]
    public void Parse_CollectsEveryError_InOnePass()
    {
        var result = _parser.Parse(new HistoryParameters
        {
            SortBy = "colour",
            SortDirection = "sideways",
            Page = 0,
            PageSize = 0,
        });

        Assert.That(
            result.Errors.Keys,
            Is.EquivalentTo(new[] { "sortBy", "sortDirection", "page", "pageSize" }));
    }
}
