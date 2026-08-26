namespace Forma.UI.Features.Toolbar;

/// <summary>A <see cref="FormaToolbarItem"/> presenting a menu of choices.</summary>
public sealed record FormaToolbarMenu : FormaToolbarItem
{
    /// <summary>The options the menu presents; at least one is required.</summary>
    public required IReadOnlyList<FormaToolbarOption> Options { get; init; }
}
