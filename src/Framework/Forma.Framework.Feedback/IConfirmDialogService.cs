namespace Forma.Framework.Feedback;

using Forma.Framework.Events;


/// <summary>Coordinates a single active confirmation dialog and its awaited result.</summary>
public interface IConfirmDialogService : INotifyStateChanged
{
    /// <summary>The confirmation currently awaiting a response.</summary>
    ConfirmDialogRequest? CurrentRequest { get; }

    /// <summary>Shows a confirmation dialog and resolves to the user's choice.</summary>
    Task<bool> ConfirmAsync(
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default);
}
