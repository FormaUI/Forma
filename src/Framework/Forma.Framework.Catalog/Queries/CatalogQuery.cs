namespace Forma.Framework.Catalog.Queries;

/// <summary>The query for a slice of catalog items.</summary>
/// <param name="Skip">How many matching items precede the slice.</param>
/// <param name="Take">How many items the slice holds.</param>
/// <param name="Search">The search text.</param>
/// <param name="SortBy">The field the items are sorted by.</param>
/// <param name="Direction">The direction the items are sorted in.</param>
public readonly record struct CatalogQuery(
    int Skip,
    int Take,
    string? Search,
    CatalogSortField SortBy,
    CatalogSortDirection Direction);
