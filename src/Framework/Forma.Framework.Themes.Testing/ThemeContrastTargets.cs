namespace Forma.Framework.Themes.Testing;

/// <summary>The least contrast a theme's tokens are held to, the theme design's targets unless raised.</summary>
public sealed record ThemeContrastTargets
{
    /// <summary>The theme design's targets.</summary>
    public static ThemeContrastTargets Default { get; } = new();

    /// <summary>The least contrast ratio of primary text and quiet text to each backdrop and to the popover's surface.</summary>
    public double Text { get; init; } = 7;

    /// <summary>The least contrast ratio of secondary text to each backdrop and to the popover's surface.</summary>
    public double SecondaryText { get; init; } = 4.5;

    /// <summary>The least contrast ratio of each role's ink to each backdrop and to the popover's surface, and to its subtle tint on each, and of the primary ink to a quiet button's hover fill, a pressed toggle's label.</summary>
    public double RoleInk { get; init; } = 4.5;

    /// <summary>The least contrast ratio of each role's fg to its solid, its hover fill and its pressed fill.</summary>
    public double RoleForeground { get; init; } = 4.5;

    /// <summary>The least contrast ratio of a field's border, and of the focus ring, to each backdrop and to the popover's surface.</summary>
    public double Boundary { get; init; } = 3;

    /// <summary>The least distance in OKLab, times 100, between adjacent chart series in protanopia and in deuteranopia.</summary>
    public double SeriesSeparation { get; init; } = 8;

    /// <summary>The least distance in OKLab, times 100, between adjacent chart series in full color vision.</summary>
    public double SeriesSeparationInFullVision { get; init; } = 15;
}
