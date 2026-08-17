namespace Forma.UI.Components.Gallery;

/// <summary>The folder list indexed for lookup, so browsing does not rescan it for every node it draws.</summary>
internal sealed class GalleryFolderTree
{
    private static readonly List<GalleryFolderInfo> NoChildren = [];

    private readonly Dictionary<string, GalleryFolderInfo> _byId = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<GalleryFolderInfo>> _childrenByParent = new(StringComparer.Ordinal);

    private readonly List<GalleryFolderInfo> _roots = [];

    /// <summary>A tree over no folders at all.</summary>
    public static GalleryFolderTree Empty { get; } = new(null);

    public GalleryFolderTree(IReadOnlyList<GalleryFolderInfo>? folders)
    {
        Source = folders;

        if (folders is null)
        {
            return;
        }

        for (var index = 0; index < folders.Count; index++)
        {
            var folder = folders[index];

            // A host can repeat an id; the first folder claiming it wins, matching what the breadcrumb walk would find.
            _byId.TryAdd(folder.Id, folder);

            if (folder.ParentId is not { } parentId)
            {
                _roots.Add(folder);
                continue;
            }

            if (!_childrenByParent.TryGetValue(parentId, out var siblings))
            {
                siblings = [];
                _childrenByParent[parentId] = siblings;
            }

            siblings.Add(folder);
        }
    }

    /// <summary>The folder list this was built from, so a caller can tell whether it is still current.</summary>
    public IReadOnlyList<GalleryFolderInfo>? Source { get; }

    /// <summary>The folders directly inside the parent, or the top-level folders when none is named.</summary>
    public IReadOnlyList<GalleryFolderInfo> ChildrenOf(string? parentId)
    {
        if (parentId is null)
        {
            return _roots;
        }

        return _childrenByParent.TryGetValue(parentId, out var children) ? children : NoChildren;
    }

    /// <summary>The folder with the id, or null when the tree has none.</summary>
    public GalleryFolderInfo? Find(string? id)
        => id is not null && _byId.TryGetValue(id, out var folder) ? folder : null;

    /// <summary>Whether anything sits inside the folder.</summary>
    public bool HasChildren(string id)
        => _childrenByParent.ContainsKey(id);

    /// <summary>Every folder holding at least one other, which are the only ones a tree can collapse.</summary>
    public IEnumerable<GalleryFolderInfo> Expandable()
    {
        foreach (var folder in _byId.Values)
        {
            if (_childrenByParent.ContainsKey(folder.Id))
            {
                yield return folder;
            }
        }
    }
}
