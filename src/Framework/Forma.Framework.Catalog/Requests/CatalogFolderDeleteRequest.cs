namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to delete a folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record CatalogFolderDeleteRequest(ICatalogFolderItem Folder);
