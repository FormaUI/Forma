namespace Forma.Blazor.Components.Buttons;

/// <summary>The visual weight a button carries relative to the others around it.</summary>
public enum ButtonVariant
{
    /// <summary>A neutral fill with a light border.</summary>
    Secondary,

    /// <summary>A solid accent fill.</summary>
    Primary,

    /// <summary>Small and lightly bordered over a transparent fill.</summary>
    Tertiary,

    /// <summary>A solid red fill for a destructive action.</summary>
    Danger,

    /// <summary>Borderless and transparent until hovered.</summary>
    Ghost
}
