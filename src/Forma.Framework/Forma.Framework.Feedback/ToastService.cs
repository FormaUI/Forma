namespace Forma.Framework.Feedback;

using Forma.Framework.Events;


/// <summary>The in-memory <see cref="IToastService"/>.</summary>
public sealed class ToastService : IToastService
{
    private readonly StateNotifier _notifier = new();
    private readonly List<ToastMessage> _toasts = [];

    /// <inheritdoc />
    public IReadOnlyList<ToastMessage> Toasts
        => _toasts;

    /// <inheritdoc />
    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _toasts.RemoveAll(t => t.Id == id);
        await _notifier.NotifyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
