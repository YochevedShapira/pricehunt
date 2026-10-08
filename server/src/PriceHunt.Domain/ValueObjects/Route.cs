namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// Origin and destination of a shipment. Locations are trimmed and must
/// differ from each other.
/// </summary>
public sealed record Route
{
    public Route(string fromLocation, string toLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromLocation);
        ArgumentException.ThrowIfNullOrWhiteSpace(toLocation);

        FromLocation = fromLocation.Trim();
        ToLocation = toLocation.Trim();

        if (string.Equals(FromLocation, ToLocation, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Origin and destination must be different locations.",
                nameof(toLocation));
        }
    }

    public string FromLocation { get; }

    public string ToLocation { get; }
}
