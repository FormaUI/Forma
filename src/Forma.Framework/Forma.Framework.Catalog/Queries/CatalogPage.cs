namespace Forma.Framework.Catalog.Queries;


/// <summary>What a provider answers a <see cref="CatalogQuery"/> with.</summary>
/// <param name="Items">The slice of the catalog items that should be processed.</param>
/// <param name="TotalCount">How many items match in all.</param>
public sealed record CatalogPage(IReadOnlyList<ICatalogItem> Items, int? TotalCount)
{
    /// <summary>Nothing to show.</summary>
    public static CatalogPage Empty { get; } = new([], 0);
}
