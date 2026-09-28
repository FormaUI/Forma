namespace Forma.Blazor.Components.DataView;

/// <summary>The window of items a data view asks its provider for, with the sort to apply and the token that cancels a request a newer one supersedes.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="StartIndex">The index of the first item asked for, counted from the start of the sorted set.</param>
/// <param name="Count">How many items are asked for, or null for every item from <paramref name="StartIndex"/> on.</param>
/// <param name="Sort">The sort to apply before the window is cut, or null for the source's own order.</param>
/// <param name="SortDirection">Whether <paramref name="Sort"/> applies as written or reversed.</param>
/// <param name="CancellationToken">Canceled when a newer request supersedes this one.</param>
public readonly record struct DataViewRequest<TItem>(
    int StartIndex,
    int? Count,
    DataViewSort<TItem>? Sort,
    DataViewSortDirection SortDirection,
    CancellationToken CancellationToken)
{
    /// <summary>Orders <paramref name="source"/> as the request asks, or leaves it as it is when the request names no sort.</summary>
    /// <param name="source">The provider's own items.</param>
    public IQueryable<TItem> ApplySorting(IQueryable<TItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Sort is null ? source : Sort.Apply(source, SortDirection);
    }

    /// <summary>The sort's keys as the members they read, for a provider that writes its own query; empty when the request names no sort.</summary>
    public IReadOnlyList<DataViewSortedProperty> GetSortedProperties()
        => Sort?.GetSortedProperties(SortDirection) ?? [];
}
