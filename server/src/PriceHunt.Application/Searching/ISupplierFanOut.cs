using PriceHunt.Application.Suppliers;

namespace PriceHunt.Application.Searching;

/// <summary>
/// Queries several suppliers concurrently and reports each outcome as soon
/// as it is known.
/// </summary>
public interface ISupplierFanOut
{
    /// <summary>
    /// Starts every supplier call at once and yields outcomes in arrival
    /// order. The sequence ends when every supplier has answered or when
    /// <paramref name="budget"/> has elapsed, whichever comes first; a
    /// supplier that has not answered by then produces no outcome. Cancelling
    /// <paramref name="cancellationToken"/> stops the supplier calls and ends
    /// the sequence with an <see cref="OperationCanceledException"/>.
    /// </summary>
    IAsyncEnumerable<SupplierOutcome> QueryAsync(
        IReadOnlyCollection<ISupplier> suppliers,
        QuoteRequest request,
        TimeSpan budget,
        CancellationToken cancellationToken);
}
