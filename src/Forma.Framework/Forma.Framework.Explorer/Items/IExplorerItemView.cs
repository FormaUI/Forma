namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Explorer.Actions;

/// <summary>What an explorer exposes about its items.</summary>
public interface IExplorerItemView
{
    /// <summary>The operations offered on every item, in render order.</summary>
    IReadOnlyList<ExplorerItemAction> Actions { get; }

    /// <summary>Which operations the explorer offers on the items it lists.</summary>
    ExplorerItemOptions ItemOptions { get; }

    /// <summary>The action currently awaiting confirmation, or null when there is none.</summary>
    ExplorerPendingAction? PendingAction { get; }
}
