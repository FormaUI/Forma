namespace Forma.Framework.Explorer.Items.Requests;


/// <summary>Request to move items into a target folder via drag-and-drop.</summary>
/// <param name="Items">The items being moved.</param>
/// <param name="TargetFolder">The destination folder receiving the items, or null for the explorer root.</param>
public sealed record ExplorerDropRequest(IReadOnlyList<IExplorerItem> Items, IExplorerFolderItem? TargetFolder);
