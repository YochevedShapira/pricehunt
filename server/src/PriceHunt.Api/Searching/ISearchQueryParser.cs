using PriceHunt.Api.Validation;
using PriceHunt.Application.Searching;

namespace PriceHunt.Api.Searching;

/// <summary>
/// Turns the raw query string into a validated <see cref="SearchRequest"/>, or
/// into the list of everything that is wrong with it.
/// </summary>
public interface ISearchQueryParser
{
    ParseResult<SearchRequest> Parse(SearchQuery query);
}
