namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;

/// <summary>The data view's sort for each field the explorer's store sorts by, and the directions between the two.</summary>
internal static class ExplorerDataViewSorts
{
    // The store orders by the field, so a sort's key names the member it orders by and never orders the items itself.
    private static readonly Dictionary<CatalogSortField, DataViewSort<ICatalogItem>> SortsByField = new()
    {
        [CatalogSortField.Name] = DataViewSort<ICatalogItem>.ByAscending(item => item.Name),
        [CatalogSortField.Kind] = DataViewSort<ICatalogItem>.ByAscending(item => item.GetKind()),
        [CatalogSortField.Size] = DataViewSort<ICatalogItem>.ByAscending(item => item.Resource!.Size),
        [CatalogSortField.DateModified] = DataViewSort<ICatalogItem>.ByAscending(item => item.ModifiedAt),
        [CatalogSortField.DateAdded] = DataViewSort<ICatalogItem>.ByAscending(item => item.CreatedAt),
    };

    /// <summary>The name the sort by <paramref name="field"/> goes by in the data view.</summary>
    /// <param name="field">The field the store sorts by.</param>
    public static string GetName(CatalogSortField field)
        => field.ToString();

    /// <summary>The data view's sort by <paramref name="field"/>.</summary>
    /// <param name="field">The field the store sorts by.</param>
    public static DataViewSort<ICatalogItem> GetSort(CatalogSortField field)
        => SortsByField[field];

    /// <summary>The field <paramref name="sort"/> stands for, where it is one of the explorer's sorts.</summary>
    /// <param name="sort">The data view's sort.</param>
    public static CatalogSortField? GetField(DataViewSort<ICatalogItem>? sort)
    {
        foreach (var (field, fieldSort) in SortsByField)
        {
            if (ReferenceEquals(fieldSort, sort))
            {
                return field;
            }
        }

        return null;
    }

    /// <summary>The data view's direction for <paramref name="direction"/>.</summary>
    /// <param name="direction">The store's direction.</param>
    public static DataViewSortDirection ToDataViewDirection(CatalogSortDirection direction)
        => direction == CatalogSortDirection.Descending ? DataViewSortDirection.Descending : DataViewSortDirection.Ascending;

    /// <summary>The store's direction for <paramref name="direction"/>.</summary>
    /// <param name="direction">The data view's direction.</param>
    public static CatalogSortDirection ToCatalogDirection(DataViewSortDirection direction)
        => direction == DataViewSortDirection.Descending ? CatalogSortDirection.Descending : CatalogSortDirection.Ascending;
}
