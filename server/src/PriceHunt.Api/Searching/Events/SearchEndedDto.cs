namespace PriceHunt.Api.Searching.Events;

/// <summary>
/// Payload of the <c>ended</c> SSE event. <paramref name="Status"/> is the
/// final <c>SearchStatus</c> name: Completed, TimedOut or Cancelled.
/// </summary>
public sealed record SearchEndedDto(string Status, int RespondedCount);
