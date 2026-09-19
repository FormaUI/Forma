namespace Forma.Framework.Catalog.Queries;

/// <summary>Answers a surface with one window of a catalog's items at a time, for catalogs too large to hold.</summary>
public interface ICatalogProvider
{
    /// <summary>The window the query asks for. Anything the surface does not own, such as the folder being browsed, belongs to the implementation.</summary>
    ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken);
}
