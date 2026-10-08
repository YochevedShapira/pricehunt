namespace PriceHunt.Domain.Enums;

/// <summary>
/// Lifecycle of a price search. <see cref="Running"/> is the only
/// non-terminal state; the other three are the possible final statuses.
/// </summary>
public enum SearchStatus
{
    Running = 0,
    Completed = 1,
    TimedOut = 2,
    Cancelled = 3,
}
