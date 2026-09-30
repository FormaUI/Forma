namespace Forma.Framework.Timing;

/// <summary>A debouncer that measures its delays on a clock.</summary>
/// <remarks>The action runs on the thread the delay elapses on, and an <see cref="OperationCanceledException"/> it throws completes the request.</remarks>
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

            // Only a request still pending once its delay elapses runs.
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
