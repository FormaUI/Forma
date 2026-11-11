namespace Forma.Framework.Events;

/// <summary>A debouncer that measures its delays on a clock.</summary>
/// <remarks>The action runs on the thread the delay elapses on, and an <see cref="OperationCanceledException"/> it throws completes the request.</remarks>
/// <param name="clock">The clock the delays are measured on.</param>
public sealed class Debouncer(TimeProvider clock) : IDebounceable
{
    private CancellationTokenSource? _pendingCancellation;

    /// <summary>The clock the delays are measured on.</summary>
    public TimeProvider Clock { get; } = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public async ValueTask DebounceAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        Cancel();

        var pendingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = pendingCancellation.Token;
        _pendingCancellation = pendingCancellation;

        try
        {
            await Task.Delay(delay, Clock, token).ConfigureAwait(false);

            // Only a request still pending once its delay elapses runs.
            if (!ReferenceEquals(_pendingCancellation, pendingCancellation))
            {
                return;
            }

            await action(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (ReferenceEquals(_pendingCancellation, pendingCancellation))
            {
                _pendingCancellation = null;
                pendingCancellation.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Cancel()
    {
        _pendingCancellation?.Cancel();
        _pendingCancellation?.Dispose();
        _pendingCancellation = null;
    }
}
