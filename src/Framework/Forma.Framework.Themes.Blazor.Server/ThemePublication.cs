namespace Forma.Framework.Themes.Blazor.Server;

using System.Globalization;

/// <summary>What a server-rendered page publishes of its theme: the theme marker the page's coordinator reads, and the stylesheets of the registered themes.</summary>
public static class ThemePublication
{
    /// <summary>The name of the theme marker's <c>meta</c> element.</summary>
    public const string MarkerName = "forma-theme";

    /// <summary>The attribute naming the theme a stylesheet's <c>link</c> holds, by which the page's coordinator confirms each registered theme.</summary>
    public const string StylesheetAttribute = "data-forma-theme-stylesheet";

    /// <summary>The theme marker's attributes: the choice the server resolved, the store's revision it rests on, and, for a page given its choice, that the page keeps nothing.</summary>
    /// <param name="theme">The request's theme.</param>
    /// <returns>The marker's attributes by name, a present attribute that has no value holding the empty string.</returns>
    public static IReadOnlyDictionary<string, string> GetMarkerAttributes(RequestTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["data-theme"] = theme.Choice.Theme,
            ["data-scheme"] = ThemeAttributes.GetSchemeValue(theme.Choice.Scheme),
            ["data-revision"] = theme.Revision.ToString(CultureInfo.InvariantCulture),
        };

        if (!theme.KeepsChoice)
        {
            attributes["data-keeps-nothing"] = string.Empty;
        }

        return attributes;
    }
}
