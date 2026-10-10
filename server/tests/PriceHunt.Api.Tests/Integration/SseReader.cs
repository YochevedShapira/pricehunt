using System.Net.ServerSentEvents;
using System.Text.Json;

namespace PriceHunt.Api.Tests.Integration;

/// <summary>
/// Reads an SSE response with the BCL parser, the same one a .NET client
/// would use, so the tests check the real wire format.
/// </summary>
public static class SseReader
{
    public static async Task<List<SseEvent>> ReadAllAsync(HttpResponseMessage response)
    {
        var events = new List<SseEvent>();
        await using var stream = await response.Content.ReadAsStreamAsync();

        await foreach (var item in SseParser.Create(stream).EnumerateAsync())
        {
            events.Add(Parse(item));
        }

        return events;
    }

    public static async Task<SseEvent> ReadFirstAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            return Parse(item);
        }

        throw new InvalidOperationException("The stream ended before its first event.");
    }

    private static SseEvent Parse(SseItem<string> item)
    {
        return new SseEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement);
    }
}
