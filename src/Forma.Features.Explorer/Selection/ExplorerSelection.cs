namespace Forma.Features.Explorer.Selection;

using Forma.Features.Explorer.Items;

/// <summary>The items an explorer has selected, and the rules deciding what a further choice does to them.</summary>
public sealed class ExplorerSelection : IExplorerSelectionView
{
    // The selection outlives the window a provider-backed explorer holds, so it keeps the items rather than keys into a list it may no longer have.
    private readonly Dictionary<string, IExplorerItem> _items = new(StringComparer.Ordinal);

    private readonly Dictionary<string, IExplorerItem> _folderItems = new(StringComparer.Ordinal);

    private IReadOnlyList<IExplorerItem>? _resolvedItems;

    /// <inheritdoc />
    public bool IsSelectionModeActive { get; set; }

    /// <summary>How many items may be selected at once; zero for unlimited.</summary>
    public int SelectionLimit { get; set; }

    /// <inheritdoc />
    public int Count
        => _items.Count + _folderItems.Count;

    /// <inheritdoc />
    public IReadOnlyList<IExplorerItem> Items
        => _resolvedItems ??= [.. _folderItems.Values, .. _items.Values];

    /// <inheritdoc />
    public bool IsSelected(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item is IExplorerFolderItem ? _folderItems : _items).ContainsKey(item.Key);
    }

    /// <summary>Adds an enabled item to the selection, or removes it when it is already selected.</summary>
    public void Toggle(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDisabled)
        {
            return;
        }

        Invalidate();

        if (item is IExplorerFolderItem)
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
    public void Remove(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Invalidate();
        (item is IExplorerFolderItem ? _folderItems : _items).Remove(item.Key);
    }

    /// <summary>Empties the selection.</summary>
    public void Clear()
    {
        Invalidate();
        _items.Clear();
        _folderItems.Clear();
    }

    /// <summary>Replaces the selection with the given items.</summary>
    public void Replace(IEnumerable<IExplorerItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Invalidate();
        _items.Clear();
        _folderItems.Clear();

        foreach (var item in items)
        {
            (item is IExplorerFolderItem ? _folderItems : _items)[item.Key] = item;
        }
    }

    private void Invalidate()
        => _resolvedItems = null;
}
