namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>The media items a gallery has selected, and whether it is currently in selection mode.</summary>
public sealed class GallerySelection
{
    // The selection outlives the window a catalog-backed gallery holds, so it keeps the resources rather than keys into a list it may no longer have.
    private readonly Dictionary<string, ResourceInfo> _resources = new(StringComparer.Ordinal);

    private readonly Dictionary<string, GalleryFolderInfo> _folders = new(StringComparer.Ordinal);

    /// <summary>Whether the gallery shows selection controls instead of activating previews.</summary>
    public bool IsSelectMode { get; internal set; }

    /// <summary>How many items may be selected at once; zero for unlimited.</summary>
    public int MaxSelection { get; internal set; }

    /// <summary>How many media items and folders are selected together.</summary>
    public int Count
        => _resources.Count + _folders.Count;

    /// <summary>Whether any media item is selected.</summary>
    public bool HasItems
        => _resources.Count > 0;

    /// <summary>Whether any folder is selected.</summary>
    public bool HasFolders
        => _folders.Count > 0;

    /// <summary>Whether the item is selected.</summary>
    public bool IsSelected(ResourceInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return _resources.ContainsKey(entry.Source);
    }

    /// <summary>Whether the folder is selected.</summary>
    public bool IsFolderSelected(GalleryFolderInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return _folders.ContainsKey(folder.Id);
    }

    /// <summary>The selected folders.</summary>
    public IReadOnlyList<GalleryFolderInfo> ResolveFolders()
        => [.. _folders.Values];

    /// <summary>The selected items.</summary>
    public IReadOnlyList<ResourceInfo> Resolve()
        => [.. _resources.Values];

    internal void Toggle(ResourceInfo entry)
    {
        // Media and folders are deleted through different contracts, so only one kind is ever selected at a time.
        _folders.Clear();

        if (_resources.Remove(entry.Source))
        {
            return;
        }

        if (MaxSelection == 1)
        {
            _resources.Clear();
            _resources[entry.Source] = entry;
        }
        else if (MaxSelection <= 0 || _resources.Count < MaxSelection)
        {
            _resources[entry.Source] = entry;
        }
    }

    internal void ToggleFolder(GalleryFolderInfo folder)
    {
        _resources.Clear();

        if (!_folders.Remove(folder.Id))
        {
            _folders[folder.Id] = folder;
        }
    }

    internal void Remove(ResourceInfo entry)
        => _resources.Remove(entry.Source);

    internal void Clear()
    {
        _resources.Clear();
        _folders.Clear();
    }

    internal void Replace(IEnumerable<ResourceInfo> entries)
    {
        _resources.Clear();
        _folders.Clear();

        foreach (var entry in entries)
        {
            _resources[entry.Source] = entry;
        }
    }
}
