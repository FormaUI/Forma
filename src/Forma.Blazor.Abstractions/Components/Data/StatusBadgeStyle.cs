using Forma.Web.Css;

namespace Forma.Blazor.Abstractions.Components.Data;

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

    /// <summary>Name the badge is referenced by.</summary>
    public string Name { get; }
}
