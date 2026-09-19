namespace Forma.Framework.Catalog.Navigation;


/// <summary>The folder items indexed for lookup, so browsing does not rescan them for every node it draws.</summary>
public sealed class CatalogFolderTree
{
    private static readonly List<ICatalogFolderItem> NoChildren = [];

    private readonly Dictionary<string, ICatalogFolderItem> _foldersByKey = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<ICatalogFolderItem>> _childrenByParent = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<ICatalogFolderItem>> _listedChildrenByParent = new(StringComparer.Ordinal);

    private readonly List<ICatalogFolderItem> _roots = [];

    private readonly List<ICatalogFolderItem> _listedRoots = [];

    /// <summary>A tree over no folders at all.</summary>
    public static CatalogFolderTree Empty { get; } = new(null);

    /// <summary>Indexes the folders among the given items, or nothing when they are null.</summary>
    public CatalogFolderTree(IReadOnlyList<ICatalogItem>? items)
    {
        if (items is null)
        {
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item is not ICatalogFolderItem folderItem)
            {
                continue;
            }

            // A host can repeat a key; the first folder claiming it wins, matching what the breadcrumb walk would find.
            _foldersByKey.TryAdd(folderItem.Key, folderItem);

            var isListed = folderItem.Visibility != CatalogItemVisibility.Hidden;

            if (folderItem.ParentKey is not { } parent)
            {
                _roots.Add(folderItem);

                if (isListed)
                {
                    _listedRoots.Add(folderItem);
                }

                continue;
            }

            Add(_childrenByParent, parent, folderItem);

            if (isListed)
            {
                Add(_listedChildrenByParent, parent, folderItem);
            }
        }
    }

    /// <summary>The folders directly inside the parent, or the top-level folders when none is named.</summary>
    public IReadOnlyList<ICatalogFolderItem> GetChildren(string? parent)
    {
        if (parent is null)
        {
            return _roots;
        }

        return _childrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The folders directly inside the parent that are not hidden, or the top-level ones when none is named.</summary>
    public IReadOnlyList<ICatalogFolderItem> GetListedChildren(string? parent)
    {
        if (parent is null)
        {
            return _listedRoots;
        }

        return _listedChildrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The folder at the key, or null when the tree has none.</summary>
    public ICatalogFolderItem? Find(string? key)
        => key is not null && _foldersByKey.TryGetValue(key, out var item) ? item : null;

    /// <summary>The folder the item sits inside, or null when it is top-level or the tree has none.</summary>
    public ICatalogFolderItem? GetParent(ICatalogFolderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Find(item.ParentKey);
    }

    /// <summary>Whether anything sits inside the folder.</summary>
    public bool HasChildren(string key)
        => _childrenByParent.ContainsKey(key);

    /// <summary>Every listed folder holding at least one listed folder, which are the only ones a tree can collapse.</summary>
    public IEnumerable<ICatalogFolderItem> GetExpandableFolders()
    {
        foreach (var (key, item) in _foldersByKey)
        {
            if (item.Visibility != CatalogItemVisibility.Hidden && _listedChildrenByParent.ContainsKey(key))
            {
                yield return item;
            }
        }
    }

    private static void Add(Dictionary<string, List<ICatalogFolderItem>> childrenByParent, string parent, ICatalogFolderItem child)
    {
        if (!childrenByParent.TryGetValue(parent, out var siblings))
        {
            siblings = [];
            childrenByParent[parent] = siblings;
        }

        siblings.Add(child);
    }
}
