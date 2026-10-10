using System.Text.Json;

namespace PriceHunt.Api.Tests.Integration;

/// <summary>
/// One event read back from the SSE stream, with its JSON payload parsed.
/// </summary>
public sealed record SseEvent(string Type, JsonElement Data)
{
    public string? Text(string property)
    {
        return Data.GetProperty(property).GetString();
    }

    public int Number(string property)
    {
        return Data.GetProperty(property).GetInt32();
    }
}
