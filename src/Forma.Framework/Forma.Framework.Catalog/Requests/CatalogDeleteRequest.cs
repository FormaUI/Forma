namespace Forma.Framework.Catalog.Requests;


/// <summary>Request to delete one or more items.</summary>
/// <param name="Items">The items to delete.</param>
public sealed record CatalogDeleteRequest(IReadOnlyList<ICatalogItem> Items);
