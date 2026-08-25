namespace Forma.UI.Design.Icons;

/// <summary>A semantic glyph together with its requested visual style.</summary>
/// <param name="Glyph">The glyph to draw.</param>
/// <param name="Style">The style to draw it in.</param>
public readonly record struct Icon(Glyph Glyph, GlyphStyle Style = GlyphStyle.Regular)
{
    /// <summary>Takes a glyph at its default style.</summary>
    /// <param name="glyph">The glyph to draw.</param>
    public static implicit operator Icon(Glyph glyph)
    {
        return new(glyph);
    }

    /// <summary>Takes a glyph drawn as a solid shape.</summary>
    /// <param name="glyph">The glyph to draw.</param>
    /// <returns>The glyph at <see cref="GlyphStyle.Filled"/>.</returns>
    public static Icon Filled(Glyph glyph)
        => new(glyph, GlyphStyle.Filled);
}
