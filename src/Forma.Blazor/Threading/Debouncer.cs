namespace Forma.Blazor.Threading;

/// <summary>Delays until a period of inactivity, abandoning any wait a later call supersedes.</summary>
internal sealed class Debouncer : IDisposable
{
    private CancellationTokenSource? _pending;

    /// <summary>Waits out delay, returning true once it elapses, or false when a later call cancels this one first.</summary>
    public async Task<bool> WaitAsync(TimeSpan delay)
    {
        Cancel();
        _pending = new CancellationTokenSource();

        try
        {
            await Task.Delay(delay, _pending.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Abandons any wait in progress.</summary>
    public void Cancel()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    /// <inheritdoc />
    public void Dispose()
        => Cancel();
}
