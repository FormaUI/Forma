namespace Forma.Blazor.Components.Feedback;

/// <summary>Default close and footer-action behavior of a <see cref="FormaDialog"/>.</summary>
public sealed record DialogOptions
{
    /// <summary>The default dialog options.</summary>
    public static DialogOptions Default { get; } = new();

    /// <summary>Whether clicking the scrim closes the dialog.</summary>
    public bool CloseOnScrimClick { get; init; } = true;

    /// <summary>Whether the dialog closes after the default cancel action.</summary>
    public bool CloseOnCancel { get; init; } = true;

    /// <summary>Whether the dialog closes after the default confirm action.</summary>
    public bool CloseOnConfirm { get; init; } = true;

    /// <summary>Whether the footer shows the default cancel action.</summary>
    public bool ShowCancelAction { get; init; } = true;

    /// <summary>Whether the footer shows the default confirm action.</summary>
    public bool ShowConfirmAction { get; init; } = true;
}
