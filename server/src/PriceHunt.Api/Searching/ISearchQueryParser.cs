namespace PriceHunt.Api.Searching;

/// <summary>
/// Turns the raw query string into a validated <c>SearchRequest</c>, or
/// into the list of everything that is wrong with it.
/// </summary>
public interface ISearchQueryParser
{
    SearchQueryParseResult Parse(SearchQuery query);
}
