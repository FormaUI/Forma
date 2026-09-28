namespace Forma.Blazor.Components.DataView;

/// <summary>The window of items a data view asks its provider for, with the grouping and the sort to apply and the token that cancels a request a newer one supersedes.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="StartIndex">The index of the first item asked for, counted from the start of the sorted set.</param>
/// <param name="Count">How many items are asked for, or null for every item from <paramref name="StartIndex"/> on.</param>
/// <param name="Grouping">The grouping whose key orders the items before <paramref name="Sort"/> does, so each group's items stand together, or null for no grouping.</param>
/// <param name="Sort">The sort to apply before the window is cut, or null for the source's own order.</param>
/// <param name="SortDirection">Whether <paramref name="Sort"/> applies as written or reversed.</param>
/// <param name="CancellationToken">Canceled when a newer request supersedes this one.</param>
public readonly record struct DataViewRequest<TItem>(
    int StartIndex,
    int? Count,
    DataViewGrouping<TItem>? Grouping,
    DataViewSort<TItem>? Sort,
    DataViewSortDirection SortDirection,
    CancellationToken CancellationToken)
{
    /// <summary>Orders <paramref name="source"/> as the request asks, the grouping's key first, or leaves it as it is when the request names neither.</summary>
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

    /// <summary>The grouping's key and the sort's keys as the members they read, in order, for a provider that writes its own query; empty when the request names neither.</summary>
    public IReadOnlyList<DataViewSortedProperty> GetSortedProperties()
    {
        var sorted = Sort?.GetSortedProperties(SortDirection) ?? [];

        return Grouping is null ? sorted : [Grouping.GetSortedProperty(), .. sorted];
    }
}
