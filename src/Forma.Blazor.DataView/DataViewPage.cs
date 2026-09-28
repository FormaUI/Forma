namespace Forma.Blazor.Components.DataView;

/// <summary>What a provider answers a request with: the items of the window, and the size of the whole set where the store can count it.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="Items">The items of the window, in the order asked for.</param>
/// <param name="TotalCount">How many items the whole set holds, or null where the store cannot count cheaply.</param>
public sealed record DataViewPage<TItem>(IReadOnlyList<TItem> Items, int? TotalCount)
{
    /// <summary>A page with no items and nothing to count.</summary>
    public static DataViewPage<TItem> Empty { get; } = new([], 0);
}
