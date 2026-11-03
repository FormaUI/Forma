namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

using Forma.Framework.Icons;

/// <summary>One operation an explorer offers on a single item.</summary>
public sealed record ExplorerItemAction
{
    /// <summary>The stable identifier of the action.</summary>
    public required string Id { get; init; }

    /// <summary>The glyph rendered inside the action's button.</summary>
    public required Icon Icon { get; init; }

    /// <summary>The localized label of the action.</summary>
    public required Func<string> Label { get; init; }

    /// <summary>The capabilities an item must support for the action to be offered on it.</summary>
    public CatalogItemCapabilities Requires { get; init; }

    /// <summary>Runs the operation against an item.</summary>
    public required Func<ICatalogItem, Task> InvokeAsync { get; init; }

    /// <summary>Whether the operation is destructive.</summary>
    public bool IsDestructive { get; init; }
}
