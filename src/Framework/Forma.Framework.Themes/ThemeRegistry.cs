namespace Forma.Framework.Themes;

using System.Diagnostics.CodeAnalysis;

/// <summary>The themes an application registered and the choice a reader starts in, which resolve a choice to one the page can draw.</summary>
public sealed class ThemeRegistry
{
    private readonly Dictionary<string, ThemeDefinition> _themesByName;

    /// <summary>Creates the registry of what <paramref name="options"/> registers.</summary>
    /// <param name="options">The themes registered and the default choice.</param>
    /// <exception cref="InvalidOperationException">The default choice names a theme that isn't registered.</exception>
    public ThemeRegistry(ThemeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Themes = [.. options.Themes];
        _themesByName = Themes.ToDictionary(theme => theme.Name, StringComparer.Ordinal);
        Default = options.Default;

        if (!_themesByName.ContainsKey(Default.Theme))
        {
            throw new InvalidOperationException($"The default theme choice names {Default.Theme}, which isn't registered; register it, or name a registered theme as the default.");
        }
    }

    /// <summary>The themes registered, in the order a reader is offered them.</summary>
    public IReadOnlyList<ThemeDefinition> Themes { get; }

    /// <summary>The choice a reader starts in until they choose.</summary>
    public ThemeChoice Default { get; }

    /// <summary>Finds the theme named <paramref name="name"/>.</summary>
    /// <param name="name">The theme's name.</param>
    /// <param name="theme">The theme of that name, where one is registered.</param>
    /// <returns>Whether a theme of that name is registered.</returns>
    public bool TryGetTheme(string name, [NotNullWhen(true)] out ThemeDefinition? theme)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _themesByName.TryGetValue(name, out theme);
    }

    /// <summary>Resolves <paramref name="choice"/> to one naming a registered theme: the choice itself, or the default theme in the choice's scheme preference.</summary>
    /// <param name="choice">The choice to resolve.</param>
    /// <returns>A choice naming a registered theme, in the scheme preference <paramref name="choice"/> holds.</returns>
    public ThemeChoice Resolve(ThemeChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        return _themesByName.ContainsKey(choice.Theme) ? choice : new ThemeChoice(Default.Theme, choice.Scheme);
    }

    /// <summary>The scheme <paramref name="choice"/> is drawn in: its preference, or the light scheme where its theme draws no dark one.</summary>
    /// <param name="choice">A choice naming a registered theme.</param>
    /// <returns>The scheme the page draws the choice in.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="choice"/> names a theme that isn't registered.</exception>
    public ColorSchemePreference GetDrawnScheme(ThemeChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        var theme = _themesByName.TryGetValue(choice.Theme, out var registered)
            ? registered
            : throw new InvalidOperationException($"The choice names {choice.Theme}, which isn't registered; resolve it first.");

        return theme.Schemes.HasFlag(ColorSchemes.Dark) ? choice.Scheme : ColorSchemePreference.Light;
    }
}
