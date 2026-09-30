namespace Forma.Framework.Catalog.Queries;

/// <summary>The provider of a catalog's items, one slice at a time.</summary>
public interface ICatalogProvider
{
    /// <summary>Gets the page of items the query asks for.</summary>
    ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken);
}
