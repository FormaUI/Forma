namespace Forma.Framework.Catalog.Queries;

/// <summary>The query for a slice of the catalog items in a folder.</summary>
/// <remarks>Items the sort ties stand in the ascending ordinal order of their keys, in either direction.</remarks>
/// <param name="FolderKey">The key of the folder whose items are asked for, where null is every folder's.</param>
/// <param name="Skip">How many matching items precede the slice.</param>
/// <param name="Take">How many items the slice holds.</param>
/// <param name="Search">The search text.</param>
/// <param name="SortBy">The field the items are sorted by.</param>
/// <param name="Direction">The direction the items are sorted in.</param>
public readonly record struct CatalogQuery(
    string? FolderKey,
    int Skip,
    int Take,
    string? Search,
    CatalogSortField SortBy,
    CatalogSortDirection Direction);
