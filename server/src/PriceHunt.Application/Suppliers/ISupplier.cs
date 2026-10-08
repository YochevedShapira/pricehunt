using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Suppliers;

/// <summary>
/// Port for one shipping-price supplier. The application layer only knows
/// this contract; Infrastructure provides the adapters (simulated today,
/// HTTP clients in a real deployment).
/// </summary>
public interface ISupplier
{
    /// <summary>
    /// Unique, human-readable supplier name. Used as the identity in
    /// requests, events and persistence.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Asks the supplier for a price. Throws
    /// <see cref="SupplierUnavailableException"/> when the supplier cannot
    /// quote, and honours <paramref name="cancellationToken"/> so that an
    /// abandoned search stops the call.
    /// </summary>
    Task<Money> GetQuoteAsync(QuoteRequest request, CancellationToken cancellationToken);
}
