using Forma.Blazor.Abstractions.Components.Feedback;
using Forma.Blazor.Core.Foundation;

namespace Forma.Blazor.Core.Components.Feedback;

/// <summary><see cref="IConfirmDialogService"/> that tracks the active request and notifies subscribers on change.</summary>
public sealed class ConfirmDialogService : IConfirmDialogService
{
    private readonly StateNotifier _notifier = new();

    /// <summary>The confirmation currently awaiting a response, or <see langword="null"/> when none is active.</summary>
    public ConfirmDialogRequest? Current { get; private set; }

    /// <summary>Shows a confirmation dialog and resolves to the user's choice.</summary>
    public async Task<bool> ConfirmAsync(
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmText);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);

        Current?.Result.TrySetResult(false);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new ConfirmDialogRequest
        {
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
            Result = tcs
        };
        Current = request;

        await _notifier.NotifyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            // Clear only if this request is still the active one; a later
            // ConfirmAsync may have superseded it while we awaited.
            if (ReferenceEquals(Current, request))
            {
                Current = null;
            }
        }
    }

    /// <summary>Registers a callback invoked whenever the active request changes.</summary>
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <summary>Removes a previously registered callback.</summary>
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
