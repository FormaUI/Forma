namespace Forma.Features.Explorer.Requests;

using Forma.Features.Explorer.Items;

/// <summary>Request to move a folder into another parent folder via drag-and-drop.</summary>
/// <param name="SourceFolder">The folder being moved.</param>
/// <param name="TargetFolder">The destination parent folder, or null for the top level.</param>
public sealed record ExplorerFolderDropRequest(IExplorerFolderItem SourceFolder, IExplorerFolderItem? TargetFolder);
