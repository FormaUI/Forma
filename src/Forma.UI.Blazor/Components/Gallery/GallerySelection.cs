namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>The media items a gallery has selected, and whether it is currently in selection mode.</summary>
public sealed class GallerySelection
{
    private readonly HashSet<string> _sources = [];

    private readonly HashSet<string> _folderIds = [];

    /// <summary>Whether the gallery shows selection controls instead of activating previews.</summary>
    public bool IsSelectMode { get; internal set; }

    /// <summary>How many items may be selected at once; zero for unlimited.</summary>
    public int MaxSelection { get; internal set; }

    /// <summary>How many media items and folders are selected together.</summary>
    public int Count
        => _sources.Count + _folderIds.Count;

    /// <summary>Whether any media item is selected.</summary>
    public bool HasItems
        => _sources.Count > 0;

    /// <summary>Whether any folder is selected.</summary>
    public bool HasFolders
        => _folderIds.Count > 0;

    /// <summary>Whether the item is selected.</summary>
    public bool IsSelected(ResourceInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return _sources.Contains(entry.Source);
    }

    /// <summary>Whether the folder is selected.</summary>
    public bool IsFolderSelected(GalleryFolderInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return _folderIds.Contains(folder.Id);
    }

    /// <summary>The selected folders, drawn from the supplied folders.</summary>
    public IReadOnlyList<GalleryFolderInfo> ResolveFolders(IEnumerable<GalleryFolderInfo> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);

        return [.. folders.Where(folder => _folderIds.Contains(folder.Id))];
    }

    /// <summary>The selected items, drawn from the supplied entries.</summary>
    public IReadOnlyList<ResourceInfo> Resolve(IEnumerable<ResourceInfo> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return [.. entries.Where(entry => _sources.Contains(entry.Source))];
    }

    internal void Toggle(ResourceInfo entry)
    {
        // Media and folders are deleted through different contracts, so only one kind is ever selected at a time.
        _folderIds.Clear();

        if (_sources.Remove(entry.Source))
        {
            return;
        }

        if (MaxSelection == 1)
        {
            _sources.Clear();
            _sources.Add(entry.Source);
        }
        else if (MaxSelection <= 0 || _sources.Count < MaxSelection)
        {
            _sources.Add(entry.Source);
        }
    }

    internal void ToggleFolder(GalleryFolderInfo folder)
    {
        _sources.Clear();

        if (!_folderIds.Remove(folder.Id))
        {
            _folderIds.Add(folder.Id);
        }
    }

    internal void Remove(ResourceInfo entry)
        => _sources.Remove(entry.Source);

    internal void Clear()
    {
        _sources.Clear();
        _folderIds.Clear();
    }

    internal void Replace(IEnumerable<ResourceInfo> entries)
    {
        _sources.Clear();
        _folderIds.Clear();

        foreach (var entry in entries)
        {
            _sources.Add(entry.Source);
        }
    }
}
