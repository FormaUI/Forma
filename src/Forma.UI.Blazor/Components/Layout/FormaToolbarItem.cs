namespace Forma.UI.Blazor.Components.Layout;

using Forma.UI.Style.Glyphs;

/// <summary>An item a <see cref="FormaToolbar"/> renders.</summary>
public abstract record FormaToolbarItem
{
    /// <summary>The unique identifier of the item within its group.</summary>
    public required string Id { get; init; }

    /// <summary>The item's label and accessible name, localized by the host.</summary>
    public required string Title { get; init; }

    /// <summary>The hover text, or null to show Title on hover as well.</summary>
    public string? Tooltip { get; init; }

    /// <summary>The icon shown for the item, or null when its title stands alone.</summary>
    public Glyph? Icon { get; init; }
}
