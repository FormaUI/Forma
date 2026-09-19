namespace Forma.Framework.Catalog.Requests;


/// <summary>Request to move items into a target folder via drag-and-drop.</summary>
/// <param name="Items">The items being moved.</param>
/// <param name="TargetFolder">The destination folder receiving the items, or null for the catalog's root.</param>
public sealed record CatalogDropRequest(IReadOnlyList<ICatalogItem> Items, ICatalogFolderItem? TargetFolder);
