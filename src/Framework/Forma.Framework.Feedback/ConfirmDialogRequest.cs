namespace Forma.Framework.Feedback;

/// <summary>An active confirmation dialog and its pending result.</summary>
public sealed class ConfirmDialogRequest
{
    /// <summary>Label for the button that rejects the confirmation.</summary>
    public required string CancelText { get; init; }

    /// <summary>Label for the button that accepts the confirmation.</summary>
    public required string ConfirmText { get; init; }

    /// <summary>The prompt shown to the user.</summary>
    public required string Message { get; init; }

    /// <summary>The pending result that carries the user's choice.</summary>
    public required TaskCompletionSource<bool> Result { get; init; }
}
