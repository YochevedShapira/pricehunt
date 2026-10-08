namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// A positive amount in an ISO 4217 currency. The currency code is stored
/// upper-cased so that two prices in the same currency compare equal.
/// </summary>
public sealed record Money
{
    private const int CURRENCY_CODE_LENGTH = 3;

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var code = currency.Trim().ToUpperInvariant();
        if (code.Length != CURRENCY_CODE_LENGTH)
        {
            throw new ArgumentException(
                $"'{currency}' is not a three-letter currency code.",
                nameof(currency));
        }

        Amount = amount;
        Currency = code;
    }

    public decimal Amount { get; }

    public string Currency { get; }
}
