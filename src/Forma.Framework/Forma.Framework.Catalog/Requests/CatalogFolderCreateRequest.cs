namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to create a folder.</summary>
/// <param name="ParentFolder">The parent folder of the new folder, where null is the top level.</param>
/// <param name="Name">The name of the new folder.</param>
public sealed record CatalogFolderCreateRequest(ICatalogFolderItem? ParentFolder, string Name);
