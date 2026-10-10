using System.Text.Json;
using PriceHunt.Api.Validation;
using PriceHunt.Application.History;

namespace PriceHunt.Api.History;

/// <summary>
/// Applies defaults and validates every history parameter, reporting all
/// problems at once. Sort columns are parsed into the closed
/// <see cref="HistorySortColumn"/> set, so the store is never asked to
/// order by an arbitrary string.
/// </summary>
public sealed class HistoryQueryParser : IHistoryQueryParser
{
    public const int DefaultPageSize = 20;

    private const string ASCENDING = "asc";
    private const string DESCENDING = "desc";

    private static readonly string START_DATE = ParameterName(nameof(HistoryParameters.StartDate));
    private static readonly string END_DATE = ParameterName(nameof(HistoryParameters.EndDate));
    private static readonly string SORT_BY = ParameterName(nameof(HistoryParameters.SortBy));
    private static readonly string SORT_DIRECTION = ParameterName(nameof(HistoryParameters.SortDirection));
    private static readonly string PAGE = ParameterName(nameof(HistoryParameters.Page));
    private static readonly string PAGE_SIZE = ParameterName(nameof(HistoryParameters.PageSize));

    public ParseResult<HistoryQuery> Parse(HistoryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var errors = new Dictionary<string, string[]>();
        ValidateDates(parameters, errors);
        var sortBy = ParseSortColumn(parameters.SortBy, errors);
        var direction = ParseDirection(parameters.SortDirection, errors);
        var page = ParsePage(parameters.Page, errors);
        var pageSize = ParsePageSize(parameters.PageSize, errors);

        if (errors.Count > 0)
        {
            return ParseResult.Invalid<HistoryQuery>(errors);
        }

        return ParseResult.Valid(new HistoryQuery(
            parameters.StartDate,
            parameters.EndDate,
            parameters.Suppliers ?? [],
            parameters.FromLocation,
            parameters.ToLocation,
            sortBy,
            direction,
            page,
            pageSize));
    }

    private static void ValidateDates(HistoryParameters parameters, Dictionary<string, string[]> errors)
    {
        if (parameters.StartDate > parameters.EndDate)
        {
            errors[END_DATE] = ["The end date must not be before the start date."];
        }
    }

    private static HistorySortColumn ParseSortColumn(string? sortBy, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return HistorySortColumn.Date;
        }

        if (Enum.TryParse<HistorySortColumn>(sortBy, ignoreCase: true, out var column))
        {
            return column;
        }

        var allowed = string.Join(", ", Enum.GetNames<HistorySortColumn>().Select(ParameterName));
        errors[SORT_BY] = [$"'{sortBy}' is not a sortable column. Use one of: {allowed}."];

        return HistorySortColumn.Date;
    }

    private static SortDirection ParseDirection(string? direction, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return SortDirection.Descending;
        }

        if (string.Equals(direction, ASCENDING, StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Ascending;
        }

        if (string.Equals(direction, DESCENDING, StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Descending;
        }

        errors[SORT_DIRECTION] = [$"'{direction}' is not a sort direction. Use {ASCENDING} or {DESCENDING}."];

        return SortDirection.Descending;
    }

    private static int ParsePage(int? page, Dictionary<string, string[]> errors)
    {
        if (page is null)
        {
            return 1;
        }

        if (page < 1)
        {
            errors[PAGE] = ["The page number starts at 1."];
        }

        return Math.Max(page.Value, 1);
    }

    private static int ParsePageSize(int? pageSize, Dictionary<string, string[]> errors)
    {
        if (pageSize is null)
        {
            return DefaultPageSize;
        }

        if (pageSize < 1 || pageSize > HistoryQuery.MaxPageSize)
        {
            errors[PAGE_SIZE] = [$"The page size must be between 1 and {HistoryQuery.MaxPageSize}."];
        }

        return Math.Clamp(pageSize.Value, 1, HistoryQuery.MaxPageSize);
    }

    private static string ParameterName(string propertyName)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
    }
}
