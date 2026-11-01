namespace Forma.Framework.Events;

/// <summary>The subscribed callbacks of an object whose state changes, invoked on each notification.</summary>
/// <remarks>
/// The callbacks are kept in the order they subscribed, each indexed by the callback itself, so a callback
/// subscribes and unsubscribes in constant time however many are subscribed, and a callback subscribed again
/// keeps its place. Callbacks subscribe and unsubscribe from any thread, and a notification invokes those
/// subscribed as it began.
/// </remarks>
public sealed class StateNotifier : INotifyStateChanged
{
    private readonly Lock _lock = new();
    private readonly LinkedList<Func<Task>> _subscribers = new();
    private readonly Dictionary<Func<Task>, LinkedListNode<Func<Task>>> _subscriberNodes = [];

    /// <summary>Invokes every subscribed callback in registration order.</summary>
    /// <remarks>Each callback completes before the next is invoked, and one that fails leaves the rest invoked: the notification then faults with an <see cref="AggregateException"/> holding every failure, once the last callback completed.</remarks>
    public async Task NotifyAsync(CancellationToken cancellationToken = default)
    {
        List<Exception>? failures = null;

        foreach (var callback in GetSubscribers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var notification = InvokeAsync(callback);
            await notification.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            if (!notification.IsCompletedSuccessfully)
            {
                (failures ??= []).AddRange(notification.Exception?.InnerExceptions ?? [new TaskCanceledException(notification)]);
            }
        }

        if (failures is not null)
        {
            throw new AggregateException(failures);
        }
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (_lock)
        {
            if (!_subscriberNodes.ContainsKey(callback))
            {
                _subscriberNodes.Add(callback, _subscribers.AddLast(callback));
            }
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (_lock)
        {
            if (_subscriberNodes.Remove(callback, out var node))
            {
                _subscribers.Remove(node);
            }
        }
    }

    // A callback that throws before returning its task fails as one whose task faulted.
    private static async Task InvokeAsync(Func<Task> callback)
        => await callback().ConfigureAwait(false);

    private Func<Task>[] GetSubscribers()
    {
        lock (_lock)
        {
            return [.. _subscribers];
        }
    }
}
