namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Buttons;
using Forma.Features.Explorer;
using Forma.Framework.Icons;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Action toolbar for media explorer operations, view switching, and sorting.</summary>
public sealed partial class FormaExplorerToolbar : ComponentBase
{
    /// <summary>Whether the explorer is in selection mode.</summary>
    [Parameter] public bool IsSelectMode { get; set; }

    /// <summary>Whether selection mode can be turned off, which a caller that opened purely to choose does not offer.</summary>
    [Parameter] public bool CanLeaveSelectMode { get; set; } = true;

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
    [Parameter] public EventCallback<ExplorerSortDirection> SortDirectionChanged { get; set; }

    /// <summary>Raised when the sort field changes.</summary>
    [Parameter] public EventCallback<ExplorerSortField> SortFieldChanged { get; set; }

    /// <summary>Raised when the upload panel is toggled.</summary>
    [Parameter] public EventCallback OnToggleUpload { get; set; }

    /// <summary>Raised when the active view mode changes.</summary>
    [Parameter] public EventCallback<ExplorerViewMode> ViewModeChanged { get; set; }

    /// <summary>Which operations the toolbar offers.</summary>
    [Parameter] public ExplorerFileOptions FileOptions { get; set; } = ExplorerFileOptions.Default;

    /// <summary>Number of currently selected resources.</summary>
    [Parameter] public int SelectedCount { get; set; }

    /// <summary>Current sort direction.</summary>
    [Parameter] public ExplorerSortDirection SortDirection { get; set; } = ExplorerSortDirection.Ascending;

    /// <summary>Current sort field.</summary>
    [Parameter] public ExplorerSortField SortField { get; set; } = ExplorerSortField.Name;

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public ExplorerViewMode ViewMode { get; set; } = ExplorerViewMode.Thumbnails;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private static readonly ExplorerSortField[] SortFields = Enum.GetValues<ExplorerSortField>();

    private static readonly ExplorerSortDirection[] SortDirections = Enum.GetValues<ExplorerSortDirection>();

    private static readonly ButtonOptions SortTriggerOptions =
        new() { Variant = ButtonVariant.Ghost };

    private string SelectionAnnouncement
        => SelectedCount > 0 ? L["{SelectedCount} selected", SelectedCount].Value : string.Empty;

    private Glyph DirectionGlyph
        => GlyphFor(SortField, SortDirection);

    // Names and kinds sort alphabetically, sizes and dates numerically, so the mark says which order is meant.
    private static Glyph GlyphFor(ExplorerSortField field, ExplorerSortDirection direction)
        => (field is ExplorerSortField.Name or ExplorerSortField.Kind, direction is ExplorerSortDirection.Ascending) switch
        {
            (true, true) => Glyph.SortAlphaAscending,
            (true, false) => Glyph.SortAlphaDescending,
            (false, true) => Glyph.SortNumericAscending,
            (false, false) => Glyph.SortNumericDescending,
        };

    private string SortFieldLabel(ExplorerSortField field)
        => field switch
        {
            ExplorerSortField.Name => L["Name"].Value,
            ExplorerSortField.Size => L["Size"].Value,
            ExplorerSortField.Kind => L["Type"].Value,
            ExplorerSortField.DateAdded => L["Date Added"].Value,
            ExplorerSortField.DateModified => L["Date Modified"].Value,
            _ => field.ToString(),
        };

    private string DirectionLabel(ExplorerSortDirection direction)
        => direction == ExplorerSortDirection.Ascending ? L["Ascending"].Value : L["Descending"].Value;
}
