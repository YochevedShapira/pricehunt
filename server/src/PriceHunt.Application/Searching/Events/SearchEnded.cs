using PriceHunt.Domain.Enums;

namespace PriceHunt.Application.Searching.Events;

/// <summary>
/// Last event of every search. <paramref name="Status"/> is one of the
/// terminal <see cref="SearchStatus"/> values; <paramref name="RespondedCount"/>
/// is how many suppliers answered (with a price or a failure) before the end.
/// </summary>
public sealed record SearchEnded(SearchStatus Status, int RespondedCount) : SearchEvent;
