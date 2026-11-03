namespace Forma.Blazor.Components.DataView;

using System.Globalization;
using System.Linq.Expressions;

using Forma.Framework.Culture;

/// <summary>A grouping over items: the key their groups fold under, the order the groups come in, and the name a group's heading shows.</summary>
/// <typeparam name="TItem">The item the key reads.</typeparam>
public sealed class DataViewGrouping<TItem>
{
    private readonly DataViewSort<TItem> _order;
    private readonly Func<TItem, object?> _key;
    private readonly Func<object?, CultureInfo, string> _nameSelector;
    private readonly Func<IQueryable<TItem>, IQueryable<DataViewGroupSize>> _count;

    private DataViewGrouping(DataViewSort<TItem> order, Func<TItem, object?> key, Func<object?, CultureInfo, string> nameSelector, Func<IQueryable<TItem>, IQueryable<DataViewGroupSize>> count)
    {
        _order = order;
        _key = key;
        _nameSelector = nameSelector;
        _count = count;
    }

    /// <summary>Groups by <paramref name="key"/>, the groups coming smallest key first.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item's group is decided by.</param>
    /// <param name="name">The name a group's heading shows for its key, where null shows the key's own text.</param>
    public static DataViewGrouping<TItem> By<TValue>(Expression<Func<TItem, TValue>> key, Func<TValue, string>? name = null)
        => Create(key, DataViewSort<TItem>.ByAscending(key), name, descending: false);

    /// <summary>Groups by <paramref name="key"/>, the groups coming largest key first.</summary>
    /// <typeparam name="TValue">The key's value.</typeparam>
    /// <param name="key">The value an item's group is decided by.</param>
    /// <param name="name">The name a group's heading shows for its key, where null shows the key's own text.</param>
    public static DataViewGrouping<TItem> ByDescending<TValue>(Expression<Func<TItem, TValue>> key, Func<TValue, string>? name = null)
        => Create(key, DataViewSort<TItem>.ByDescending(key), name, descending: true);

    /// <summary>Orders <paramref name="source"/> so that each group's items stand together, in the order the groups come in.</summary>
    /// <param name="source">The items to order.</param>
    public IOrderedQueryable<TItem> Apply(IQueryable<TItem> source)
        => _order.Apply(source, DataViewSortDirection.Ascending);

    /// <summary>Counts the items of each group of <paramref name="source"/>, the groups in the order they come in, as a query the source's store runs.</summary>
    /// <param name="source">The items to count.</param>
    public IQueryable<DataViewGroupSize> CountGroups(IQueryable<TItem> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return _count(source);
    }

    /// <summary>The key as the member it reads, with the direction the groups come in.</summary>
    public DataViewSortedProperty GetSortedProperty()
        => _order.GetSortedProperties(DataViewSortDirection.Ascending)[0];

    /// <summary>The key of the group <paramref name="item"/> belongs to.</summary>
    /// <param name="item">The item.</param>
    public object? GetKey(TItem item)
        => _key(item);

    /// <summary>Creates the group of <paramref name="key"/> over <paramref name="items"/>, which names itself by this grouping's name callback in the culture state's cultures.</summary>
    /// <param name="key">The key the group's items share.</param>
    /// <param name="items">The group's items on screen.</param>
    /// <param name="cultureState">The culture state the group names itself in.</param>
    internal DataViewGroup<TItem> CreateGroup(object? key, IReadOnlyList<TItem> items, ICultureState cultureState)
        => new(key, _nameSelector, items, cultureState);

    private static DataViewGrouping<TItem> Create<TValue>(Expression<Func<TItem, TValue>> key, DataViewSort<TItem> order, Func<TValue, string>? name, bool descending)
    {
        ArgumentNullException.ThrowIfNull(key);

        var read = key.Compile();

        return new DataViewGrouping<TItem>(
            order,
            item => read(item),
            name is null
                ? (value, culture) => Convert.ToString(value, culture) ?? string.Empty
                : (value, _) => name((TValue)value!),
            source => CountGroups(source, key, descending));
    }

    // The store groups, orders and counts, so the groups come in the order its own sort puts their items in.
    private static IQueryable<DataViewGroupSize> CountGroups<TValue>(IQueryable<TItem> source, Expression<Func<TItem, TValue>> key, bool descending)
    {
        var groups = source.GroupBy(key);
        var ordered = descending ? groups.OrderByDescending(group => group.Key) : groups.OrderBy(group => group.Key);

        return ordered.Select(group => new DataViewGroupSize(group.Key, group.Count()));
    }
}
