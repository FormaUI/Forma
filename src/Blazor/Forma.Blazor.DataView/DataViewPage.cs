namespace Forma.Blazor.Components.DataView;

/// <summary>What a provider answers a request with.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="Items">The items of the slice.</param>
/// <param name="TotalCount">How many items the whole set holds.</param>
public sealed record DataViewPage<TItem>(IReadOnlyList<TItem> Items, int? TotalCount)
{
    /// <summary>How many of the source's positions the slice read, where null is one for each of its items.</summary>
    public int? ReadCount { get; init; }

    /// <summary>Whether the source holds positions past the slice, where null leaves it to the counts or to whether the slice read all it was asked for.</summary>
    public bool? HasMore { get; init; }

    /// <summary>How many positions the source holds, where its slices read positions they leave out and so differ from <see cref="DataViewPage{TItem}.TotalCount"/>.</summary>
    public int? PositionCount { get; init; }

    /// <summary>How many items each group holds, in the order the groups come in.</summary>
    public IReadOnlyList<DataViewGroupSize>? GroupSizes { get; init; }

    /// <summary>A page with no items and nothing to count.</summary>
    public static DataViewPage<TItem> Empty { get; } = new([], 0);
}
