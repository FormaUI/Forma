namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Icons;

/// <summary>One operation an explorer offers on a single item.</summary>
public sealed record ExplorerItemAction
{
    /// <summary>Stable identifier, rendered as a data attribute so callers can target the action.</summary>
    public required string Id { get; init; }

    /// <summary>The glyph rendered inside the action's button.</summary>
    public required Icon Icon { get; init; }

    /// <summary>Produces the localized, item-independent label shown for the action.</summary>
    public required Func<string> Label { get; init; }

    /// <summary>The capabilities an item must support for the action to be offered on it.</summary>
    public ExplorerItemCapabilities Requires { get; init; }

    /// <summary>Runs the operation against an item.</summary>
    public required Func<IExplorerItem, Task> InvokeAsync { get; init; }

    /// <summary>Whether the operation destroys data and reads as destructive.</summary>
    public bool IsDestructive { get; init; }
}
