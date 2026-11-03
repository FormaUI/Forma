namespace Forma.Framework.Icons;

/// <summary>An icon drawing a glyph in a variant.</summary>
/// <param name="Glyph">The glyph to draw.</param>
/// <param name="Variant">The variant to draw.</param>
public sealed record GlyphIcon(Glyph Glyph, GlyphVariant Variant) : Icon;
