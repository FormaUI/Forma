namespace Forma.UI.Design.Icons;

/// <summary>A semantic glyph together with its requested variant.</summary>
/// <param name="Glyph">The glyph to draw.</param>
/// <param name="Variant">The glyph variant to request.</param>
public readonly record struct Icon(
    Glyph Glyph,
    GlyphVariant Variant = GlyphVariant.Default)
{
    /// <summary>Takes a glyph in its default variant.</summary>
    public static implicit operator Icon(Glyph glyph)
    {
        return new(glyph);
    }

    /// <summary>Takes a glyph in its alternate variant.</summary>
    public static Icon Alternate(Glyph glyph)
        => new(glyph, GlyphVariant.Alternate);
}
