using PriceHunt.Application.Searching;

namespace PriceHunt.Api.Searching;

/// <summary>
/// Either a valid <see cref="SearchRequest"/> or the validation errors,
/// keyed by query parameter name as the client sent it.
/// </summary>
public sealed class SearchQueryParseResult
{
    private SearchQueryParseResult(
        SearchRequest? request,
        IReadOnlyDictionary<string, string[]> errors)
    {
        Request = request;
        Errors = errors;
    }

    public SearchRequest? Request { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public bool IsValid => Request is not null;

    public static SearchQueryParseResult Valid(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchQueryParseResult(request, new Dictionary<string, string[]>());
    }

    public static SearchQueryParseResult Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("An invalid result needs at least one error.", nameof(errors));
        }

        return new SearchQueryParseResult(request: null, errors);
    }
}
