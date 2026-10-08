using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Infrastructure.Suppliers;

/// <summary>
/// <see cref="ISupplier"/> adapter that simulates a remote supplier from a
/// <see cref="SupplierProfile"/>. Delays go through <see cref="TimeProvider"/>
/// and randomness through an injected <see cref="Random"/>, so tests can
/// drive both deterministically.
/// </summary>
public sealed class SimulatedSupplier : ISupplier
{
    private const string CURRENCY = "USD";
    private const int PRICE_DECIMALS = 2;

    private readonly SupplierProfile _profile;
    private readonly TimeProvider _timeProvider;
    private readonly Random _random;

    public SimulatedSupplier(SupplierProfile profile, TimeProvider timeProvider, Random random)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(random);

        _profile = profile;
        _timeProvider = timeProvider;
        _random = random;
    }

    public string Name => _profile.Name;

    public async Task<Money> GetPriceAsync(PriceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_profile.NeverResponds)
        {
            // Leaves only by throwing OperationCanceledException: the supplier
            // honours cancellation even though it never answers.
            await Task.Delay(Timeout.InfiniteTimeSpan, _timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }

        await Task.Delay(DrawDelay(), _timeProvider, cancellationToken).ConfigureAwait(false);

        if (_random.NextDouble() < _profile.FailureRate)
        {
            throw new SupplierUnavailableException(
                Name, $"{Name} could not provide a price for this request.");
        }

        return new Money(DrawPrice(), CURRENCY);
    }

    private TimeSpan DrawDelay()
    {
        var span = _profile.MaxDelay - _profile.MinDelay;

        return _profile.MinDelay + span * _random.NextDouble();
    }

    private decimal DrawPrice()
    {
        var span = _profile.MaxPrice - _profile.MinPrice;
        var price = _profile.MinPrice + span * (decimal)_random.NextDouble();

        return Math.Round(price, PRICE_DECIMALS);
    }
}
