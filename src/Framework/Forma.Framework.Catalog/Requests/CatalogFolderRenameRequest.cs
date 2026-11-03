namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to rename a folder.</summary>
/// <param name="Folder">The folder to rename.</param>
/// <param name="NewName">The new name of the folder.</param>
public sealed record CatalogFolderRenameRequest(ICatalogFolderItem Folder, string NewName);
