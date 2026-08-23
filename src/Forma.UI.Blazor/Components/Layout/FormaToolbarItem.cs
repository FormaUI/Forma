namespace Forma.UI.Blazor.Components.Layout;

using Forma.UI.Style.Glyphs;

/// <summary>One command a <see cref="FormaToolbar"/> renders, alone or grouped with others.</summary>
public sealed record FormaToolbarItem
{
    /// <summary>The unique identifier of the item.</summary>
    public required string Id { get; init; }

    /// <summary>The item's label and accessible name, localized by the host.</summary>
    public required string Title { get; init; }

    /// <summary>The icon shown on the item, or null when its title stands alone.</summary>
    public Glyph? Icon { get; init; }

    /// <summary>The visual group the item is clustered with, separated from the next group by a rule.</summary>
    public string? Group { get; init; }

    /// <summary>The command invoked when the item is activated.</summary>
    public string? Command { get; init; }

    /// <summary>A component rendered in place of a standard toolbar button, or null.</summary>
    public Type? CustomComponent { get; init; }

    /// <summary>The options the item offers as a menu, or empty when the item is a plain command.</summary>
    public IReadOnlyList<FormaToolbarOption> Options { get; init; } = [];

    /// <summary>Whether the item reflects an on/off state rather than firing once.</summary>
    public bool IsToggle { get; init; }

    /// <summary>Whether a toggle item is currently on.</summary>
    public bool IsActive { get; init; }

    /// <summary>The shortcut shown in the item's tooltip, such as "Ctrl+B", or null.</summary>
    public string? ShortcutDisplay { get; init; }

    /// <summary>The shortcut in `aria-keyshortcuts` notation, such as "Control+B", or null.</summary>
    public string? ShortcutAriaKeys { get; init; }
}
