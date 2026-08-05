namespace Forma.Blazor.Abstractions.Components.Data;

using Forma.Web.Css;

/// <summary>A named status badge and the colours it renders with.</summary>
public sealed record StatusBadgeStyle
{
    /// <summary>Creates a status badge with the given name and colours.</summary>
    public StatusBadgeStyle(string name, CssColor background, CssColor foreground)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Background = background;
        Foreground = foreground;
    }

    /// <summary>Background colour of the badge.</summary>
    public CssColor Background { get; }

    /// <summary>Text colour of the badge.</summary>
    public CssColor Foreground { get; }

    /// <summary>Glyph shown before the text, so the status reads without colour.</summary>
    public string? Marker { get; init; }

    /// <summary>Name the badge is referenced by.</summary>
    public string Name { get; }
}
