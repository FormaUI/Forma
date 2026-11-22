namespace Forma.Framework.Feedback;

using Forma.Framework.Css;
using Forma.Framework.Icons;

/// <summary>A named status badge and the colors it renders with.</summary>
public sealed record StatusBadgeStyle
{
    /// <summary>Creates a status badge with the given name and colors.</summary>
    public StatusBadgeStyle(string name, CssColorValue background, CssColorValue foreground)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(foreground);

        Name = name;
        Background = background;
        Foreground = foreground;
    }

    /// <summary>Background color of the badge.</summary>
    public CssColorValue Background { get; }

    /// <summary>Text color of the badge.</summary>
    public CssColorValue Foreground { get; }

    /// <summary>The marker icon shown before the badge's text.</summary>
    public Icon? Marker { get; init; }

    /// <summary>Name the badge is referenced by.</summary>
    public string Name { get; }
}
