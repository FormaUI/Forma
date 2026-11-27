namespace Forma.Framework.Themes;

/// <summary>What a reader chose: a theme, and the color scheme they prefer it drawn in.</summary>
public sealed record ThemeChoice
{
    /// <summary>Creates the choice of <paramref name="theme"/> in <paramref name="scheme"/>.</summary>
    /// <param name="theme">The theme's name, as <c>data-forma-theme</c> takes it.</param>
    /// <param name="scheme">The color scheme the reader prefers.</param>
    public ThemeChoice(string theme, ColorSchemePreference scheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);

        if (!Enum.IsDefined(scheme))
        {
            throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
        }

        Theme = theme;
        Scheme = scheme;
    }

    /// <summary>The theme's name, as <c>data-forma-theme</c> takes it.</summary>
    public string Theme { get; }

    /// <summary>The color scheme the reader prefers.</summary>
    public ColorSchemePreference Scheme { get; }
}
