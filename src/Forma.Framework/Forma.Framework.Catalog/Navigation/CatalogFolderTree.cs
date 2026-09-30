namespace Forma.Framework.Catalog.Navigation;


/// <summary>The folder items of a catalog, indexed by key and by parent.</summary>
public sealed class CatalogFolderTree
{
    private static readonly List<ICatalogFolderItem> NoChildren = [];

    private readonly Dictionary<string, ICatalogFolderItem> _foldersByKey = [with(StringComparer.Ordinal)];

    private readonly Dictionary<string, List<ICatalogFolderItem>> _childrenByParent = [with(StringComparer.Ordinal)];

    private readonly Dictionary<string, List<ICatalogFolderItem>> _listedChildrenByParent = [with(StringComparer.Ordinal)];

    private readonly List<ICatalogFolderItem> _roots = [];

    private readonly List<ICatalogFolderItem> _listedRoots = [];

    /// <summary>The tree with no folders.</summary>
    public static CatalogFolderTree Empty { get; } = new(null);

    /// <summary>Creates the tree over the folder items among the items.</summary>
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

            // The first folder with a repeated key wins.
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

    /// <summary>The folders directly inside the parent, where a null parent is the top level.</summary>
    public IReadOnlyList<ICatalogFolderItem> GetChildren(string? parent)
    {
        if (parent is null)
        {
            return _roots;
        }

        return _childrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The listed folders directly inside the parent, where a null parent is the top level.</summary>
    public IReadOnlyList<ICatalogFolderItem> GetListedChildren(string? parent)
    {
        if (parent is null)
        {
            return _listedRoots;
        }

        return _listedChildrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The folder with the key.</summary>
    public ICatalogFolderItem? Find(string? key)
        => key is not null && _foldersByKey.TryGetValue(key, out var item) ? item : null;

    /// <summary>The parent folder of the item.</summary>
    public ICatalogFolderItem? GetParent(ICatalogFolderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Find(item.ParentKey);
    }

    /// <summary>Whether the folder with the key has child folders.</summary>
    public bool HasChildren(string key)
        => _childrenByParent.ContainsKey(key);

    /// <summary>The expandable folders, each a listed folder holding at least one listed folder.</summary>
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
