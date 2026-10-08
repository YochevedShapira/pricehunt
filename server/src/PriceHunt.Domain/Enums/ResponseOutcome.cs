namespace PriceHunt.Domain.Enums;

/// <summary>
/// What a supplier did within a search. Every requested supplier ends up
/// with exactly one outcome once the search has ended.
/// </summary>
public enum ResponseOutcome
{
    /// <summary>
    /// The supplier returned a price.
    /// </summary>
    Quoted = 0,

    /// <summary>
    /// The supplier answered with an error.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// The supplier had not answered when the search ended.
    /// </summary>
    NoResponse = 2,
}
