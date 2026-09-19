namespace Forma.Framework.Catalog.Queries;

/// <summary>The window of items a surface is asking for, described by the state the surface itself owns.</summary>
/// <param name="Skip">How many matching items to pass over before the window begins.</param>
/// <param name="Take">How many items the window holds.</param>
/// <param name="Search">What the reader typed into the surface's search box, or null when it is empty.</param>
/// <param name="SortBy">Which field the reader is ordering by.</param>
/// <param name="Direction">Which way that order runs.</param>
public readonly record struct CatalogQuery(
    int Skip,
    int Take,
    string? Search,
    CatalogSortField SortBy,
    CatalogSortDirection Direction);
