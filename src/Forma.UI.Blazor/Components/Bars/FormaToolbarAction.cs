namespace Forma.UI.Blazor.Components.Bars;

/// <summary>A <see cref="FormaToolbarItem"/> that invokes a command when activated.</summary>
public abstract record FormaToolbarAction : FormaToolbarItem
{
    /// <summary>The command invoked when the item is activated.</summary>
    public required string Command { get; init; }

    /// <summary>The shortcut shown in the item's tooltip, such as "Ctrl+B", or null.</summary>
    public string? ShortcutDisplay { get; init; }

    /// <summary>The shortcut in `aria-keyshortcuts` notation, such as "Control+B", or null.</summary>
    public string? ShortcutAriaKeys { get; init; }
}
