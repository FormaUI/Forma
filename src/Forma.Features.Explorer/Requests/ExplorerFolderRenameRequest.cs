namespace Forma.Features.Explorer.Requests;

using Forma.Features.Explorer.Items;

/// <summary>Request to rename an existing folder.</summary>
/// <param name="Folder">The folder being renamed.</param>
/// <param name="NewName">The new display name for the folder.</param>
public sealed record ExplorerFolderRenameRequest(IExplorerFolderItem Folder, string NewName);
