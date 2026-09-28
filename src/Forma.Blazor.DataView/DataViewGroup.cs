namespace Forma.Blazor.Components.DataView;

/// <summary>A group of the items on screen: the key they fold under, the name its heading shows, and the items standing under it.</summary>
/// <typeparam name="TItem">The item the group holds.</typeparam>
/// <param name="Key">The key the group's items share.</param>
/// <param name="Name">The name the heading shows.</param>
/// <param name="Items">The group's items, in the order shown.</param>
public sealed record DataViewGroup<TItem>(object? Key, string Name, IReadOnlyList<TItem> Items);
