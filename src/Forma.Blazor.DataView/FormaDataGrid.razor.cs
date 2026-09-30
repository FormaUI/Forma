namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.DataView.Interop;
using Forma.Blazor.Components.Layout;
using Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

/// <summary>A view over a data view drawing its items as rows with aligned columns and headers, sorted by a header, edited a row at a time.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataGrid<TItem> : FormaElementBase, IDataViewViewport, IDataViewPositions<TItem>
    where TItem : notnull
{
    private const string ViewKey = "grid";

    private readonly List<FormaColumnBase<TItem>> _columns = [];
    private readonly Dictionary<FormaColumnBase<TItem>, FormaDataGridColumnHeader<TItem>> _headers = [];
    private readonly RowEditSession<TItem> _editSession = new();
    private readonly Dictionary<string, double> _wantedWidths = [with(StringComparer.Ordinal)];
    private readonly HashSet<string> _droppedColumnKeys = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, bool> _columnChoices = [with(StringComparer.Ordinal)];
    private ColumnFit _columnFit = default!;
    private DotNetObjectReference<FormaDataGrid<TItem>>? _selfReference;
    private bool _isColumnFitAttached;
    private double _availableWidth;
    private double? _frozenExtent;

    private bool _isCollectingColumns;
    private bool _isDefaultSortApplied;

    /// <summary>The data view the grid presents.</summary>
    internal IDataView<TItem> DataView
        => View;

    /// <summary>What the grid asks of the data view.</summary>
    internal IDataViewInteraction<TItem> DataViewInteraction
        => Interaction;

    /// <summary>The columns, declared with the grid's own column components in the order they render.</summary>
    [Parameter, EditorRequired] public RenderFragment Columns { get; set; } = default!;

    /// <summary>Whether the header row shows; off for a consumer that names its columns elsewhere.</summary>
    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>Stands the rows on the small target step, for rows that are plain text.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Allows one row at a time to be edited in place by the columns that offer an editor. Requires <see cref="CloneForEdit"/>.</summary>
    [Parameter] public bool EnableInlineEditing { get; set; }

    /// <summary>Produces the detached copy an edit writes to, so canceling discards it rather than restoring the row.</summary>
    [Parameter] public Func<TItem, TItem>? CloneForEdit { get; set; }

    /// <summary>Raised with the edited copy once it passes validation. Persisting it is the host's job.</summary>
    [Parameter] public EventCallback<TItem> OnRowSave { get; set; }

    /// <summary>Raised with the untouched row when an edit is abandoned.</summary>
    [Parameter] public EventCallback<TItem> OnRowCancel { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;


    /// <summary>The row being edited, or null when no edit is in progress.</summary>
    public TItem? EditingItem
        => _editSession.Item;

    /// <summary>The table, once rendered, which the part that draws it holds.</summary>
    public override ElementReference RootElement
    {
        get => Table?.RootElement ?? default;
        protected set
        {
        }
    }

    private FormaDataGridTable<TItem>? Table { get; set; }

    private Dictionary<TItem, int> _rowIndices = [];
    private DataViewRows<TItem> _rows = default!;
    private bool _isRefreshWanted;

    private bool IsVirtualizing
        => View.Virtualize && !View.EnablePagination;

    /// <summary>Whether the grid is the view showing, which it is alone or when the data view's active view names it.</summary>
    internal bool IsActive
        => View.ActiveViewKey == ViewKey;

    /// <summary>The columns collected in the current render, in the order written.</summary>
    internal IReadOnlyList<FormaColumnBase<TItem>> CollectedColumns
        => _columns;

    /// <summary>The columns drawn: the visible ones the width holds, in the order written.</summary>
    internal IEnumerable<FormaColumnBase<TItem>> ShownColumns
        => _columns.Where(IsShown);

    /// <summary>The columns the width may drop, listed in the columns menu: the visible ones below the highest priority that are not frozen.</summary>
    internal IEnumerable<FormaColumnBase<TItem>> DroppableColumns
        => _columns.Where(IsDroppable);

    /// <summary>Whether the header holds the columns menu, which it does once any column can be dropped.</summary>
    internal bool HasColumnsMenu
        => DroppableColumns.Any();

    /// <summary>Whether any column stays at the start while the rest scroll, which freezes the selection column with it.</summary>
    internal bool HasFrozenColumns
        => _columns.Any(column => column.Visible && column.Frozen);

    /// <summary>The attribute a header cell names its column by, which the fit measures it under.</summary>
    internal const string ColumnAttribute = "data-column";

    // The keys the selection column and the menu column measure under, beside the columns' own.
    private const string SelectionColumnKey = "selection";
    private const string MenuColumnKey = "menu";
    private const string MeasuringClass = "forma-data-grid-measuring";

    private FreezePaneOptions PaneOptions
        => new()
        {
            Navigation = ScrollNavigation.Arrows,
            ScrollsCrossAxis = true,
            ScrollBackLabel = L["Scroll Columns Back"],
            ScrollForwardLabel = L["Scroll Columns Forward"],
            FrozenEdgeLabel = L["Resize Frozen Columns"],
        };

    /// <summary>Whether the grid draws the selection column, for the boxes or the markers.</summary>
    internal bool HasSelectionColumn
        => View.EnableCheckedSelection || View.EnableItemSelection;

    /// <summary>The class of the selection column's cells: the box's column, widened for the menu beside the master control, or the collapsed one carrying the markers alone.</summary>
    internal string SelectionColumnClass
        => View.EnableCheckedSelection
            ? View.CheckedSelectionOptions.SelectAllMode == SelectAllMode.FromMenu
                ? "forma-data-grid-select-column forma-data-grid-select-column-menu"
                : "forma-data-grid-select-column"
            : "forma-data-grid-select-column forma-data-grid-select-column-marker";

    /// <summary>How many cells a row has: the columns drawn, with the selection column and the menu column where they are.</summary>
    internal int ColumnCount
        => ShownColumns.Count() + (HasSelectionColumn ? 1 : 0) + (HasColumnsMenu ? 1 : 0);

    /// <summary>Whether <paramref name="column"/> is drawn: visible, and not dropped by the width or the reader.</summary>
    internal bool IsShown(FormaColumnBase<TItem> column)
        => column.Visible && !_droppedColumnKeys.Contains(column.ColumnKey);

    /// <summary>The class the selection column's cells carry, frozen with the frozen columns where there are any.</summary>
    internal string SelectionCellClass
        => HasFrozenColumns ? $"{SelectionColumnClass} {FormaFreezePane.FrozenClass}" : SelectionColumnClass;

    /// <summary>The style a header cell of <paramref name="column"/> carries: its declared width, and its place in the frozen region.</summary>
    internal string? GetHeaderStyle(FormaColumnBase<TItem> column)
    {
        var builder = new CssStyleBuilder();

        if (column.Width is { } width)
        {
            builder.Add("inline-size", width);
        }

        AddFrozenStyle(builder, column);

        return builder.ToStringOrNull();
    }

    /// <summary>The style a cell of <paramref name="column"/> carries: its place in the frozen region.</summary>
    internal string? GetCellStyle(FormaColumnBase<TItem> column)
    {
        var builder = new CssStyleBuilder();
        AddFrozenStyle(builder, column);

        return builder.ToStringOrNull();
    }

    /// <summary>Takes the width the table has and the widths its columns want, and decides again which fit; called from the script whenever either changes.</summary>
    [JSInvokable]
    public Task HandleColumnFitAsync(ColumnFitReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var widthsChanged = false;

        foreach (var column in report.Columns)
        {
            widthsChanged |= !_wantedWidths.TryGetValue(column.Key, out var wanted) || wanted != column.Width;
            _wantedWidths[column.Key] = column.Width;
        }

        // A reader's choice holds until the grid is given a different width.
        if (Math.Abs(report.AvailableWidth - _availableWidth) >= 1)
        {
            _availableWidth = report.AvailableWidth;
            _columnChoices.Clear();
        }

        // The frozen columns are placed by the widths, so a change of width moves them even where the fit stands.
        var frozenColumnsMoved = widthsChanged && HasFrozenColumns;

        return FitColumns() || frozenColumnsMoved ? InvokeAsync(StateHasChanged) : Task.CompletedTask;
    }

    /// <summary>Shows a column the width dropped, or drops one it shows; the reader's choice holds until the grid is given a different width.</summary>
    /// <param name="column">The column chosen in the columns menu.</param>
    public Task ToggleColumnAsync(FormaColumnBase<TItem> column)
    {
        ArgumentNullException.ThrowIfNull(column);

        _columnChoices[column.ColumnKey] = !IsShown(column);
        FitColumns();

        return InvokeAsync(StateHasChanged);
    }

    // The frozen edge the reader drags sets how far the frozen region reaches; the last frozen column takes up the difference.
    private void HandleFrozenExtentChanged(double extent)
        => _frozenExtent = extent;

    private bool IsDroppable(FormaColumnBase<TItem> column)
        => column.Visible && !column.Frozen && column.Priority < TopPriority;

    private int TopPriority
        => _columns.Where(column => column.Visible).Select(column => column.Priority).DefaultIfEmpty(0).Max();

    // A column the width has not measured yet counts as nothing wide, so it is drawn and measured before it can be dropped.
    private double GetWantedWidth(string key)
        => _wantedWidths.GetValueOrDefault(key);

    // Decides which columns fit the width: a column the reader chose is shown or dropped as chosen and counts for nothing, since the pane
    // scrolls sideways for a chosen one; of the rest, the lowest priorities leave, the last written first among equals, until they fit.
    private bool FitColumns()
    {
        var dropped = new HashSet<string>(StringComparer.Ordinal);

        if (_availableWidth > 0)
        {
            var total = GetWantedWidth(SelectionColumnKey) + GetWantedWidth(MenuColumnKey)
                + _columns.Where(column => column.Visible && !_columnChoices.ContainsKey(column.ColumnKey)).Sum(column => GetWantedWidth(column.ColumnKey));

            foreach (var column in DroppableColumns)
            {
                if (_columnChoices.TryGetValue(column.ColumnKey, out var shown) && !shown)
                {
                    dropped.Add(column.ColumnKey);
                }
            }

            var candidates = DroppableColumns
                .Select((column, index) => (column, index))
                .Where(candidate => !_columnChoices.ContainsKey(candidate.column.ColumnKey))
                .OrderBy(candidate => candidate.column.Priority)
                .ThenByDescending(candidate => candidate.index);

            foreach (var (column, _) in candidates)
            {
                if (total <= _availableWidth)
                {
                    break;
                }

                dropped.Add(column.ColumnKey);
                total -= GetWantedWidth(column.ColumnKey);
            }
        }

        if (dropped.SetEquals(_droppedColumnKeys))
        {
            return false;
        }

        _droppedColumnKeys.Clear();
        _droppedColumnKeys.UnionWith(dropped);

        return true;
    }

    // A frozen column sticks at its distance from the start: the widths of the frozen columns before it, the selection column's among them.
    private void AddFrozenStyle(CssStyleBuilder builder, FormaColumnBase<TItem> column)
    {
        if (!column.Frozen || !HasFrozenColumns)
        {
            return;
        }

        var offset = HasSelectionColumn ? GetWantedWidth(SelectionColumnKey) : 0;
        FormaColumnBase<TItem>? lastFrozen = null;

        foreach (var frozen in ShownColumns.Where(shown => shown.Frozen))
        {
            if (frozen == column)
            {
                break;
            }

            offset += GetWantedWidth(frozen.ColumnKey);
        }

        foreach (var frozen in ShownColumns.Where(shown => shown.Frozen))
        {
            lastFrozen = frozen;
        }

        builder.Add("--forma-freeze-pane-frozen-offset", CssLength.FromPx(offset));

        // The reader's extent is honored by the last frozen column, which takes what the columns before it leave.
        if (lastFrozen == column && _frozenExtent is { } extent && extent > offset)
        {
            builder.Add("inline-size", CssLength.FromPx(extent - offset));
        }
    }

    /// <summary>Tells a virtualized row's item from a re-fetched copy of it, by the data view's key.</summary>
    /// <summary>The height a placeholder row holds while its item loads.</summary>
    internal string PlaceholderStyle
        => string.Create(CultureInfo.InvariantCulture, $"block-size: {View.ItemHeight}px");

    /// <summary>The header shows the master checkbox unless a caption with a text stands in for it.</summary>
    internal bool HasMasterCaption
        => View.CheckedSelectionOptions.Caption is { } caption && (caption.SelectAllText is not null || caption.DeselectAllText is not null);

    internal bool MasterCaptionInteractive
        => View.CheckedSelectionOptions.Caption is { SelectAllText: not null, DeselectAllText: not null };

    internal string? MasterCaptionText
        => View.CheckedSelectionOptions.Caption switch
        {
            null => null,
            { SelectAllText: not null, DeselectAllText: not null } caption
                => View.PartiallySelected || View.AllSelected ? caption.DeselectAllText : caption.SelectAllText,
            { SelectAllText: not null } caption => caption.SelectAllText,
            { DeselectAllText: not null } caption => caption.DeselectAllText,
            _ => null,
        };

    // The caption the toggle would show in its other state.
    internal string? MasterCaptionAlternateText
        => MasterCaptionInteractive
            ? View.PartiallySelected || View.AllSelected ? View.CheckedSelectionOptions.Caption!.SelectAllText : View.CheckedSelectionOptions.Caption!.DeselectAllText
            : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataGrid<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["Grid"], Glyph.DetailsView, this));
        _rows = new DataViewRows<TItem>(View, () => Table?.Virtualizer, InvokeAsync, StateHasChanged);
        _columnFit = new ColumnFit(Context.JS);
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Columns);

        if (EnableInlineEditing && CloneForEdit is null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableInlineEditing)} requires {nameof(CloneForEdit)}; an edit needs a copy to write to.");
        }

        // Editors write through to the draft by reference; a value type would be copied on every read, so the edits would be silently dropped rather than reported.
        if (EnableInlineEditing && typeof(TItem).IsValueType)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableInlineEditing)} requires a reference type, but {typeof(TItem)} is a value type.");
        }

        _editSession.KeySelector = View.ItemKey;
        _isRefreshWanted |= IsVirtualizing && _rows.Update() && Table?.Virtualizer is not null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The address's sort and the default sort column are only known once the columns have registered, which the first render does.
        if (!_isDefaultSortApplied)
        {
            _isDefaultSortApplied = true;
            await ApplyInitialSortAsync();
        }

        // Only a grid that can drop or freeze a column needs its widths; a plain grid attaches nothing.
        if (!_isColumnFitAttached && (HasColumnsMenu || HasFrozenColumns) && Table is { } table)
        {
            // Claimed before the await so a render arriving mid-call cannot attach a second observer to the same table.
            _isColumnFitAttached = true;
            _selfReference ??= DotNetObjectReference.Create(this);
            _isColumnFitAttached = await _columnFit.AttachAsync(table.RootElement, ColumnAttribute, MeasuringClass, _selfReference);
        }

        if (_isRefreshWanted && Table?.Virtualizer is { } virtualizer)
        {
            _isRefreshWanted = false;
            // The virtualization fetches the rows without drawing them, leaving the draw to its host.
            await virtualizer.RefreshDataAsync();
            StateHasChanged();
        }
    }

    /// <summary>Whether <paramref name="item"/> is the row being edited.</summary>
    /// <param name="item">The row to judge.</param>
    public bool IsEditing(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _editSession.IsEditing(item);
    }

    /// <summary>Starts editing <paramref name="item"/>, replacing any edit already in progress.</summary>
    /// <param name="item">The row to edit.</param>
    public void BeginEdit(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!EnableInlineEditing)
        {
            throw new InvalidOperationException($"{nameof(BeginEdit)} requires {nameof(EnableInlineEditing)}.");
        }

        _editSession.BeginEditing(item, CloneForEdit!(item));
        StateHasChanged();
    }

    /// <summary>Validates the edit and, if it passes, ends it and reports the edited copy. Returns whether it was accepted.</summary>
    public async Task<bool> SaveEditAsync()
    {
        if (_editSession is not { Draft: { } draft, EditContext: { } editContext })
        {
            return false;
        }

        if (!await editContext.ValidateAsync())
        {
            return false;
        }

        _editSession.EndEditing();
        StateHasChanged();
        await OnRowSave.InvokeAsync(draft);

        return true;
    }

    /// <summary>Abandons the edit, discarding the draft.</summary>
    public async Task CancelEditAsync()
    {
        if (_editSession.Item is not { } item)
        {
            return;
        }

        _editSession.EndEditing();
        StateHasChanged();
        await OnRowCancel.InvokeAsync(item);
    }

    /// <summary>Sorts by <paramref name="column"/> in <paramref name="direction"/>, as pressing its header does.</summary>
    /// <param name="column">The column to sort by, which must be sortable.</param>
    /// <param name="direction">The direction to sort in.</param>
    public Task SortByColumnAsync(FormaColumnBase<TItem> column, DataViewSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(column);

        if (!column.IsSortable || column.SortBy is not { } sort)
        {
            throw new InvalidOperationException($"The column '{column.Title}' is not sortable.");
        }

        return Interaction.SortByAsync(column.SortName!, sort, direction);
    }

    /// <summary>Opens <paramref name="column"/>'s options surface.</summary>
    /// <param name="column">The column whose options open.</param>
    public Task ShowColumnOptionsAsync(FormaColumnBase<TItem> column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return _headers.TryGetValue(column, out var header) ? header.ShowOptionsAsync() : Task.CompletedTask;
    }

    /// <summary>Closes whichever column's options surface is open.</summary>
    public async Task HideColumnOptionsAsync()
    {
        foreach (var header in _headers.Values)
        {
            await header.HideOptionsAsync();
        }
    }

    /// <summary>Whether the data view is sorted by <paramref name="column"/>.</summary>
    /// <param name="column">The column to judge.</param>
    public bool IsSortedBy(FormaColumnBase<TItem> column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return column.IsSortable && View.SortName is { } name && string.Equals(name, column.SortName, StringComparison.Ordinal);
    }

    /// <summary>Registers a column for the current render, in the order the columns are written.</summary>
    /// <param name="column">The column registering.</param>
    internal void AddColumn(FormaColumnBase<TItem> column)
    {
        if (_isCollectingColumns)
        {
            _columns.Add(column);
        }
        else
        {
            // A column whose parameters changed after the pass re-renders the table so its header and cells follow.
            StateHasChanged();
        }
    }

    internal void RegisterHeader(FormaColumnBase<TItem> column, FormaDataGridColumnHeader<TItem> header)
        => _headers[column] = header;

    internal void UnregisterHeader(FormaColumnBase<TItem> column, FormaDataGridColumnHeader<TItem> header)
    {
        if (_headers.TryGetValue(column, out var registered) && ReferenceEquals(registered, header))
        {
            _headers.Remove(column);
        }
    }

    internal int GetColumnIndex(FormaColumnBase<TItem> column)
        => _columns.IndexOf(column);

    // Cycles a header press: ascending, then descending, then the source's own order.
    internal Task SortByColumnCoreAsync(FormaColumnBase<TItem> column)
    {
        if (column.SortBy is not { } sort)
        {
            return Task.CompletedTask;
        }

        if (!IsSortedBy(column))
        {
            return Interaction.SortByAsync(column.SortName!, sort, column.InitialSortDirection);
        }

        return View.SortDirection == column.InitialSortDirection
            ? Interaction.SortByAsync(column.SortName!, sort, column.InitialSortDirection.Reverse())
            : Interaction.ClearSortAsync();
    }

    // Selection state is layered on top of whatever class the host already assigns, so a custom class and the built-in selected and disabled styling never conflict.
    internal string? GetRowClass(TItem item)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-grid-row");
        builder.Add(View.ItemClass?.Invoke(item));

        if (HasSelectionColumn)
        {
            builder.Add("forma-data-grid-row-selected", View.IsSelected(item));
            builder.Add("forma-data-grid-row-disabled", !View.CanInteractWith(item));
        }

        builder.Add("forma-data-grid-row-editing", _editSession.IsEditing(item));

        return builder.ToStringOrNull();
    }

    internal string? GetCellClass(FormaColumnBase<TItem> column)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-grid-cell");
        builder.Add(GetAlignClass(column));
        AddFrozenClass(builder, column);
        builder.Add(column.Class);

        return builder.ToStringOrNull();
    }

    // With a frozen region, a frozen cell sticks and every other cell is a stop the pane's arrows step between.
    private void AddFrozenClass(CssClassBuilder builder, FormaColumnBase<TItem> column)
    {
        if (!HasFrozenColumns)
        {
            return;
        }

        builder.Add(column.Frozen ? FormaFreezePane.FrozenClass : FormaFreezePane.StopClass);
    }

    private static string? GetAlignClass(FormaColumnBase<TItem> column)
        => column.Align switch
        {
            ColumnAlign.Center => "forma-data-grid-align-center",
            ColumnAlign.End => "forma-data-grid-align-end",
            _ => null,
        };

    internal string? GetHeaderClass(FormaColumnBase<TItem> column)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-grid-header-cell");
        builder.Add(GetAlignClass(column));
        builder.Add("forma-data-grid-header-cell-sortable", column.IsSortable && column.SortBy is not null);
        builder.Add("forma-data-grid-header-cell-sorted", IsSortedBy(column));
        AddFrozenClass(builder, column);
        builder.Add(column.Class);

        return builder.ToStringOrNull();
    }

    internal string? GetAriaSort(FormaColumnBase<TItem> column)
    {
        if (!column.IsSortable || column.SortBy is null)
        {
            return null;
        }

        if (!IsSortedBy(column))
        {
            return "none";
        }

        return View.SortDirection == DataViewSortDirection.Ascending ? "ascending" : "descending";
    }

    // The table part calls this as it starts rendering, which is after every column declared before it has registered.
    // Two sortable columns naming one sort would leave the address unable to tell them apart, so the grid refuses them.
    internal void FinishCollectingColumns()
    {
        _isCollectingColumns = false;

        if (_columns.Where(column => column.IsSortable).GroupBy(column => column.SortName, StringComparer.Ordinal).FirstOrDefault(named => named.Count() > 1) is { } repeated)
        {
            throw new InvalidOperationException($"Two sortable columns of one {nameof(FormaDataGrid<>)} name the sort '{repeated.Key}'; give one a {nameof(FormaColumnBase<>.SortName)} of its own.");
        }
    }

    private void StartCollectingColumns()
    {
        _columns.Clear();
        _isCollectingColumns = true;
    }

    // The address names the sort it asked for by the column's name; a name no column carries is left for a sort menu beside the grid to apply. Where the address asked for nothing, the default sort column opens the grid sorted.
    private async Task ApplyInitialSortAsync()
    {
        if (View.RequestedSortName is { } requestedName)
        {
            var requested = _columns.FirstOrDefault(column => column.IsSortable && column.SortBy is not null && string.Equals(column.SortName, requestedName, StringComparison.Ordinal));

            if (requested is { SortBy: { } requestedSort })
            {
                await Interaction.SortByAsync(requested.SortName!, requestedSort, View.RequestedSortDirection);
                return;
            }
        }

        if (View.SortName is null && View.RequestedSortName is null && _columns.FirstOrDefault(column => column.IsDefaultSortColumn && column.SortBy is not null) is { SortBy: { } defaultSort } defaultColumn)
        {
            await Interaction.SortByAsync(defaultColumn.SortName!, defaultSort, defaultColumn.InitialSortDirection);
        }
    }

    /// <summary>Tells a virtualized row from a re-fetched copy of it, by its heading's group or its item's key.</summary>
    internal IEqualityComparer<DataViewRow<TItem>> RowComparer
        => _rows.Comparer;

    /// <summary>The row the grid opens on: the one holding the data view's initial item.</summary>
    internal int InitialRowIndex
        => _rows.GetRowIndex(View.InitialItemIndex);

    internal async ValueTask<ItemsProviderResult<DataViewRow<TItem>>> ProvideVirtualizedRowsAsync(ItemsProviderRequest request)
    {
        var result = await _rows.ProvideAsync(request);

        // The rows drawn are the last slice's, so their places in the set are recorded once per fetch, never per render.
        var rowIndices = new Dictionary<TItem, int>(View.ItemComparer);

        foreach (var row in result.Items)
        {
            for (var offset = 0; offset < row.Items.Count; offset++)
            {
                rowIndices[row.Items[offset]] = row.FirstItemIndex + offset;
            }
        }

        _rowIndices = rowIndices;

        return result;
    }

    /// <summary>The one-based place of <paramref name="item"/> in the whole set, for a row a virtualizing grid draws; null where the grid does not virtualize.</summary>
    internal int? GetRowIndex(TItem item)
        => _rowIndices.TryGetValue(item, out var index) ? index + 1 : null;

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => _rows.ScrollToAsync(index, cancellationToken);

    /// <inheritdoc />
    public ValueTask RefreshAsync()
    {
        _rows.Reset();
        _rows.Update();

        return Table?.Virtualizer is { } virtualizer
            ? new ValueTask(virtualizer.RefreshDataAsync())
            : ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    int? IDataViewPositions<TItem>.GetEdgePosition(bool last)
        => _rows.GetEdgePosition(last);

    /// <inheritdoc />
    ValueTask<IReadOnlyList<TItem>> IDataViewPositions<TItem>.ReadAsync(int from, int to, CancellationToken cancellationToken)
        => _rows.ReadAsync(from, to, cancellationToken);

    /// <inheritdoc />
    ValueTask IDataViewPositions<TItem>.ScrollToPositionAsync(int position, CancellationToken cancellationToken)
        => ScrollToItemAsync(position, cancellationToken);

    // A fold, a new count or a new grouping lays the virtualized rows out again, and the rows on screen are fetched again after the render.
    private Task HandleViewChangedAsync()
    {
        _isRefreshWanted |= IsVirtualizing && _rows.Update() && Table?.Virtualizer is not null;

        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-grid")
            .Add("forma-data-grid-dense", Dense)
            .Add("forma-data-grid-headerless", !ShowHeader);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);
        await _columnFit.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
