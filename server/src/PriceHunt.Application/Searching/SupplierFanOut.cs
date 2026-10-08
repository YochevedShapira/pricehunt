using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Application.Searching;

/// <summary>
/// Fan-out / fan-in over suppliers built on a <see cref="Channel{T}"/>:
/// one producer task per supplier, a single consumer that yields outcomes
/// in the order they arrive. The timeout and caller cancellation are
/// combined into one token that every supplier call receives, so stopping
/// the search stops the work.
/// </summary>
public sealed partial class SupplierFanOut : ISupplierFanOut
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SupplierFanOut> _logger;

    public SupplierFanOut(TimeProvider timeProvider, ILogger<SupplierFanOut> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async IAsyncEnumerable<SupplierOutcome> QueryAsync(
        IReadOnlyCollection<ISupplier> suppliers,
        PriceRequest request,
        TimeSpan timeout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        using var timeoutExpiry = new CancellationTokenSource(timeout, _timeProvider);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutExpiry.Token);

        var outcomes = Channel.CreateUnbounded<SupplierOutcome>(
            new UnboundedChannelOptions { SingleReader = true });

        // Whatever ends the search (timeout or caller) also ends the stream,
        // even if a misbehaving supplier ignores its cancellation token.
        using var closeOnStop = stop.Token.Register(() => outcomes.Writer.TryComplete());

        StartProducers(suppliers, request, outcomes.Writer, stop.Token);

        try
        {
            await foreach (var outcome in outcomes.Reader.ReadAllAsync(CancellationToken.None))
            {
                yield return outcome;
            }
        }
        finally
        {
            // A consumer that stops enumerating early must not leave
            // supplier calls running until the timeout expires.
            await stop.CancelAsync().ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private void StartProducers(
        IReadOnlyCollection<ISupplier> suppliers,
        PriceRequest request,
        ChannelWriter<SupplierOutcome> writer,
        CancellationToken stopToken)
    {
        var producers = suppliers
            .Select(s => QueryOneAsync(s, request, writer, stopToken))
            .ToArray();

        _ = CloseWhenAllAnsweredAsync(producers, writer);
    }

    private static async Task CloseWhenAllAnsweredAsync(
        IEnumerable<Task> producers,
        ChannelWriter<SupplierOutcome> writer)
    {
        await Task.WhenAll(producers).ConfigureAwait(false);
        writer.TryComplete();
    }

    /// <summary>
    /// Runs one supplier call to completion and writes its outcome. Never
    /// throws: a failure of this supplier must not affect the others, and a
    /// cancelled call simply produces no outcome.
    /// </summary>
    private async Task QueryOneAsync(
        ISupplier supplier,
        PriceRequest request,
        ChannelWriter<SupplierOutcome> writer,
        CancellationToken stopToken)
    {
        SupplierOutcome outcome;
        try
        {
            var price = await supplier.GetPriceAsync(request, stopToken).ConfigureAwait(false);
            outcome = new SupplierPriced(supplier.Name, price);
        }
        catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
        {
            LogSupplierStopped(supplier.Name);
            return;
        }
        catch (SupplierUnavailableException ex)
        {
            LogSupplierUnavailable(supplier.Name, ex.Message);
            outcome = new SupplierFailure(supplier.Name, ex.Message);
        }
#pragma warning disable CA1031 // Isolation requirement: one supplier's bug must not end the search.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSupplierCrashed(ex, supplier.Name);
            outcome = new SupplierFailure(
                supplier.Name, "The supplier failed unexpectedly.");
        }

        // TryWrite is false once the stream has been closed by the timeout or
        // the caller; a late outcome is deliberately dropped.
        writer.TryWrite(outcome);
    }

    [LoggerMessage(LogLevel.Debug, "Supplier {Supplier} stopped before answering.")]
    private partial void LogSupplierStopped(string supplier);

    [LoggerMessage(LogLevel.Information, "Supplier {Supplier} is unavailable: {Reason}")]
    private partial void LogSupplierUnavailable(string supplier, string reason);

    [LoggerMessage(LogLevel.Error, "Supplier {Supplier} threw an unexpected exception.")]
    private partial void LogSupplierCrashed(Exception exception, string supplier);
}
