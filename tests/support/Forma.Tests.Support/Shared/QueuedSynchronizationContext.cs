namespace Forma.Tests.Shared;

/// <summary>A synchronization context that holds what is posted to it until the test runs it, so a continuation posted to it runs where the test asks.</summary>
public sealed class QueuedSynchronizationContext : SynchronizationContext
{
    private readonly Lock _lock = new();
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _posted = new();

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        lock (_lock)
        {
            _posted.Enqueue((d, state));
        }
    }

    /// <summary>Runs what was posted, and what that posts in turn, on the calling thread with the context current, until nothing is left.</summary>
    public void RunPending()
    {
        using var scope = MakeCurrent();

        while (TryTake(out var callback, out var state))
        {
            callback(state);
        }
    }

    /// <summary>Makes the context the calling thread's current one until the returned scope is disposed, which restores the one before it.</summary>
    /// <returns>The scope.</returns>
    public IDisposable MakeCurrent()
    {
        var previousContext = Current;
        SetSynchronizationContext(this);

        return new CurrentScope(previousContext);
    }

    private bool TryTake(out SendOrPostCallback callback, out object? state)
    {
        lock (_lock)
        {
            if (_posted.TryDequeue(out var item))
            {
                (callback, state) = item;
                return true;
            }
        }

        callback = default!;
        state = null;
        return false;
    }

    private sealed class CurrentScope(SynchronizationContext? previousContext) : IDisposable
    {
        public void Dispose()
            => SetSynchronizationContext(previousContext);
    }
}
