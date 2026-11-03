namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Explorer.Actions;

/// <summary>What an explorer exposes about its items.</summary>
public interface IExplorerItemView
{
    /// <summary>The actions offered on every item.</summary>
    IReadOnlyList<ExplorerItemAction> Actions { get; }

    /// <summary>Which operations the explorer offers on the items it lists.</summary>
    ExplorerItemOptions ItemOptions { get; }

    /// <summary>The action awaiting confirmation.</summary>
    ExplorerPendingAction? PendingAction { get; }
}
