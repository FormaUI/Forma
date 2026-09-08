namespace Forma.Blazor.Components.Feedback;

/// <summary>Default close and footer-action behavior of a <see cref="FormaDialog"/>.</summary>
public sealed record DialogOptions
{
    /// <summary>The scrim dismisses the dialog, and both default footer actions show and close it.</summary>
    public static DialogOptions Default { get; } = new();

    /// <summary>Whether clicking the scrim, outside the dialog, closes it.</summary>
    public bool CloseOnScrimClick { get; init; } = true;

    /// <summary>Whether the dialog closes after the default cancel action.</summary>
    public bool CloseOnCancel { get; init; } = true;

    /// <summary>Whether the dialog closes after the default confirm action.</summary>
    public bool CloseOnConfirm { get; init; } = true;

    /// <summary>Shows the default cancel action in the footer.</summary>
    public bool ShowCancelAction { get; init; } = true;

    /// <summary>Shows the default confirm action in the footer.</summary>
    public bool ShowConfirmAction { get; init; } = true;
}
