namespace Forma.Framework.Themes;

/// <summary>The color schemes a theme is drawn in, named as CSS names them.</summary>
[Flags]
public enum ColorSchemes
{
    /// <summary>No color scheme.</summary>
    None = 0,

    /// <summary>The light scheme: dark ink on light paper.</summary>
    Light = 1,

    /// <summary>The dark scheme: light ink on dark paper.</summary>
    Dark = 2,
}
