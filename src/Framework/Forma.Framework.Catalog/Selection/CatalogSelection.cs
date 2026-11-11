namespace Forma.Framework.Catalog.Selection;


/// <summary>The items a surface has selected, with the operations that change them.</summary>
public sealed class CatalogSelection : ICatalogSelectionView
{
    private readonly Dictionary<string, ICatalogItem> _items = [with(StringComparer.Ordinal)];

    private readonly Dictionary<string, ICatalogItem> _folderItems = [with(StringComparer.Ordinal)];

    private IReadOnlyList<ICatalogItem>? _resolvedItems;

    /// <inheritdoc />
    public bool IsSelectionModeActive { get; set; }

    /// <summary>The maximum number of selected items, where zero is unlimited.</summary>
    public int SelectionLimit { get; set; }

    /// <inheritdoc />
    public int Count
        => _items.Count + _folderItems.Count;

    /// <inheritdoc />
    public IReadOnlyList<ICatalogItem> Items
        => _resolvedItems ??= [.. _folderItems.Values, .. _items.Values];

    /// <inheritdoc />
    public bool IsSelected(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item is ICatalogFolderItem ? _folderItems : _items).ContainsKey(item.Key);
    }

    /// <summary>Adds an enabled item to the selection, or removes it when it is already selected.</summary>
    public void Toggle(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDisabled)
        {
            return;
        }

        Invalidate();

        // A selection holds folders and other items together, its limit counting both.
        var items = item is ICatalogFolderItem ? _folderItems : _items;

        if (items.Remove(item.Key))
        {
            return;
        }

        if (SelectionLimit == 1)
        {
            _items.Clear();
            _folderItems.Clear();
            items[item.Key] = item;
        }
        else if (SelectionLimit <= 0 || Count < SelectionLimit)
        {
            items[item.Key] = item;
        }
    }

    /// <summary>Removes the item from the selection.</summary>
    public void Remove(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Invalidate();
        (item is ICatalogFolderItem ? _folderItems : _items).Remove(item.Key);
    }

    /// <summary>Empties the selection.</summary>
    public void Clear()
    {
        Invalidate();
        _items.Clear();
        _folderItems.Clear();
    }

    /// <summary>Replaces the selection with the items.</summary>
    public void Replace(IEnumerable<ICatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Invalidate();
        _items.Clear();
        _folderItems.Clear();

        foreach (var item in items)
        {
            (item is ICatalogFolderItem ? _folderItems : _items)[item.Key] = item;
        }
    }

    private void Invalidate()
        => _resolvedItems = null;
}
