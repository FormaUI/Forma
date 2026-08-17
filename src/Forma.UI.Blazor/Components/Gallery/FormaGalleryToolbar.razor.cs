namespace Forma.UI.Components.Gallery;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Action toolbar for media gallery operations, view switching, and sorting.</summary>
public sealed partial class FormaGalleryToolbar : ComponentBase
{
    /// <summary>Whether the gallery is in selection mode.</summary>
    [Parameter] public bool IsSelectMode { get; set; }

    /// <summary>Raised when selection mode is toggled.</summary>
    [Parameter] public EventCallback OnToggleSelectMode { get; set; }

    /// <summary>Raised to clear the current selection.</summary>
    [Parameter] public EventCallback OnClearSelection { get; set; }

    /// <summary>Raised when the delete selected action is clicked.</summary>
    [Parameter] public EventCallback OnDelete { get; set; }

    /// <summary>Raised when the new folder action is clicked.</summary>
    [Parameter] public EventCallback OnNewFolder { get; set; }

    /// <summary>Whether the toolbar offers the new folder action.</summary>
    [Parameter] public bool ShowNewFolder { get; set; } = true;

    /// <summary>Raised when the sort direction changes.</summary>
    [Parameter] public EventCallback<GallerySortDirection> OnSortDirectionChanged { get; set; }

    /// <summary>Raised when the sort field changes.</summary>
    [Parameter] public EventCallback<GallerySortField> OnSortFieldChanged { get; set; }

    /// <summary>Raised when the upload panel is toggled.</summary>
    [Parameter] public EventCallback OnToggleUpload { get; set; }

    /// <summary>Raised when the active view mode changes.</summary>
    [Parameter] public EventCallback<GalleryViewMode> OnViewModeChanged { get; set; }

    /// <summary>Which operations the toolbar offers.</summary>
    [Parameter] public FormaGalleryFileOptions FileOptions { get; set; } = FormaGalleryFileOptions.Default;

    /// <summary>Number of currently selected resources.</summary>
    [Parameter] public int SelectedCount { get; set; }

    /// <summary>Current sort direction.</summary>
    [Parameter] public GallerySortDirection SortDirection { get; set; } = GallerySortDirection.Ascending;

    /// <summary>Current sort field.</summary>
    [Parameter] public GallerySortField SortField { get; set; } = GallerySortField.Name;

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public GalleryViewMode ViewMode { get; set; } = GalleryViewMode.Thumbnails;

    [Inject] private IStringLocale<FormaGallery> L { get; set; } = default!;

    private string SelectionAnnouncement
        => SelectedCount > 0 ? L[$"{SelectedCount} selected"].Value : string.Empty;

    private async Task HandleSortFieldChange(ChangeEventArgs e)
    {
        if (Enum.TryParse<GallerySortField>(e.Value?.ToString(), out var field))
        {
            await OnSortFieldChanged.InvokeAsync(field);
        }
    }

    private async Task ToggleSortDirection()
    {
        var next = SortDirection == GallerySortDirection.Ascending
            ? GallerySortDirection.Descending
            : GallerySortDirection.Ascending;

        await OnSortDirectionChanged.InvokeAsync(next);
    }
}
