namespace Forma.Framework.Toolbar;

/// <summary>A <see cref="ToolbarItem"/> that invokes a command when activated.</summary>
public abstract record ToolbarAction : ToolbarItem
{
    /// <summary>The command invoked when the item is activated.</summary>
    public required string Command { get; init; }

    /// <summary>The shortcut shown in the item's tooltip, such as "Ctrl+B".</summary>
    public string? ShortcutDisplay { get; init; }

    /// <summary>The shortcut in `aria-keyshortcuts` notation, such as "Control+B".</summary>
    public string? ShortcutAriaKeys { get; init; }

    /// <summary>The state that must be active for the action to run.</summary>
    public string? RequiresState { get; init; }
}
