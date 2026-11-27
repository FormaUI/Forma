namespace Forma.Framework.Themes;

/// <summary>The themes a host registers and the choice a reader starts in, which <c>AddFormaThemes</c> configures.</summary>
public sealed class ThemeOptions
{
    private readonly List<ThemeDefinition> _themes = [];

    /// <summary>The themes registered, in the order a reader is offered them.</summary>
    public IReadOnlyList<ThemeDefinition> Themes
        => _themes;

    /// <summary>The choice a reader starts in until they choose: Forma's theme in the System preference unless the host sets another.</summary>
    public ThemeChoice Default { get; set; } = new("forma", ColorSchemePreference.System);

    /// <summary>Registers <paramref name="theme"/> after the themes registered before it.</summary>
    /// <param name="theme">The theme to register.</param>
    /// <exception cref="InvalidOperationException">A theme of the same name is registered already.</exception>
    public void Add(ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (_themes.Exists(registered => string.Equals(registered.Name, theme.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"A theme named {theme.Name} is registered already; remove it first to register another in its place.");
        }

        _themes.Add(theme);
    }

    /// <summary>Removes the theme named <paramref name="name"/>.</summary>
    /// <param name="name">The theme's name.</param>
    /// <returns>Whether a theme of that name was registered.</returns>
    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _themes.RemoveAll(theme => string.Equals(theme.Name, name, StringComparison.Ordinal)) > 0;
    }

    /// <summary>Removes every theme registered.</summary>
    public void Clear()
        => _themes.Clear();
}
