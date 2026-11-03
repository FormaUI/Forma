namespace Forma.Framework.Toolbar;

/// <summary>A <see cref="ToolbarItem"/> presenting a menu of choices.</summary>
public sealed record ToolbarMenu : ToolbarItem
{
    /// <summary>The options the menu presents.</summary>
    public required IReadOnlyList<ToolbarOption> Options { get; init; }
}
