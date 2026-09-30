namespace Forma.Framework.Toolbar;

using Forma.Framework.Icons;

/// <summary>An item a toolbar renders.</summary>
public abstract record ToolbarItem
{
    /// <summary>The unique identifier of the item within its group.</summary>
    public required string Id { get; init; }

    /// <summary>The item's label and accessible name, localized by the host.</summary>
    public required string Title { get; init; }

    /// <summary>The item's tooltip text, where null shows the title.</summary>
    public string? Tooltip { get; init; }

    /// <summary>The icon shown for the item.</summary>
    public Icon? Icon { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public bool IsDisabled { get; init; }
}
