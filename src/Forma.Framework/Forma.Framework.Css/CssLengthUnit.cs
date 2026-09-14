namespace Forma.Framework.Css;

/// <summary>The unit a <see cref="CssLength"/> is measured in.</summary>
public enum CssLengthUnit
{
    /// <summary>The width of the font's zero glyph, which sizes a field by the characters it holds.</summary>
    Ch,

    /// <summary>Relative to the element's own font size.</summary>
    Em,

    /// <summary>A share of the containing block, from 0 to 100.</summary>
    Percent,

    /// <summary>An absolute pixel.</summary>
    Px,

    /// <summary>Relative to the root font size.</summary>
    Rem,
}
