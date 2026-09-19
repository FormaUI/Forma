namespace Forma.Framework.Explorer.Items.Requests;


/// <summary>Request to rename an item.</summary>
/// <param name="Item">The item to rename.</param>
/// <param name="NewName">The new name requested for the item.</param>
public sealed record ExplorerRenameRequest(IExplorerItem Item, string NewName);
