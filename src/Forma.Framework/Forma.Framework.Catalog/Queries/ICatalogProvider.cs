namespace Forma.Framework.Catalog.Queries;

/// <summary>Answers a surface with one slice of a catalog's items at a time.</summary>
public interface ICatalogProvider
{
    /// <summary>Answers the slice the query asks for.</summary>
    ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken);
}
