namespace Forma.Framework.Catalog.Requests;


/// <summary>Request to delete an existing folder.</summary>
/// <param name="Folder">The folder to delete.</param>
public sealed record CatalogFolderDeleteRequest(ICatalogFolderItem Folder);
