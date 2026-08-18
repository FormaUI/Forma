namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

/// <summary>The media resources an explorer has selected, and whether it is currently in selection mode.</summary>
public sealed class ExplorerSelection
{
    // The selection outlives the window a provider-backed explorer holds, so it keeps the resources rather than keys into a list it may no longer have.
    private readonly Dictionary<string, ResourceInfo> _resources = new(StringComparer.Ordinal);

    private readonly Dictionary<string, FolderResourceInfo> _folders = new(StringComparer.Ordinal);

    private IReadOnlyList<ResourceInfo>? _resolvedResources;

    private IReadOnlyList<FolderResourceInfo>? _resolvedFolders;

    /// <summary>Whether the explorer shows selection controls instead of activating previews.</summary>
    public bool IsSelectMode { get; internal set; }

    /// <summary>How many resources may be selected at once; zero for unlimited.</summary>
    public int MaxSelection { get; internal set; }

    /// <summary>How many media resources and folders are selected together.</summary>
    public int Count
        => _resources.Count + _folders.Count;

    /// <summary>Whether any media resource is selected.</summary>
    public bool HasResources
        => _resources.Count > 0;

    /// <summary>Whether any folder is selected.</summary>
    public bool HasFolders
        => _folders.Count > 0;

    /// <summary>Whether the resource is selected.</summary>
    public bool IsSelected(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return _resources.ContainsKey(resource.Source);
    }

    /// <summary>Whether the folder is selected.</summary>
    public bool IsFolderSelected(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return _folders.ContainsKey(folder.Source);
    }

    /// <summary>The selected folders.</summary>
    public IReadOnlyList<FolderResourceInfo> ResolveFolders()
        => _resolvedFolders ??= [.. _folders.Values];

    /// <summary>The selected resources.</summary>
    public IReadOnlyList<ResourceInfo> Resolve()
        => _resolvedResources ??= [.. _resources.Values];

    internal void Toggle(ResourceInfo resource)
    {
        Invalidate();

        // Media and folders are deleted through different contracts, so only one kind is ever selected at a time.
        _folders.Clear();

        if (_resources.Remove(resource.Source))
        {
            return;
        }

        if (MaxSelection == 1)
        {
            _resources.Clear();
            _resources[resource.Source] = resource;
        }
        else if (MaxSelection <= 0 || _resources.Count < MaxSelection)
        {
            _resources[resource.Source] = resource;
        }
    }

    internal void ToggleFolder(FolderResourceInfo folder)
    {
        Invalidate();
        _resources.Clear();

        if (!_folders.Remove(folder.Source))
        {
            _folders[folder.Source] = folder;
        }
    }

    internal void Remove(ResourceInfo resource)
    {
        Invalidate();
        _resources.Remove(resource.Source);
    }

    internal void Clear()
    {
        Invalidate();
        _resources.Clear();
        _folders.Clear();
    }

    internal void Replace(IEnumerable<ResourceInfo> resources)
    {
        Invalidate();
        _resources.Clear();
        _folders.Clear();

        foreach (var resource in resources)
        {
            _resources[resource.Source] = resource;
        }
    }

    private void Invalidate()
    {
        _resolvedResources = null;
        _resolvedFolders = null;
    }
}
