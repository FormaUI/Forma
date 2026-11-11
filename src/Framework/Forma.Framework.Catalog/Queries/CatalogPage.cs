namespace Forma.Framework.Catalog.Queries;

/// <summary>The page of catalog items a provider returns for a <see cref="CatalogQuery"/>.</summary>
/// <param name="Items">The slice of the catalog items that should be processed.</param>
/// <param name="TotalCount">How many items the store matched for the query, each a position a slice can read.</param>
public sealed record CatalogPage(IReadOnlyList<ICatalogItem> Items, int? TotalCount)
{
    /// <summary>The page with no items.</summary>
    public static CatalogPage Empty { get; } = new([], 0);

    /// <summary>How many store positions the slice read, where null is one for each of its items.</summary>
    public int? ReadCount { get; init; }

    /// <summary>Whether the store holds positions past the slice, where null leaves it to <see cref="TotalCount"/> or to whether the slice read all it was asked for.</summary>
    public bool? HasMore { get; init; }

    /// <summary>How many items of the whole query are listed, which a store gives where it applied the query's filter and left hidden items out before paging.</summary>
    public int? ListedCount { get; init; }

    /// <summary>How many listed items each group of the query's grouping holds across the whole query, in the order the groups stand.</summary>
    public IReadOnlyList<CatalogGroupSize>? GroupSizes { get; init; }

    // Whether positions follow the slice that answered the query, where the run has read the positions before it and this slice's. A
    // slice that read nothing ends the run, so a provider that makes no progress is never asked again.
    internal bool IsFollowed(CatalogQuery query, int runReadCount)
    {
        var read = ReadCount ?? Items.Count;

        return read > 0 && (HasMore ?? (TotalCount is { } total ? runReadCount < total : read >= query.Take));
    }
}
