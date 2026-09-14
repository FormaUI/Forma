namespace Forma.Framework.Explorer.Requests;

using Forma.Framework.Explorer.Items;

/// <summary>Request to delete an existing folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record ExplorerFolderDeleteRequest(IExplorerFolderItem Folder);
