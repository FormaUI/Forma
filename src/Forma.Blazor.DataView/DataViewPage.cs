namespace Forma.Blazor.Components.DataView;

/// <summary>What a provider answers a request with: the items of the slice, the size of the whole set where the store can count it, and the size of each group where the request asks for them.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="Items">The items of the slice, in the order asked for; fewer than asked for only where the set ends inside the slice, which is how a store that cannot count says where it ends.</param>
/// <param name="TotalCount">How many items the whole set holds, or null where the store cannot count cheaply.</param>
public sealed record DataViewPage<TItem>(IReadOnlyList<TItem> Items, int? TotalCount)
{
    /// <summary>How many items each group holds, in the order the groups come in, where the request's <see cref="DataViewRequest{TItem}.IncludesGroupSizes"/> asks for them.</summary>
    public IReadOnlyList<DataViewGroupSize>? GroupSizes { get; init; }

    /// <summary>A page with no items and nothing to count.</summary>
    public static DataViewPage<TItem> Empty { get; } = new([], 0);
}
