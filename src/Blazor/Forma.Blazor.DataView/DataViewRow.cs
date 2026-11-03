namespace Forma.Blazor.Components.DataView;

/// <summary>One row a virtualizing view draws: a group's heading, or the items standing across one row.</summary>
/// <typeparam name="TItem">The item the row holds.</typeparam>
/// <param name="Group">The group whose heading the row is, or null for a row of items.</param>
/// <param name="Items">The items across the row, empty for a heading.</param>
/// <param name="FirstItemIndex">The index in the whole set of the row's first item, or of the group's first item for a heading.</param>
internal sealed record DataViewRow<TItem>(DataViewGroup<TItem>? Group, IReadOnlyList<TItem> Items, int FirstItemIndex);
