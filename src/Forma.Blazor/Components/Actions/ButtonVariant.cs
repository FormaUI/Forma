namespace Forma.Blazor.Components.Actions;

/// <summary>The visual weight a button carries relative to the others around it.</summary>
public enum ButtonVariant
{
    /// <summary>Outlined in the accent colour over a faint tint — the everyday button.</summary>
    Secondary,

    /// <summary>Solid accent fill for the one action a screen exists to perform.</summary>
    Primary,

    /// <summary>Small, lightly bordered button for repeated inline actions such as table row commands.</summary>
    Tertiary,

    /// <summary>Solid red fill for destructive actions.</summary>
    Danger,

    /// <summary>Borderless and transparent until hovered, for low-emphasis actions.</summary>
    Ghost
}
