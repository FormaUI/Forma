namespace Forma.Framework.Themes;

/// <summary>The color scheme a reader prefers a theme drawn in: the device's, light or dark.</summary>
public enum ColorSchemePreference
{
    /// <summary>The scheme the device prefers, which the browser resolves.</summary>
    System,

    /// <summary>The light scheme.</summary>
    Light,

    /// <summary>The dark scheme.</summary>
    Dark,
}
