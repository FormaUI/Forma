namespace Forma.Framework.Icons;

/// <summary>The size an icon is drawn at.</summary>
public enum IconSize
{
    /// <summary>The size of the surrounding text, for a glyph inline with it.</summary>
    Inline,

    /// <summary>A glyph on a button, in a toolbar or in a dense row: a menu item, a grid cell, a toast marker.</summary>
    Small,

    /// <summary>A glyph that leads a panel, a tile or a picker cell.</summary>
    Medium,

    /// <summary>A section's figure.</summary>
    Large,

    /// <summary>The figure of an empty state, a drop zone or a media facade.</summary>
    Display,
}
