namespace Forma.Framework.Timing;

/// <summary>A debouncer measuring its delays on a clock, the system's unless another is given.</summary>
/// <remarks>The action runs where the delay elapsed, not where it was requested. A cancelled action's <see cref="OperationCanceledException"/> completes the request quietly.</remarks>
public sealed class Debouncer : IDebounceable
{
    private CancellationTokenSource? _pending;

    /// <summary>The clock the delays are measured on.</summary>
    public TimeProvider Clock { get; init; } = TimeProvider.System;

    /// <inheritdoc />
    public async ValueTask DebounceAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        Cancel();

        var pending = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = pending.Token;
        _pending = pending;

        try
        {
            await Task.Delay(delay, Clock, token).ConfigureAwait(false);

            // A request cancelled or superseded while its delay elapsed does not run.
            if (!ReferenceEquals(_pending, pending))
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
            if (ReferenceEquals(_pending, pending))
            {
                _pending = null;
                pending.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Cancel()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }
}
