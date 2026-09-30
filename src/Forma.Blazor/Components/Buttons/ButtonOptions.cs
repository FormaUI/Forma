namespace Forma.Blazor.Components.Buttons;

/// <summary>The appearance of a <see cref="FormaButton"/>.</summary>
public sealed record ButtonOptions
{
    /// <summary>A normally sized secondary button padded for text.</summary>
    public static ButtonOptions Default { get; } = new();

    /// <summary>A ghost button drawn square around a single glyph.</summary>
    public static ButtonOptions GhostIcon { get; } = new() { IconOnly = true, Variant = ButtonVariant.Ghost };

    /// <summary>A ghost icon button at the mini size.</summary>
    public static ButtonOptions MiniGhostIcon { get; } = GhostIcon with { Size = ButtonSize.Mini };

    /// <summary>The visual weight the button is drawn with.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Secondary;

    /// <summary>The size the button is drawn at.</summary>
    public ButtonSize Size { get; init; } = ButtonSize.Normal;

    /// <summary>Whether the button is drawn square around a single glyph.</summary>
    public bool IconOnly { get; init; }
}
