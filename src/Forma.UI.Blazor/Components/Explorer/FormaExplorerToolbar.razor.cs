namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Action toolbar for media explorer operations, view switching, and sorting.</summary>
public sealed partial class FormaExplorerToolbar : ComponentBase
{
    /// <summary>Whether the explorer is in selection mode.</summary>
    [Parameter] public bool IsSelectMode { get; set; }

    /// <summary>Raised when selection mode is toggled.</summary>
    [Parameter] public EventCallback OnToggleSelectMode { get; set; }

    /// <summary>Raised to clear the current selection.</summary>
    [Parameter] public EventCallback OnClearSelection { get; set; }

    /// <summary>Raised when the delete selected action is clicked.</summary>
    [Parameter] public EventCallback OnDelete { get; set; }

    /// <summary>Raised when the new folder action is clicked.</summary>
    [Parameter] public EventCallback OnFolderCreate { get; set; }

    /// <summary>Whether the toolbar offers the new folder action.</summary>
    [Parameter] public bool ShowNewFolder { get; set; } = true;

    /// <summary>Raised when the sort direction changes.</summary>
    [Parameter] public EventCallback<ExplorerSortDirection> OnSortDirectionChanged { get; set; }

    /// <summary>Raised when the sort field changes.</summary>
    [Parameter] public EventCallback<ExplorerSortField> OnSortFieldChanged { get; set; }

    /// <summary>Raised when the upload panel is toggled.</summary>
    [Parameter] public EventCallback OnToggleUpload { get; set; }

    /// <summary>Raised when the active view mode changes.</summary>
    [Parameter] public EventCallback<ExplorerViewMode> OnViewModeChanged { get; set; }

    /// <summary>Which operations the toolbar offers.</summary>
    [Parameter] public FormaExplorerFileOptions FileOptions { get; set; } = FormaExplorerFileOptions.Default;

    /// <summary>Number of currently selected resources.</summary>
    [Parameter] public int SelectedCount { get; set; }

    /// <summary>Current sort direction.</summary>
    [Parameter] public ExplorerSortDirection SortDirection { get; set; } = ExplorerSortDirection.Ascending;

    /// <summary>Current sort field.</summary>
    [Parameter] public ExplorerSortField SortField { get; set; } = ExplorerSortField.Name;

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public ExplorerViewMode ViewMode { get; set; } = ExplorerViewMode.Thumbnails;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private string SelectionAnnouncement
        => SelectedCount > 0 ? L[$"{SelectedCount} selected"].Value : string.Empty;

    private async Task HandleSortFieldChange(ChangeEventArgs e)
    {
        if (Enum.TryParse<ExplorerSortField>(e.Value?.ToString(), out var field))
        {
            await OnSortFieldChanged.InvokeAsync(field);
        }
    }

    private async Task ToggleSortDirection()
    {
        var next = SortDirection == ExplorerSortDirection.Ascending
            ? ExplorerSortDirection.Descending
            : ExplorerSortDirection.Ascending;

        await OnSortDirectionChanged.InvokeAsync(next);
    }
}
