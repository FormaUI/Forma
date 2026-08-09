namespace Forma.Blazor.Components.Feedback;

using Forma.Blazor.Components;

/// <summary>Coordinates a single active confirmation dialog and its awaited result.</summary>
public interface IConfirmDialogService : INotifyStateChanged
{
    /// <summary>The confirmation currently awaiting a response, or <see langword="null"/> when none is active.</summary>
    ConfirmDialogRequest? Current { get; }

    /// <summary>Shows a confirmation dialog and resolves to the user's choice.</summary>
    Task<bool> ConfirmAsync(
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default);
}
