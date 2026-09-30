namespace Forma.Blazor.Components.DataView;

/// <summary>What a provider answers a request with.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="Items">The items of the slice.</param>
/// <param name="TotalCount">How many items the whole set holds.</param>
public sealed record DataViewPage<TItem>(IReadOnlyList<TItem> Items, int? TotalCount)
{
    /// <summary>How many items each group holds, in the order the groups come in.</summary>
    public IReadOnlyList<DataViewGroupSize>? GroupSizes { get; init; }

    /// <summary>A page with no items and nothing to count.</summary>
    public static DataViewPage<TItem> Empty { get; } = new([], 0);
}
