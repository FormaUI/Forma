namespace Forma.UI.Blazor.Components.Actions;

/// <summary>Appearance of a <see cref="FormaButton"/>, for a component that lets its caller style a button it renders.</summary>
public sealed record FormaButtonOptions
{
    /// <summary>A normally sized secondary button padded for text.</summary>
    public static FormaButtonOptions Default { get; } = new();

    /// <summary>Visual weight the button is drawn with.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Secondary;

    /// <summary>Padding and text size the button is drawn at, or the square size when icon-only.</summary>
    public ButtonSize Size { get; init; } = ButtonSize.Normal;

    /// <summary>Draws the button square around a single glyph instead of padding it for text.</summary>
    public bool IconOnly { get; init; }
}
