namespace Forma.Blazor.Core.Foundation;

using Forma.Blazor.Abstractions.Foundation;

/// <summary>Maintains a list of async callbacks and invokes them when notified.</summary>
public sealed class StateNotifier : INotifyStateChanged
{
    private readonly List<Func<Task>> _subscribers = [];

    /// <summary>Invokes every subscribed callback in registration order.</summary>
    public async Task NotifyAsync(CancellationToken cancellationToken = default)
    {
        foreach (var callback in _subscribers.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await callback().ConfigureAwait(false);
        }
    }

    /// <summary>Registers a callback invoked on each notification.</summary>
    public void Subscribe(Func<Task> callback)
        => _subscribers.Add(callback);

    /// <summary>Removes a previously registered callback.</summary>
    public void Unsubscribe(Func<Task> callback)
        => _subscribers.Remove(callback);
}
