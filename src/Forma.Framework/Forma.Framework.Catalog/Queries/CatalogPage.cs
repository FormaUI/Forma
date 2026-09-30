namespace Forma.Framework.Catalog.Queries;


/// <summary>The page of catalog items a provider returns for a <see cref="CatalogQuery"/>.</summary>
/// <param name="Items">The slice of the catalog items that should be processed.</param>
/// <param name="TotalCount">The total count of matching items.</param>
public sealed record CatalogPage(IReadOnlyList<ICatalogItem> Items, int? TotalCount)
{
    /// <summary>The page with no items.</summary>
    public static CatalogPage Empty { get; } = new([], 0);
}
