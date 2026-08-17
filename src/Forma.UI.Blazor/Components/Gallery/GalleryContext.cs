namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

/// <summary>The state and operations a gallery shares with the components it renders.</summary>
public sealed class GalleryContext
{
    private readonly Func<ResourceInfo, Task> _preview;
    private readonly Func<Task> _selectionChanged;
    private readonly Func<GallerySortField, Task> _sortRequested;
    private readonly Action<ResourceInfo> _dragStarted;

    internal GalleryContext(
        Func<ResourceInfo, Task> preview,
        Func<Task> selectionChanged,
        Func<GallerySortField, Task> sortRequested,
        Action<ResourceInfo> dragStarted)
    {
        _preview = preview;
        _selectionChanged = selectionChanged;
        _sortRequested = sortRequested;
        _dragStarted = dragStarted;
    }

    /// <summary>How the gallery presents itself.</summary>
    public FormaGalleryOptions Options { get; internal set; } = FormaGalleryOptions.Default;

    /// <summary>Which operations the gallery offers on the media and folders it lists.</summary>
    public FormaGalleryFileOptions FileOptions { get; internal set; } = FormaGalleryFileOptions.Default;

    /// <summary>The folder list indexed, so the tree and the breadcrumb do not each rescan it.</summary>
    internal GalleryFolderTree Folders { get; set; } = GalleryFolderTree.Empty;

    /// <summary>The item selection the gallery is tracking.</summary>
    public GallerySelection Selection { get; } = new();

    /// <summary>The operations offered on every media item, in render order.</summary>
    public IReadOnlyList<GalleryAction> ItemActions { get; internal set; } = [];

    /// <summary>The field the gallery is sorted by.</summary>
    public GallerySortField SortField { get; internal set; } = GallerySortField.Name;

    /// <summary>The direction the gallery is sorted in.</summary>
    public GallerySortDirection SortDirection { get; internal set; } = GallerySortDirection.Ascending;

    /// <summary>Produces the localized accessible name for selecting or deselecting an item.</summary>
    public Func<ResourceInfo, string> SelectLabel { get; internal set; } = _ => string.Empty;

    /// <summary>Produces the localized accessible name for activating an item, reflecting the current mode.</summary>
    public Func<ResourceInfo, string> ActivateLabel { get; internal set; } = _ => string.Empty;

    /// <summary>Toggles the item's selection in selection mode, and previews it otherwise.</summary>
    public Task ActivateAsync(ResourceInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return Selection.IsSelectMode ? ToggleSelectAsync(entry) : _preview(entry);
    }

    /// <summary>Adds the item to the selection, or removes it when it is already selected.</summary>
    public async Task ToggleSelectAsync(ResourceInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Selection.Toggle(entry);

        await _selectionChanged();
    }

    /// <summary>Adds the folder to the selection, or removes it when it is already selected.</summary>
    public async Task ToggleFolderSelectAsync(GalleryFolderInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        Selection.ToggleFolder(folder);

        await _selectionChanged();
    }

    /// <summary>Sorts by the field, reversing the direction when it is already the sorted field.</summary>
    public Task SortByAsync(GallerySortField field)
        => _sortRequested(field);

    /// <summary>Records the item as the one being dragged.</summary>
    public void BeginDrag(ResourceInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _dragStarted(entry);
    }
}
