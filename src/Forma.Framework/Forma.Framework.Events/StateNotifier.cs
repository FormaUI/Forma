namespace Forma.Framework.Events;

/// <summary>The subscribed callbacks of an object whose state changes, invoked on each notification.</summary>
/// <remarks>
/// The callbacks are kept in the order they subscribed, each indexed by the callback itself, so a callback
/// subscribes and unsubscribes in constant time however many are subscribed, and a callback subscribed again
/// keeps its place.
/// </remarks>
public sealed class StateNotifier : INotifyStateChanged
{
    private readonly LinkedList<Func<Task>> _subscribers = new();
    private readonly Dictionary<Func<Task>, LinkedListNode<Func<Task>>> _subscriberNodes = [];

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
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!_subscriberNodes.ContainsKey(callback))
        {
            _subscriberNodes.Add(callback, _subscribers.AddLast(callback));
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_subscriberNodes.Remove(callback, out var node))
        {
            _subscribers.Remove(node);
        }
    }
}
