using System.Net.ServerSentEvents;
using System.Text.Json;
using PriceHunt.Api.Searching.Events;
using PriceHunt.Application.Searching.Events;

namespace PriceHunt.Api.Searching;

/// <summary>
/// Maps each <see cref="SearchEvent"/> to a named SSE event with a JSON
/// payload. The event names are the contract with the Angular client.
/// </summary>
public static class SearchEventMapper
{
    private const string STARTED = "started";
    private const string PRICE = "price";
    private const string FAILED = "failed";
    private const string ENDED = "ended";

    public static async IAsyncEnumerable<SseItem<string>> ToSseItems(
        IAsyncEnumerable<SearchEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        await foreach (var searchEvent in events.ConfigureAwait(false))
        {
            yield return ToSseItem(searchEvent);
        }
    }

    public static SseItem<string> ToSseItem(SearchEvent searchEvent)
    {
        ArgumentNullException.ThrowIfNull(searchEvent);

        return searchEvent switch
        {
            SearchStarted started => Item(STARTED, new SearchStartedDto(
                started.SearchId, started.SupplierCount)),
            PriceReceived price => Item(PRICE, new PriceReceivedDto(
                price.SupplierName,
                price.Price.Amount,
                price.Price.Currency,
                (long)price.ResponseTime.TotalMilliseconds)),
            SupplierFailed failed => Item(FAILED, new SupplierFailedDto(
                failed.SupplierName,
                failed.Reason,
                (long)failed.ResponseTime.TotalMilliseconds)),
            SearchEnded ended => Item(ENDED, new SearchEndedDto(
                ended.Status.ToString(), ended.RespondedCount)),
            _ => throw new InvalidOperationException(
                $"Unexpected search event {searchEvent.GetType().Name}."),
        };
    }

    /// <summary>
    /// The payload is serialised here so that every event uses the same
    /// camel-case web defaults regardless of its runtime type.
    /// </summary>
    private static SseItem<string> Item<T>(string eventType, T payload)
    {
        return new SseItem<string>(
            JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            eventType);
    }
}
