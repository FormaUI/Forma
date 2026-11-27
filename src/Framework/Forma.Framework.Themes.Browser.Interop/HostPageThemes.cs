namespace Forma.Framework.Themes.Browser.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>What a standalone app's host page declares of its themes: each theme its stylesheets name, and the default choice its bootstrap names.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
internal sealed record HostPageThemes
{
    /// <summary>The themes the page's stylesheets name.</summary>
    public IReadOnlyList<string>? Themes { get; init; }

    /// <summary>The default theme the bootstrap names.</summary>
    public string? DefaultTheme { get; init; }

    /// <summary>The default scheme preference the bootstrap names.</summary>
    public string? DefaultScheme { get; init; }

    /// <summary>Each way the page differs from <paramref name="registry"/>: a registered theme it links no stylesheet for, a stylesheet naming a theme the registration doesn't, and a default other than the registration's.</summary>
    /// <param name="registry">The themes registered and the default choice.</param>
    /// <returns>Each difference, written for the exception that names it, in that order.</returns>
    public IReadOnlyList<string> FindDifferences(ThemeRegistry registry)
    {
        var linked = Themes ?? [];
        var differences = new List<string>();

        foreach (var theme in registry.Themes.Where(theme => !linked.Contains(theme.Name, StringComparer.Ordinal)))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"the host page links no stylesheet for {theme.Name}, which the registration names"));
        }

        foreach (var name in linked.Where(name => !registry.TryGetTheme(name, out _)))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"the host page links a stylesheet for {name}, which the registration doesn't name"));
        }

        if (!string.Equals(DefaultTheme, registry.Default.Theme, StringComparison.Ordinal))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"the host page's default theme is {DefaultTheme}, where the registration's is {registry.Default.Theme}"));
        }

        var defaultScheme = ThemeAttributes.GetSchemeValue(registry.Default.Scheme);

        if (!string.Equals(DefaultScheme, defaultScheme, StringComparison.Ordinal))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"the host page's default scheme is {DefaultScheme}, where the registration's is {defaultScheme}"));
        }

        return differences;
    }
}
