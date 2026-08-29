namespace Forma.UI.Design.Icons;

using System.Text;

/// <summary>What an icon draws: a glyph in a variant, a brand mark, or a single character.</summary>
public abstract record Icon
{
    /// <summary>A glyph in a variant.</summary>
    /// <param name="glyph">The glyph to draw.</param>
    /// <param name="variant">The representation to request.</param>
    public static Icon FromGlyph(Glyph glyph, GlyphVariant variant = GlyphVariant.Default)
        => new GlyphIcon(glyph, variant);

    /// <summary>A brand mark.</summary>
    /// <param name="brand">The brand whose mark to draw.</param>
    public static Icon FromBrand(Brand brand)
        => new BrandIcon(brand);

    /// <summary>A single character set by the font.</summary>
    /// <param name="character">The character to draw.</param>
    public static Icon FromCharacter(Rune character)
        => new CharacterIcon(character);

    /// <summary>Takes a glyph in its default variant.</summary>
    public static implicit operator Icon(Glyph glyph)
    {
        return FromGlyph(glyph);
    }

    /// <summary>Takes a brand mark.</summary>
    public static implicit operator Icon(Brand brand)
    {
        return FromBrand(brand);
    }

    /// <summary>Takes a character.</summary>
    public static implicit operator Icon(Rune character)
    {
        return FromCharacter(character);
    }

    /// <summary>Closes the hierarchy to the icon kinds this project defines.</summary>
    private protected Icon()
    {
    }
}
