namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to rename an item.</summary>
/// <param name="Item">The item to rename.</param>
/// <param name="NewName">The new name of the item.</param>
public sealed record CatalogRenameRequest(ICatalogItem Item, string NewName);
