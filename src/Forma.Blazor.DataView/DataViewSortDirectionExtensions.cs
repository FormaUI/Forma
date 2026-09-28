namespace Forma.Blazor.Components.DataView;

/// <summary>What a sort direction becomes when a sort is reversed.</summary>
internal static class DataViewSortDirectionExtensions
{
    /// <summary>The opposite direction.</summary>
    public static DataViewSortDirection Reverse(this DataViewSortDirection direction)
        => direction == DataViewSortDirection.Ascending ? DataViewSortDirection.Descending : DataViewSortDirection.Ascending;
}
