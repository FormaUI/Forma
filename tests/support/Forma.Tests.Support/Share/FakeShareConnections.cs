namespace Forma.Tests.Share;

using Forma.Framework.Events;
using Forma.Framework.Share;

/// <summary>Share connections whose state a test sets directly, recording every connection started.</summary>
public sealed class FakeShareConnections : IShareConnections
{
    private readonly StateNotifier _notifier = new();

    /// <summary>The target ids the user counts as connected.</summary>
    public HashSet<string> Connected { get; } = new(StringComparer.Ordinal);

    /// <summary>Every target id a connection was started for, in order.</summary>
    public List<string> ConnectRequests { get; } = [];

    /// <inheritdoc />
    public bool IsConnected(string targetId)
        => Connected.Contains(targetId);

    /// <inheritdoc />
    public Task ConnectAsync(string targetId, CancellationToken cancellationToken = default)
    {
        ConnectRequests.Add(targetId);

        return Task.CompletedTask;
    }

    /// <summary>Marks the target connected and notifies the subscribers.</summary>
    public Task MarkConnectedAsync(string targetId)
    {
        Connected.Add(targetId);

        return _notifier.NotifyAsync();
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
