namespace Forma.Blazor.Components.DataView;

using System.Linq.Expressions;

/// <summary>A sort over items: a first key and any further keys, each ascending or descending, applied as written or reversed.</summary>
/// <typeparam name="TItem">The item the keys read.</typeparam>
public sealed class DataViewSort<TItem>
{
    private readonly List<SortKey> _keys = [];

    private DataViewSort()
    {
    }

    /// <summary>Begins a sort ordering by <paramref name="key"/>, smallest first.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item is ordered by.</param>
    public static DataViewSort<TItem> ByAscending<TValue>(Expression<Func<TItem, TValue>> key)
        => new DataViewSort<TItem>().ThenAscending(key);

    /// <summary>Begins a sort ordering by <paramref name="key"/>, largest first.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item is ordered by.</param>
    public static DataViewSort<TItem> ByDescending<TValue>(Expression<Func<TItem, TValue>> key)
        => new DataViewSort<TItem>().ThenDescending(key);

    /// <summary>Adds <paramref name="key"/> as the next key, smallest first, deciding between items the keys before it order alike.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item is ordered by.</param>
    public DataViewSort<TItem> ThenAscending<TValue>(Expression<Func<TItem, TValue>> key)
        => Add(key, DataViewSortDirection.Ascending);

    /// <summary>Adds <paramref name="key"/> as the next key, largest first, deciding between items the keys before it order alike.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item is ordered by.</param>
    public DataViewSort<TItem> ThenDescending<TValue>(Expression<Func<TItem, TValue>> key)
        => Add(key, DataViewSortDirection.Descending);

    /// <summary>Orders <paramref name="source"/> by every key, as written or with every key reversed.</summary>
    /// <param name="source">The items to order.</param>
    /// <param name="direction">Whether the keys apply as written or reversed.</param>
    public IOrderedQueryable<TItem> Apply(IQueryable<TItem> source, DataViewSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(source);

        var isReversed = direction == DataViewSortDirection.Descending;
        var orderedItems = _keys[0].Order(source, isReversed);

        for (var index = 1; index < _keys.Count; index++)
        {
            orderedItems = _keys[index].ThenOrder(orderedItems, isReversed);
        }

        return orderedItems;
    }

    /// <summary>Orders <paramref name="orderedItems"/> further by every key, deciding between items the order before it ranks alike, as written or with every key reversed.</summary>
    /// <param name="orderedItems">The items already ordered.</param>
    /// <param name="direction">Whether the keys apply as written or reversed.</param>
    public IOrderedQueryable<TItem> ThenApply(IOrderedQueryable<TItem> orderedItems, DataViewSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(orderedItems);

        var isReversed = direction == DataViewSortDirection.Descending;

        foreach (var key in _keys)
        {
            orderedItems = key.ThenOrder(orderedItems, isReversed);
        }

        return orderedItems;
    }

    /// <summary>The keys as the members they read, in order, each with the direction it applies in as written or reversed.</summary>
    /// <param name="direction">Whether the keys apply as written or reversed.</param>
    public IReadOnlyList<DataViewSortedProperty> GetSortedProperties(DataViewSortDirection direction)
    {
        var isReversed = direction == DataViewSortDirection.Descending;

        return [.. _keys.Select(key => new DataViewSortedProperty(key.PropertyName, isReversed ? key.Direction.Reverse() : key.Direction))];
    }

    private DataViewSort<TItem> Add<TValue>(Expression<Func<TItem, TValue>> key, DataViewSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(key);

        var isAscending = direction == DataViewSortDirection.Ascending;
        _keys.Add(new SortKey(
            GetPropertyName(key),
            direction,
            (source, reversed) => isAscending != reversed ? source.OrderBy(key) : source.OrderByDescending(key),
            (source, reversed) => isAscending != reversed ? source.ThenBy(key) : source.ThenByDescending(key)));

        return this;
    }

    // The member path the key reads, such as Address.City, and empty for a key that reads no member.
    private static string GetPropertyName<TValue>(Expression<Func<TItem, TValue>> key)
    {
        var parts = new List<string>();
        var body = key.Body;

        while (true)
        {
            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
            {
                body = conversion.Operand;
                continue;
            }

            if (body is MemberExpression member)
            {
                parts.Insert(0, member.Member.Name);
                body = member.Expression!;
                continue;
            }

            break;
        }

        return string.Join('.', parts);
    }

    private sealed record SortKey(
        string PropertyName,
        DataViewSortDirection Direction,
        Func<IQueryable<TItem>, bool, IOrderedQueryable<TItem>> Order,
        Func<IOrderedQueryable<TItem>, bool, IOrderedQueryable<TItem>> ThenOrder);
}
