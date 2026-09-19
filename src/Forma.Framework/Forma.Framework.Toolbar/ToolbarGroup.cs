namespace Forma.Framework.Toolbar;

/// <summary>A named cluster of <see cref="ToolbarItem"/>s, separated from its neighbors by a rule.</summary>
public sealed record ToolbarGroup : ToolbarItem
{
    /// <summary>The items the group contains; groups do not nest.</summary>
    public required IReadOnlyList<ToolbarItem> Items { get; init; }
}
