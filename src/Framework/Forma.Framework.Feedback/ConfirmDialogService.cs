namespace Forma.Framework.Feedback;

using Forma.Framework.Events;


/// <summary>The in-memory <see cref="IConfirmDialogService"/>.</summary>
public sealed class ConfirmDialogService : IConfirmDialogService
{
    private readonly StateNotifier _notifier = new();

    /// <inheritdoc />
    public ConfirmDialogRequest? CurrentRequest { get; private set; }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmText);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);

        CurrentRequest?.Result.TrySetResult(false);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new ConfirmDialogRequest
        {
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            Result = tcs
        };
        CurrentRequest = request;

        await _notifier.NotifyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            // A later ConfirmAsync may have superseded this request while it was awaited.
            if (ReferenceEquals(CurrentRequest, request))
            {
                CurrentRequest = null;
            }
        }
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
