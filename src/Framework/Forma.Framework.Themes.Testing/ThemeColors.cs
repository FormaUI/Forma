namespace Forma.Framework.Themes.Testing;

using Forma.Framework.Css;

/// <summary>The colors a theme draws its tokens in, in one of its color schemes.</summary>
public sealed class ThemeColors
{
    /// <summary>Initializes the colors <paramref name="theme"/> draws <paramref name="tokens"/> in, in <paramref name="scheme"/>.</summary>
    /// <param name="theme">The theme's name, as <c>data-forma-theme</c> takes it.</param>
    /// <param name="scheme">The color scheme the tokens are drawn in, <see cref="ColorSchemes.Light"/> or <see cref="ColorSchemes.Dark"/>.</param>
    /// <param name="tokens">The color each token is drawn in, by the token's name.</param>
    public ThemeColors(string theme, ColorSchemes scheme, IReadOnlyDictionary<string, CssColor> tokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        ArgumentNullException.ThrowIfNull(tokens);

        if (scheme is not (ColorSchemes.Light or ColorSchemes.Dark))
        {
            throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "A theme's colors are drawn in one scheme, Light or Dark.");
        }

        Theme = theme;
        Scheme = scheme;
        Tokens = new Dictionary<string, CssColor>(tokens, StringComparer.Ordinal);
    }

    /// <summary>The theme's name.</summary>
    public string Theme { get; }

    /// <summary>The color scheme the tokens are drawn in.</summary>
    public ColorSchemes Scheme { get; }

    /// <summary>The color each token is drawn in, by the token's name.</summary>
    public IReadOnlyDictionary<string, CssColor> Tokens { get; }
}
