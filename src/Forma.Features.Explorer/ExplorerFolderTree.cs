namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The folder list indexed for lookup, so browsing does not rescan it for every node it draws.</summary>
public sealed class ExplorerFolderTree
{
    private static readonly List<FolderResourceInfo> NoChildren = [];

    private readonly Dictionary<string, FolderResourceInfo> _bySource = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<FolderResourceInfo>> _childrenByParent = new(StringComparer.Ordinal);

    private readonly List<FolderResourceInfo> _roots = [];

    /// <summary>A tree over no folders at all.</summary>
    public static ExplorerFolderTree Empty { get; } = new(null);

    public ExplorerFolderTree(IReadOnlyList<FolderResourceInfo>? folders)
    {
        if (folders is null)
        {
            return;
        }

        for (var index = 0; index < folders.Count; index++)
        {
            var folder = folders[index];

            // A host can repeat a source; the first folder claiming it wins, matching what the breadcrumb walk would find.
            _bySource.TryAdd(folder.Source, folder);

            if (folder.Parent is not { } parent)
            {
                _roots.Add(folder);
                continue;
            }

            if (!_childrenByParent.TryGetValue(parent, out var siblings))
            {
                siblings = [];
                _childrenByParent[parent] = siblings;
            }

            siblings.Add(folder);
        }
    }

    /// <summary>The folders directly inside the parent, or the top-level folders when none is named.</summary>
    public IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent)
    {
        if (parent is null)
        {
            return _roots;
        }

        return _childrenByParent.TryGetValue(parent, out var children) ? children : NoChildren;
    }

    /// <summary>The folder at the source, or null when the tree has none.</summary>
    public FolderResourceInfo? Find(string? source)
        => source is not null && _bySource.TryGetValue(source, out var folder) ? folder : null;

    /// <summary>Whether anything sits inside the folder.</summary>
    public bool HasChildren(string source)
        => _childrenByParent.ContainsKey(source);

    /// <summary>Every folder holding at least one other, which are the only ones a tree can collapse.</summary>
    public IEnumerable<FolderResourceInfo> Expandable()
    {
        foreach (var folder in _bySource.Values)
        {
            if (_childrenByParent.ContainsKey(folder.Source))
            {
                yield return folder;
            }
        }
    }
}
