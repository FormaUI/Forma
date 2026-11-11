namespace Forma.Framework.Catalog.Queries;

/// <summary>The query for a slice of the catalog items in a folder.</summary>
/// <remarks>Items the sort ties stand in the ascending ordinal order of their keys, in either direction.</remarks>
/// <param name="FolderKey">The key of the folder whose items are asked for, where null is every folder's.</param>
/// <param name="Skip">How many store positions precede the slice.</param>
/// <param name="Take">How many store positions the slice reads.</param>
/// <param name="Search">The search text.</param>
/// <param name="SortBy">The field the items are sorted by.</param>
/// <param name="Direction">The direction the items are sorted in.</param>
public readonly record struct CatalogQuery(
    string? FolderKey,
    int Skip,
    int Take,
    string? Search,
    CatalogSortField SortBy,
    CatalogSortDirection Direction)
{
    /// <summary>The filter the listed items pass, for a store that applies it before paging.</summary>
    public CatalogResourceFilter? Filter { get; init; }

    /// <summary>The kinds of item the query asks for, where null asks for every kind and an empty set matches no item.</summary>
    /// <remarks>
    /// A provider applies the kinds before it pages, so the slice, the positions it reads, its counts and whether more follow all
    /// describe the items of those kinds alone. A caller that drops an item of another kind from an answer only keeps it from the
    /// reader; it cannot make the answer of a provider that ignored the kinds whole.
    /// </remarks>
    public IReadOnlySet<CatalogItemKind>? Kinds { get; init; }

    /// <summary>The groups the items gather into, which order them before the sort does.</summary>
    public CatalogGrouping? Grouping { get; init; }

    /// <summary>Whether the page is to say how many items each group of <see cref="Grouping"/> holds across the whole query, in <see cref="CatalogPage.GroupSizes"/>.</summary>
    public bool IncludesGroupSizes { get; init; }

    /// <summary>Whether the query asks for items of <paramref name="kind"/>.</summary>
    /// <param name="kind">The kind of item.</param>
    public bool AsksFor(CatalogItemKind kind)
        => Kinds is not { } kinds || kinds.Contains(kind);
}
