namespace Forma.Framework.Themes;

using Forma.Framework.Localization;

/// <summary>A theme a host offers its readers: its name, the name a reader sees, the color schemes it draws and its stylesheet.</summary>
public sealed record ThemeDefinition
{
    /// <summary>Creates the definition of the theme <paramref name="name"/>.</summary>
    /// <param name="name">The theme's name, as <c>data-forma-theme</c> takes it.</param>
    /// <param name="displayName">The theme's name as a reader sees it.</param>
    /// <param name="schemes">The color schemes the theme draws, its light scheme among them.</param>
    /// <param name="stylesheet">The address of the stylesheet holding the theme's rule, as a page's head links it.</param>
    public ThemeDefinition(string name, LocalizableText displayName, ColorSchemes schemes, string stylesheet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stylesheet);

        if (!schemes.HasFlag(ColorSchemes.Light) || (schemes & ~(ColorSchemes.Light | ColorSchemes.Dark)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemes), schemes, "A theme draws a light scheme, and a dark one beside it where it has one.");
        }

        Name = name;
        DisplayName = displayName;
        Schemes = schemes;
        Stylesheet = stylesheet;
    }

    /// <summary>The theme's name, as <c>data-forma-theme</c> takes it.</summary>
    public string Name { get; }

    /// <summary>The theme's name as a reader sees it.</summary>
    public LocalizableText DisplayName { get; }

    /// <summary>The color schemes the theme draws, its light scheme among them.</summary>
    public ColorSchemes Schemes { get; }

    /// <summary>The address of the stylesheet holding the theme's rule, as a page's head links it.</summary>
    public string Stylesheet { get; }
}
