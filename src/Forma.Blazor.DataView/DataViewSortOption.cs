namespace Forma.Blazor.Components.DataView;

/// <summary>A sort a sort menu offers: the name the address carries it by, the label the reader sees, the keys it sorts by, and the direction it opens in.</summary>
/// <typeparam name="TItem">The item the sort orders.</typeparam>
/// <param name="Name">The name the sort goes by in the address, the same in every culture; an option naming a grid column's sort name is that column's sort, and choosing it marks the column's header.</param>
/// <param name="Label">The label the menu shows for the sort.</param>
/// <param name="Sort">The keys the items are sorted by.</param>
public sealed record DataViewSortOption<TItem>(string Name, string Label, DataViewSort<TItem> Sort)
    where TItem : notnull
{
    /// <summary>The direction the sort opens in when chosen.</summary>
    public DataViewSortDirection InitialDirection { get; init; }
}
