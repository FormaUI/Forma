namespace Forma.Framework.Catalog.Queries;

/// <summary>How a query's items gather into groups: the field they are grouped by, and the groups in the order they stand.</summary>
/// <remarks>
/// An item falls in the first group that admits it, and a <see cref="CatalogRemainderGroup"/> takes whatever the other
/// groups do not, wherever it stands. Items stand in the order of their groups first, then in the query's sort, then in
/// the ascending ordinal order of their keys.
/// </remarks>
/// <param name="Field">The field the items are grouped by.</param>
/// <param name="Groups">The groups in the order they stand.</param>
public sealed record CatalogGrouping(CatalogGroupField Field, IReadOnlyList<CatalogGroup> Groups)
{
    /// <summary>The place of the group <paramref name="item"/> falls in.</summary>
    /// <param name="item">The item.</param>
    /// <exception cref="InvalidOperationException">No group admits the item.</exception>
    public int IndexOf(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var remainder = -1;

        for (var index = 0; index < Groups.Count; index++)
        {
            if (Groups[index] is CatalogRemainderGroup)
            {
                remainder = index;
            }
            else if (Groups[index].Admits(item, Field))
            {
                return index;
            }
        }

        return remainder >= 0 ? remainder : throw new InvalidOperationException($"No group admits \"{item.Key}\".");
    }

    /// <summary>The group <paramref name="item"/> falls in.</summary>
    /// <param name="item">The item.</param>
    public CatalogGroup GetGroup(ICatalogItem item)
        => Groups[IndexOf(item)];

    /// <summary>How many of <paramref name="items"/> fall in each group that holds any, in the order the groups stand.</summary>
    /// <param name="items">The items of the whole query.</param>
    public IReadOnlyList<CatalogGroupSize> CountGroups(IEnumerable<ICatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var counts = new int[Groups.Count];

        foreach (var item in items)
        {
            counts[IndexOf(item)]++;
        }

        return [.. Groups.Select((group, index) => new CatalogGroupSize(group.Key, counts[index])).Where(size => size.Count > 0)];
    }

    /// <summary>The item's date the field names.</summary>
    internal static DateTimeOffset? GetDate(ICatalogItem item, CatalogGroupField field)
        => field switch
        {
            CatalogGroupField.DateAdded => item.CreatedAt,
            CatalogGroupField.DateModified => item.ModifiedAt,
            _ => null
        };
}
