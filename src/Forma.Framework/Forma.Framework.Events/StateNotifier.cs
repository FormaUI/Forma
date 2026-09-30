namespace Forma.Framework.Events;

/// <summary>The subscribed callbacks of an object whose state changes, invoked on each notification.</summary>
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

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _subscribers.Add(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _subscribers.Remove(callback);
}
