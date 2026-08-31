namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The media resources an explorer has selected, and the rules deciding what a further choice does to them.</summary>
public sealed class ExplorerSelection : IExplorerSelectionView
{
    // The selection outlives the window a provider-backed explorer holds, so it keeps the resources rather than keys into a list it may no longer have.
    private readonly Dictionary<string, ResourceInfo> _resources = new(StringComparer.Ordinal);

    private readonly Dictionary<string, FolderResourceInfo> _folders = new(StringComparer.Ordinal);

    private IReadOnlyList<ResourceInfo>? _resolvedResources;

    private IReadOnlyList<FolderResourceInfo>? _resolvedFolders;

    /// <inheritdoc />
    public bool IsSelectMode { get; set; }

    /// <summary>How many resources may be selected at once; zero for unlimited.</summary>
    public int MaxSelection { get; set; }

    /// <inheritdoc />
    public int Count
        => _resources.Count + _folders.Count;

    /// <inheritdoc />
    public bool HasResources
        => _resources.Count > 0;

    /// <inheritdoc />
    public bool HasFolders
        => _folders.Count > 0;

    /// <inheritdoc />
    public bool IsSelected(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return _resources.ContainsKey(resource.Source);
    }

    /// <inheritdoc />
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

    public void Toggle(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

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

    public void ToggleFolder(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        Invalidate();
        _resources.Clear();

        if (!_folders.Remove(folder.Source))
        {
            _folders[folder.Source] = folder;
        }
    }

    public void Remove(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Invalidate();
        _resources.Remove(resource.Source);
    }

    public void Clear()
    {
        Invalidate();
        _resources.Clear();
        _folders.Clear();
    }

    public void Replace(IEnumerable<ResourceInfo> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

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
