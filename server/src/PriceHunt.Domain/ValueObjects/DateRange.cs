namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// Inclusive range of calendar dates. <see cref="From"/> never follows
/// <see cref="To"/>.
/// </summary>
public sealed record DateRange
{
    public DateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException(
                $"The range start {from} is after its end {to}.",
                nameof(from));
        }

        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }
}
