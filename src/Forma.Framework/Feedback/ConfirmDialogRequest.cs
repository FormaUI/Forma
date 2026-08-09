namespace Forma.Framework.Feedback;

/// <summary>Describes an active confirmation dialog and carries its pending result.</summary>
public sealed class ConfirmDialogRequest
{
    /// <summary>Label for the button that rejects the confirmation.</summary>
    public required string CancelText { get; init; }

    /// <summary>Label for the button that accepts the confirmation.</summary>
    public required string ConfirmText { get; init; }

    /// <summary>The prompt shown to the user.</summary>
    public required string Message { get; init; }

    /// <summary>Completed with the user's choice when the dialog resolves.</summary>
    public required TaskCompletionSource<bool> Result { get; init; }
}
