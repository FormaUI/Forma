namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

/// <summary>The state and operations an explorer shares with the components it renders.</summary>
public sealed class ExplorerContext
{
    private readonly Func<ResourceInfo, Task> _preview;
    private readonly Func<Task> _selectionChanged;
    private readonly Func<ExplorerSortField, Task> _sortRequested;
    private readonly Action<ResourceInfo> _dragStarted;

    internal ExplorerContext(
        Func<ResourceInfo, Task> preview,
        Func<Task> selectionChanged,
        Func<ExplorerSortField, Task> sortRequested,
        Action<ResourceInfo> dragStarted)
    {
        _preview = preview;
        _selectionChanged = selectionChanged;
        _sortRequested = sortRequested;
        _dragStarted = dragStarted;
    }

    /// <summary>How the explorer presents itself.</summary>
    public FormaExplorerOptions Options { get; internal set; } = FormaExplorerOptions.Default;

    /// <summary>Which operations the explorer offers on the media and folders it lists.</summary>
    public FormaExplorerFileOptions FileOptions { get; internal set; } = FormaExplorerFileOptions.Default;

    /// <summary>The folder list indexed, so the tree and the breadcrumb do not each rescan it.</summary>
    internal ExplorerFolderTree Folders { get; set; } = ExplorerFolderTree.Empty;

    /// <summary>The selection the explorer is tracking.</summary>
    public ExplorerSelection Selection { get; } = new();

    /// <summary>The operations offered on every media resource, in render order.</summary>
    public IReadOnlyList<ExplorerAction> Actions { get; internal set; } = [];

    /// <summary>The field the explorer is sorted by.</summary>
    public ExplorerSortField SortField { get; internal set; } = ExplorerSortField.Name;

    /// <summary>The direction the explorer is sorted in.</summary>
    public ExplorerSortDirection SortDirection { get; internal set; } = ExplorerSortDirection.Ascending;

    /// <summary>Produces the localized accessible name for selecting or deselecting a resource.</summary>
    public Func<ResourceInfo, string> SelectLabel { get; internal set; } = _ => string.Empty;

    /// <summary>Produces the localized accessible name for activating a resource, reflecting the current mode.</summary>
    public Func<ResourceInfo, string> ActivateLabel { get; internal set; } = _ => string.Empty;

    /// <summary>Produces the localized accessible name for a resource's actions menu.</summary>
    public Func<ResourceInfo, string> ActionsLabel { get; internal set; } = _ => string.Empty;

    /// <summary>Toggles the resource's selection in selection mode, and previews it otherwise.</summary>
    public Task ActivateAsync(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return Selection.IsSelectMode ? ToggleSelectAsync(resource) : _preview(resource);
    }

    /// <summary>Adds the resource to the selection, or removes it when it is already selected.</summary>
    public async Task ToggleSelectAsync(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Selection.Toggle(resource);

        await _selectionChanged();
    }

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    public async Task ToggleFolderSelectAsync(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        Selection.ToggleFolder(folder);

        await _selectionChanged();
    }

    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    public Task SortByAsync(ExplorerSortField field)
        => _sortRequested(field);

    /// <summary>Records the resource as the one being dragged.</summary>
    public void BeginDrag(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        _dragStarted(resource);
    }
}
