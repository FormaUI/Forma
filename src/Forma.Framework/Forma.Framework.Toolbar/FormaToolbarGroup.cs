namespace Forma.Framework.Toolbar;

/// <summary>A named cluster of <see cref="FormaToolbarItem"/>s, separated from its neighbors by a rule.</summary>
public sealed record FormaToolbarGroup : FormaToolbarItem
{
    /// <summary>The items the group contains; groups do not nest.</summary>
    public required IReadOnlyList<FormaToolbarItem> Items { get; init; }
}
