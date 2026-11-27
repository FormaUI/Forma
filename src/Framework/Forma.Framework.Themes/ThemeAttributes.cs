namespace Forma.Framework.Themes;

/// <summary>The two attributes an element names its theme and its color scheme in, and the value each scheme preference takes.</summary>
public static class ThemeAttributes
{
    /// <summary>The attribute naming the theme an element and everything inside it draws.</summary>
    public const string Theme = "data-forma-theme";

    /// <summary>The attribute naming the color scheme an element and everything inside it draws: <c>system</c>, <c>light</c> or <c>dark</c>.</summary>
    public const string Scheme = "data-forma-scheme";

    /// <summary>The value <see cref="Scheme"/> takes for <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme preference.</param>
    /// <returns><c>system</c>, <c>light</c> or <c>dark</c>.</returns>
    public static string GetSchemeValue(ColorSchemePreference scheme)
        => scheme switch
        {
            ColorSchemePreference.System => "system",
            ColorSchemePreference.Light => "light",
            ColorSchemePreference.Dark => "dark",
            _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null),
        };

    /// <summary>Reads the scheme preference <paramref name="value"/> names, as <see cref="Scheme"/> takes it.</summary>
    /// <param name="value">The attribute's value.</param>
    /// <param name="scheme">The scheme preference it names, where it names one.</param>
    /// <returns>Whether <paramref name="value"/> names a scheme preference.</returns>
    public static bool TryParseScheme(string? value, out ColorSchemePreference scheme)
    {
        switch (value)
        {
            case "system":
                scheme = ColorSchemePreference.System;
                return true;
            case "light":
                scheme = ColorSchemePreference.Light;
                return true;
            case "dark":
                scheme = ColorSchemePreference.Dark;
                return true;
            default:
                scheme = default;
                return false;
        }
    }

    /// <summary>The attributes an element carries to draw <paramref name="choice"/>: its theme, and the scheme <paramref name="registry"/> draws it in.</summary>
    /// <param name="registry">The themes registered.</param>
    /// <param name="choice">A choice naming a registered theme.</param>
    /// <returns>The two attributes, by name, for an element's <c>@attributes</c>.</returns>
    public static IReadOnlyDictionary<string, object> Of(ThemeRegistry registry, ThemeChoice choice)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(choice);

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Theme] = choice.Theme,
            [Scheme] = GetSchemeValue(registry.GetDrawnScheme(choice)),
        };
    }
}
