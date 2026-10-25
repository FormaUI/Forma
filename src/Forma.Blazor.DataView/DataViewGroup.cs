namespace Forma.Blazor.Components.DataView;

/// <summary>A group of the data view's items: the key they fold under, the items of it on screen, and how many items it holds.</summary>
/// <typeparam name="TItem">The item the group holds.</typeparam>
/// <param name="Key">The key the group's items share.</param>
/// <param name="Items">The group's items on screen, in the order shown, where empty means a virtualizing view fetches them as it scrolls.</param>
public sealed record DataViewGroup<TItem>(object? Key, IReadOnlyList<TItem> Items)
{
    /// <summary>How many items the group holds.</summary>
    public int Count { get; init; } = Items.Count;
}
