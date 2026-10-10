using System.Text.Json;
using PriceHunt.Api.Validation;
using PriceHunt.Application.Searching;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;
using Route = PriceHunt.Domain.ValueObjects.Route;

namespace PriceHunt.Api.Searching;

/// <summary>
/// Validates every field of a <see cref="SearchQuery"/> and reports all
/// problems at once, so the client can show them together. The domain
/// value objects re-check their own invariants when constructed; the
/// explicit checks here exist to produce user-facing messages.
/// </summary>
public sealed class SearchQueryParser : ISearchQueryParser
{
    private static readonly string FROM_LOCATION = ParameterName(nameof(SearchQuery.FromLocation));
    private static readonly string TO_LOCATION = ParameterName(nameof(SearchQuery.ToLocation));
    private static readonly string FROM_DATE = ParameterName(nameof(SearchQuery.FromDate));
    private static readonly string TO_DATE = ParameterName(nameof(SearchQuery.ToDate));
    private static readonly string SUPPLIERS = ParameterName(nameof(SearchQuery.Suppliers));

    private readonly ISupplierCatalog _catalog;

    public SearchQueryParser(ISupplierCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
    }

    public ParseResult<SearchRequest> Parse(SearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new Dictionary<string, string[]>();
        var route = ParseRoute(query, errors);
        var dates = ParseDates(query, errors);
        var suppliers = ParseSuppliers(query, errors);

        return errors.Count > 0
            ? ParseResult.Invalid<SearchRequest>(errors)
            : ParseResult.Valid(new SearchRequest(route!, dates!, suppliers!));
    }

    private static Route? ParseRoute(SearchQuery query, Dictionary<string, string[]> errors)
    {
        var valid = true;
        if (string.IsNullOrWhiteSpace(query.FromLocation))
        {
            errors[FROM_LOCATION] = ["The origin location is required."];
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(query.ToLocation))
        {
            errors[TO_LOCATION] = ["The destination location is required."];
            valid = false;
        }
        else if (valid && SameLocation(query.FromLocation!, query.ToLocation))
        {
            errors[TO_LOCATION] = ["The destination must differ from the origin."];
            valid = false;
        }

        return valid ? new Route(query.FromLocation!, query.ToLocation!) : null;
    }

    private static DateRange? ParseDates(SearchQuery query, Dictionary<string, string[]> errors)
    {
        var valid = true;
        if (query.FromDate is null)
        {
            errors[FROM_DATE] = ["The start date is required."];
            valid = false;
        }

        if (query.ToDate is null)
        {
            errors[TO_DATE] = ["The end date is required."];
            valid = false;
        }
        else if (valid && query.ToDate < query.FromDate)
        {
            errors[TO_DATE] = ["The end date must not be before the start date."];
            valid = false;
        }

        return valid ? new DateRange(query.FromDate!.Value, query.ToDate!.Value) : null;
    }

    private IReadOnlyCollection<string>? ParseSuppliers(
        SearchQuery query,
        Dictionary<string, string[]> errors)
    {
        if (query.Suppliers is null || query.Suppliers.Length == 0)
        {
            return _catalog.All.Select(s => s.Name).ToList();
        }

        var problems = new List<string>();
        problems.AddRange(query.Suppliers
            .Where(name => _catalog.Find(name) is null)
            .Select(name => $"Unknown supplier '{name}'."));
        problems.AddRange(query.Suppliers
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"Supplier '{group.Key}' is listed more than once."));

        if (problems.Count > 0)
        {
            errors[SUPPLIERS] = [.. problems];
            return null;
        }

        return query.Suppliers;
    }

    private static bool SameLocation(string from, string to)
    {
        return string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string ParameterName(string propertyName)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
    }
}
