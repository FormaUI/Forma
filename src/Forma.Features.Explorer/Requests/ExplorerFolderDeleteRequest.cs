namespace Forma.Features.Explorer.Requests;

using Forma.Features.Explorer.Items;

/// <summary>Request to delete an existing folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record ExplorerFolderDeleteRequest(IExplorerFolderItem Folder);
