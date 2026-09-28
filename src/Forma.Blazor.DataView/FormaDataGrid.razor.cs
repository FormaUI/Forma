namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>A view over a data view drawing its items as rows with aligned columns and headers, sorted by a header, edited a row at a time.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataGrid<TItem> : FormaElementBase, IDataViewViewport
    where TItem : notnull
{
    private const string ViewKey = "grid";

    private readonly List<FormaColumnBase<TItem>> _columns = [];
    private readonly Dictionary<FormaColumnBase<TItem>, FormaDataGridColumnHeader<TItem>> _headers = [];
    private readonly RowEditSession<TItem> _editSession = new();

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

    /// <summary>The grid's own id, generated where the host gave none, from which its parts derive theirs.</summary>
    internal string ElementId
        => Id ?? $"grid-{Context.RuntimeId:N}";

    /// <summary>The columns collected in the current render, in the order written.</summary>
    internal IReadOnlyList<FormaColumnBase<TItem>> CollectedColumns
        => _columns;

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

    /// <summary>How many cells a row has, the selection column included.</summary>
    internal int ColumnCount
        => _columns.Count + (HasSelectionColumn ? 1 : 0);

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

        Interaction.RegisterView(ViewKey, this);
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

        return Interaction.SortByAsync(column.SortName, sort, direction);
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
            return Interaction.SortByAsync(column.SortName, sort, column.InitialSortDirection);
        }

        return View.SortDirection == column.InitialSortDirection
            ? Interaction.SortByAsync(column.SortName, sort, column.InitialSortDirection.Reverse())
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

    internal static string? GetCellClass(FormaColumnBase<TItem> column)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-grid-cell");
        builder.Add(GetAlignClass(column));
        builder.Add(column.Class);

        return builder.ToStringOrNull();
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
    internal void FinishCollectingColumns()
        => _isCollectingColumns = false;

    private void StartCollectingColumns()
    {
        _columns.Clear();
        _isCollectingColumns = true;
    }

    // The address names the sort it asked for by the column's name; failing that, the default sort column opens the grid sorted.
    private async Task ApplyInitialSortAsync()
    {
        if (View.RequestedSortName is { } requestedName)
        {
            var requested = _columns.FirstOrDefault(column => column.IsSortable && column.SortBy is not null && string.Equals(column.SortName, requestedName, StringComparison.Ordinal));

            if (requested is { SortBy: { } requestedSort })
            {
                await Interaction.SortByAsync(requested.SortName, requestedSort, View.RequestedSortDirection);
                return;
            }
        }

        if (View.SortName is null && _columns.FirstOrDefault(column => column.IsDefaultSortColumn && column.SortBy is not null) is { SortBy: { } defaultSort } defaultColumn)
        {
            await Interaction.SortByAsync(defaultColumn.SortName, defaultSort, defaultColumn.InitialSortDirection);
        }
    }

    internal async ValueTask<ItemsProviderResult<TItem>> ProvideVirtualizedItemsAsync(ItemsProviderRequest request)
    {
        var window = await View.LoadWindowAsync(request.StartIndex, request.Count, request.CancellationToken);

        // The rows drawn are the last window's, so their places in the set are recorded once per fetch, never per render.
        var rowIndices = new Dictionary<TItem, int>(window.Items.Count, View.ItemComparer);
        for (var offset = 0; offset < window.Items.Count; offset++)
        {
            rowIndices[window.Items[offset]] = request.StartIndex + offset;
        }

        _rowIndices = rowIndices;

        return new ItemsProviderResult<TItem>(window.Items, window.TotalCount ?? window.Items.Count);
    }

    /// <summary>The one-based place of <paramref name="item"/> in the whole set, for a row a virtualizing grid draws; null where the grid does not virtualize.</summary>
    internal int? GetRowIndex(TItem item)
        => _rowIndices.TryGetValue(item, out var index) ? index + 1 : null;

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => Table?.Virtualizer is { } virtualizer
            ? new ValueTask(virtualizer.ScrollToItemAsync(index, cancellationToken))
            : ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RefreshAsync()
        => Table?.Virtualizer is { } virtualizer
            ? new ValueTask(virtualizer.RefreshDataAsync())
            : ValueTask.CompletedTask;

    private Task HandleViewChangedAsync()
        => InvokeAsync(StateHasChanged);

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
    protected override ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);

        return base.DisposeAsyncCore();
    }
}
