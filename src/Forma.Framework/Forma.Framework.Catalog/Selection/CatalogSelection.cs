namespace Forma.Framework.Catalog.Selection;


/// <summary>The items a surface has selected, and the rules deciding what a further choice does to them.</summary>
public sealed class CatalogSelection : ICatalogSelectionView
{
    // The selection outlives the window a provider-backed surface holds, so it keeps the items rather than keys into a list it may no longer have.
    private readonly Dictionary<string, ICatalogItem> _items = [with(StringComparer.Ordinal)];

    private readonly Dictionary<string, ICatalogItem> _folderItems = [with(StringComparer.Ordinal)];

    private IReadOnlyList<ICatalogItem>? _resolvedItems;

    /// <inheritdoc />
    public bool IsSelectionModeActive { get; set; }

    /// <summary>How many items may be selected at once; zero for unlimited.</summary>
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

        if (item is ICatalogFolderItem)
        {
            _items.Clear();

            if (!_folderItems.Remove(item.Key))
            {
                _folderItems[item.Key] = item;
            }

            return;
        }

        // Media and folders are deleted through different contracts, so only one kind is ever selected at a time.
        _folderItems.Clear();

        if (_items.Remove(item.Key))
        {
            return;
        }

        if (SelectionLimit == 1)
        {
            _items.Clear();
            _items[item.Key] = item;
        }
        else if (SelectionLimit <= 0 || _items.Count < SelectionLimit)
        {
            _items[item.Key] = item;
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

    /// <summary>Replaces the selection with the given items.</summary>
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
