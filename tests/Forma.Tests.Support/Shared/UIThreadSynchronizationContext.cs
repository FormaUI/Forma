namespace Forma.Tests.Shared;

using System.Collections.Concurrent;

/// <summary>A synchronization context that runs what is posted to it on a thread of its own, one item at a time in the order posted, as a UI thread does, each item in the thread's own flow.</summary>
public sealed class UIThreadSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _work = [];
    private readonly Thread _thread;

    /// <summary>Creates the context and starts its thread.</summary>
    public UIThreadSynchronizationContext()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = nameof(UIThreadSynchronizationContext) };
        _thread.Start();
    }

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object? state)
        => _work.Add((d, state));

    /// <summary>Runs <paramref name="function"/> on the thread, under this context.</summary>
    /// <returns>A task that completes with what <paramref name="function"/> returned once the thread ran it.</returns>
    public Task<T> RunAsync<T>(Func<T> function)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(_ => completion.SetResult(function()), null);

        return completion.Task;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _work.CompleteAdding();
        _thread.Join();
        _work.Dispose();
    }

    private void Run()
    {
        SetSynchronizationContext(this);

        foreach (var (callback, state) in _work.GetConsumingEnumerable())
        {
            callback(state);
        }
    }
}
