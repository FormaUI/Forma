namespace Forma.Blazor.Components.Feedback;

/// <summary>Default close and footer-action behavior of a <see cref="FormaModal"/>.</summary>
public sealed record FormaModalOptions
{
    /// <summary>The backdrop dismisses the modal, and both default footer actions show and close it.</summary>
    public static FormaModalOptions Default { get; } = new();

    /// <summary>Whether clicking the backdrop, outside the dialog, closes the modal.</summary>
    public bool CloseOnBackdropClick { get; init; } = true;

    /// <summary>Whether the modal closes after the default cancel action.</summary>
    public bool CloseOnCancel { get; init; } = true;

    /// <summary>Whether the modal closes after the default confirm action.</summary>
    public bool CloseOnConfirm { get; init; } = true;

    /// <summary>Shows the default cancel action in the footer.</summary>
    public bool ShowCancelAction { get; init; } = true;

    /// <summary>Shows the default confirm action in the footer.</summary>
    public bool ShowConfirmAction { get; init; } = true;
}
