namespace Forma.Blazor.Components.Buttons;

/// <summary>Appearance of a <see cref="FormaButton"/>, for a component that lets its caller style a button it renders.</summary>
public sealed record ButtonOptions
{
    /// <summary>A normally sized secondary button padded for text.</summary>
    public static ButtonOptions Default { get; } = new();

    /// <summary>A ghost button drawn square around a single glyph.</summary>
    public static ButtonOptions GhostIcon { get; } = new() { IconOnly = true, Variant = ButtonVariant.Ghost };

    /// <summary>A ghost icon button at the mini size, for a trigger standing in a dense bar.</summary>
    public static ButtonOptions MiniGhostIcon { get; } = GhostIcon with { Size = ButtonSize.Mini };

    /// <summary>Visual weight the button is drawn with.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Secondary;

    /// <summary>Padding and text size the button is drawn at, or the square size when icon-only.</summary>
    public ButtonSize Size { get; init; } = ButtonSize.Normal;

    /// <summary>Draws the button square around a single glyph instead of padding it for text.</summary>
    public bool IconOnly { get; init; }
}
