using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Application.Searching;

namespace PriceHunt.Api.Searching;

/// <summary>
/// <c>GET /api/searches/stream</c>: runs a search and streams its events as
/// Server-Sent Events. Closing the connection cancels the search, because
/// the request's cancellation token is the one handed to the use case.
/// </summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearch(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/searches/stream", Stream)
            .WithName("StreamSearch")
            .WithSummary("Streams supplier prices for a route and date range as they arrive.");

        return endpoints;
    }

    private static Results<ServerSentEventsResult<string>, ValidationProblem> Stream(
        [AsParameters] SearchQuery query,
        ISearchQueryParser parser,
        IPriceSearch priceSearch,
        CancellationToken cancellationToken)
    {
        var parsed = parser.Parse(query);
        if (!parsed.IsValid)
        {
            return TypedResults.ValidationProblem(
                parsed.Errors.ToDictionary(e => e.Key, e => e.Value));
        }

        var events = priceSearch.RunAsync(parsed.Request!, cancellationToken);

        return TypedResults.ServerSentEvents(SearchEventMapper.ToSseItems(events));
    }
}
