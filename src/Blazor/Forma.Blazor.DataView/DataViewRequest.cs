namespace Forma.Blazor.Components.DataView;

/// <summary>The slice of items a data view asks its provider for.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="StartIndex">The index of the first item asked for.</param>
/// <param name="Count">How many items are asked for.</param>
/// <param name="Parent">The node whose content or children are asked for, where null is the roots.</param>
/// <param name="Grouping">The grouping that orders the items.</param>
/// <param name="Sort">The sort to apply.</param>
/// <param name="SortDirection">Whether <paramref name="Sort"/> applies as written or reversed.</param>
/// <param name="CancellationToken">Cancels the request.</param>
public readonly record struct DataViewRequest<TItem>(
    int StartIndex,
    int? Count,
    TItem? Parent,
    DataViewGrouping<TItem>? Grouping,
    DataViewSort<TItem>? Sort,
    DataViewSortDirection SortDirection,
    CancellationToken CancellationToken)
{
    /// <summary>Whether the request asks for the content the views beside the tree show or for the children the tree shows, which a provider tells apart even at the roots.</summary>
    public DataViewRequestKind Kind { get; init; }

    /// <summary>Whether the answer carries the size of every group in <see cref="DataViewPage{TItem}.GroupSizes"/>.</summary>
    public bool IncludesGroupSizes { get; init; }

    /// <summary>Counts the items of each group of <paramref name="source"/> in the order the groups come in.</summary>
    /// <param name="source">The provider's own items.</param>
    public IQueryable<DataViewGroupSize> CountGroups(IQueryable<TItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Grouping is { } grouping
            ? grouping.CountGroups(source)
            : throw new InvalidOperationException("The request names no grouping to count.");
    }

    /// <summary>Orders <paramref name="source"/> as the request asks, the grouping's key first.</summary>
    /// <param name="source">The provider's own items.</param>
    public IQueryable<TItem> ApplySorting(IQueryable<TItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (Grouping is null)
        {
            return Sort is null ? source : Sort.Apply(source, SortDirection);
        }

        var grouped = Grouping.Apply(source);

        return Sort is null ? grouped : Sort.ThenApply(grouped, SortDirection);
    }

    /// <summary>The grouping's key and the sort's keys as the members they read, in order.</summary>
    public IReadOnlyList<DataViewSortedProperty> GetSortedProperties()
    {
        var sorted = Sort?.GetSortedProperties(SortDirection) ?? [];

        return Grouping is null ? sorted : [Grouping.GetSortedProperty(), .. sorted];
    }
}
