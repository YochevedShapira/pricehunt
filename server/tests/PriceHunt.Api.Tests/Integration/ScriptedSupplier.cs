using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Api.Tests.Integration;

/// <summary>
/// Deterministic <see cref="ISupplier"/> for integration tests: answers,
/// fails or never responds after a fixed delay. Real time is used, with
/// delays short enough to keep the tests fast.
/// </summary>
public sealed class ScriptedSupplier : ISupplier
{
    private readonly TimeSpan _delay;
    private readonly Money? _price;
    private readonly bool _neverResponds;

    private ScriptedSupplier(string name, TimeSpan delay, Money? price, bool neverResponds)
    {
        Name = name;
        _delay = delay;
        _price = price;
        _neverResponds = neverResponds;
    }

    public string Name { get; }

    public static ScriptedSupplier Answering(string name, TimeSpan delay, decimal amount)
    {
        return new ScriptedSupplier(name, delay, new Money(amount, "USD"), neverResponds: false);
    }

    public static ScriptedSupplier Failing(string name, TimeSpan delay)
    {
        return new ScriptedSupplier(name, delay, price: null, neverResponds: false);
    }

    public static ScriptedSupplier Silent(string name)
    {
        return new ScriptedSupplier(name, TimeSpan.Zero, price: null, neverResponds: true);
    }

    public async Task<Money> GetPriceAsync(PriceRequest request, CancellationToken cancellationToken)
    {
        if (_neverResponds)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        await Task.Delay(_delay, cancellationToken);

        return _price ?? throw new SupplierUnavailableException(Name, $"{Name} is down.");
    }
}
