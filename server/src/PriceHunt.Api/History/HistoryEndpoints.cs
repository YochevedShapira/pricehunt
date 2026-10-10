using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Application.History;

namespace PriceHunt.Api.History;

/// <summary>
/// <c>GET /api/history</c>: one page of past supplier responses, filtered,
/// sorted and paged in the database.
/// </summary>
public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/history", Read)
            .WithName("ReadHistory")
            .WithSummary("Reads a page of price history with filters, sorting and paging.");

        return endpoints;
    }

    private static async Task<Results<Ok<HistoryPageDto>, ValidationProblem>> Read(
        [AsParameters] HistoryParameters parameters,
        IHistoryQueryParser parser,
        IHistoryReader reader,
        CancellationToken cancellationToken)
    {
        var parsed = parser.Parse(parameters);
        if (!parsed.IsValid)
        {
            return TypedResults.ValidationProblem(parsed.ErrorsForProblemDetails());
        }

        var page = await reader.ReadAsync(parsed.Value!, cancellationToken);

        return TypedResults.Ok(ToDto(page));
    }

    private static HistoryPageDto ToDto(HistoryPage page)
    {
        return new HistoryPageDto(
            page.Items.Select(ToDto).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    private static HistoryEntryDto ToDto(HistoryEntry entry)
    {
        return new HistoryEntryDto(
            entry.SearchId,
            entry.RecordedAt,
            entry.FromLocation,
            entry.ToLocation,
            entry.SupplierName,
            entry.Outcome.ToString(),
            entry.Price?.Amount,
            entry.Price?.Currency,
            (long)entry.ResponseTime.TotalMilliseconds);
    }
}
