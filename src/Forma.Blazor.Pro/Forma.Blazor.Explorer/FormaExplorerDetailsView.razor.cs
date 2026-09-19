namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Blazor.Components.Layout;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Items.Queries;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Tabular details view for explorer items with sortable column headers, its actions and name columns staying put while the columns the reader has chosen scroll sideways.</summary>
public sealed partial class FormaExplorerDetailsView : FormaComponentBase
{
    /// <summary>The collection of items to display.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Items { get; set; } = [];

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private const string Placeholder = "—";

    private double? _frozenExtent;

    // The actions and name columns, then whichever optional columns are shown.
    private int ColumnCount
        => 2 + (ShowsKind ? 1 : 0) + (ShowsSize ? 1 : 0) + (ShowsModified ? 1 : 0) + (ShowsAdded ? 1 : 0);

    // The wheel is left to the panel, which scrolls the rows; the columns move by the arrows alone.
    private FreezePaneOptions PaneOptions
        => new()
        {
            Navigation = ScrollNavigation.Arrows,
            PageOnWheelGesture = false,
            ScrollBackLabel = L["Scroll Columns Back"],
            ScrollForwardLabel = L["Scroll Columns Forward"],
            FrozenEdgeLabel = L["Resize Name Column"],
        };

    // The frozen region the reader asked for; the name column is what gives way to honor it.
    private string? FrozenStyle
        => _frozenExtent is { } extent
            ? new CssStyleBuilder().Add("--forma-explorer-frozen-extent", CssLength.FromPx(extent)).ToString()
            : null;

    private void HandleFrozenExtentChanged(double extent)
        => _frozenExtent = extent;

    private ExplorerDetailsColumns Columns
        => View.DetailsColumns;

    private bool ShowsKind
        => Columns.HasFlag(ExplorerDetailsColumns.Kind);

    private bool ShowsSize
        => Columns.HasFlag(ExplorerDetailsColumns.Size);

    private bool ShowsModified
        => Columns.HasFlag(ExplorerDetailsColumns.Modified);

    private bool ShowsAdded
        => Columns.HasFlag(ExplorerDetailsColumns.Added);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerDetailsView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private static string DisplayDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", CultureInfo.CurrentCulture) ?? Placeholder;

    private string SortState(ExplorerSortField field)
    {
        if (View.SortField != field)
        {
            return "none";
        }

        return View.SortDirection == ExplorerSortDirection.Ascending ? "ascending" : "descending";
    }
}
