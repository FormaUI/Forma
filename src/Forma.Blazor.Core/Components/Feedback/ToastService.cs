namespace Forma.Blazor.Components.Feedback;

using Forma.Blazor.Components;

/// <summary>In-memory <see cref="IToastService"/> that tracks active toasts and notifies subscribers on change.</summary>
public sealed class ToastService : IToastService
{
    private readonly StateNotifier _notifier = new();
    private readonly List<ToastMessage> _toasts = [];

    /// <summary>The currently visible toasts.</summary>
    public IReadOnlyList<ToastMessage> Toasts
        => _toasts;

    /// <summary>Removes the toast with the given identifier.</summary>
    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _toasts.RemoveAll(t => t.Id == id);
        await _notifier.NotifyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a toast with the given message, severity, and lifetime.</summary>
    public async Task ShowAsync(
        string message,
        ToastType type = ToastType.Success,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var actualDuration = duration ?? TimeSpan.FromSeconds(3);
        ArgumentOutOfRangeException.ThrowIfLessThan(actualDuration, TimeSpan.Zero);

        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Message = message,
            Type = type,
            Duration = actualDuration
        };

        _toasts.Add(toast);
        await _notifier.NotifyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Registers a callback invoked whenever the toast collection changes.</summary>
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <summary>Removes a previously registered callback.</summary>
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
