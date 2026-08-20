namespace Forma.UI.Blazor.Components.Buttons;

/// <summary>The visual weight a button carries relative to the others around it.</summary>
public enum ButtonVariant
{
    /// <summary>A neutral surface with a light border — the everyday button.</summary>
    Secondary,

    /// <summary>Solid accent fill for the one action a screen exists to perform.</summary>
    Primary,

    /// <summary>Small and lightly bordered over a transparent fill, for repeated inline actions such as table row commands.</summary>
    Tertiary,

    /// <summary>Solid red fill for destructive actions.</summary>
    Danger,

    /// <summary>Borderless and transparent until hovered, for low-emphasis actions.</summary>
    Ghost
}
