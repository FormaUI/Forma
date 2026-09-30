namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to delete items.</summary>
/// <param name="Items">The items to delete.</param>
public sealed record CatalogDeleteRequest(IReadOnlyList<ICatalogItem> Items);
