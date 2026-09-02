namespace Forma.Features.Explorer;

/// <summary>The folder items indexed for lookup, so browsing does not rescan them for every node it draws.</summary>
public sealed class ExplorerFolderTree
{
    private static readonly List<IExplorerFolderItem> NoChildren = [];

    private readonly Dictionary<string, IExplorerFolderItem> _foldersByKey = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<IExplorerFolderItem>> _childrenByParent = new(StringComparer.Ordinal);

    private readonly List<IExplorerFolderItem> _roots = [];

    /// <summary>A tree over no folders at all.</summary>
    public static ExplorerFolderTree Empty { get; } = new(null);

    public ExplorerFolderTree(IReadOnlyList<IExplorerItem>? items)
    {
        if (items is null)
        {
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item is not IExplorerFolderItem folderItem)
            {
                continue;
            }

            // A host can repeat a key; the first folder claiming it wins, matching what the breadcrumb walk would find.
            _foldersByKey.TryAdd(folderItem.Key, folderItem);

            if (folderItem.ParentKey is not { } parent)
            {
                _roots.Add(folderItem);
                continue;
            }

            if (!_childrenByParent.TryGetValue(parent, out var siblings))
            {
                siblings = [];
                _childrenByParent[parent] = siblings;
            }

            siblings.Add(folderItem);
        }
    }

    /// <summary>The folders directly inside the parent, or the top-level folders when none is named.</summary>
    public IReadOnlyList<IExplorerFolderItem> ChildrenOf(string? parent)
    {
        if (parent is null)
        {
            return _roots;
        }

        return _childrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The folder at the key, or null when the tree has none.</summary>
    public IExplorerFolderItem? Find(string? key)
        => key is not null && _foldersByKey.TryGetValue(key, out var item) ? item : null;

    /// <summary>The folder the item sits inside, or null when it is top-level or the tree has none.</summary>
    public IExplorerFolderItem? ParentOf(IExplorerFolderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Find(item.ParentKey);
    }

    /// <summary>Whether anything sits inside the folder.</summary>
    public bool HasChildren(string key)
        => _childrenByParent.ContainsKey(key);

    /// <summary>Every folder holding at least one other, which are the only ones a tree can collapse.</summary>
    public IEnumerable<IExplorerFolderItem> Expandable()
    {
        foreach (var (key, item) in _foldersByKey)
        {
            if (_childrenByParent.ContainsKey(key))
            {
                yield return item;
            }
        }
    }
}
