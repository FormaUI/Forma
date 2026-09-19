namespace Forma.Framework.Explorer.Items.Requests;


/// <summary>Request to create a new folder under an optional parent folder.</summary>
/// <param name="ParentFolder">The parent folder under which the new folder will be created, or null for top-level.</param>
/// <param name="Name">The display name of the new folder.</param>
public sealed record ExplorerFolderCreateRequest(IExplorerFolderItem? ParentFolder, string Name);
