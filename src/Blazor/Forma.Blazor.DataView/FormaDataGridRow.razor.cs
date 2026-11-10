namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>One row of a grid: the item's selection control where selection is on, then a cell per column.</summary>
/// <typeparam name="TItem">The item the row shows.</typeparam>
/// <remarks>The row draws again only when its item, its place, what the grid draws every row by or its own state changed; a row being edited draws with every render.</remarks>
public sealed partial class FormaDataGridRow<TItem> : FormaComponentBase
    where TItem : notnull
{
    private DataViewItemGate _gate;

    /// <summary>The item the row shows.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; } = default!;

    /// <summary>The item's place in the whole set, where a virtualizing grid draws the row.</summary>
    [Parameter] public int? Position { get; set; }

    /// <summary>The token the selection behavior names the item by, which the grid asks for each time it draws.</summary>
    [Parameter] public int Token { get; set; }

    /// <summary>What the grid draws every row by.</summary>
    [Parameter] public int ViewRevision { get; set; }

    [CascadingParameter] private FormaDataGrid<TItem> Grid { get; set; } = default!;

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field a row's box names to the input base as its value expression.
    private bool RowSelection { get; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataGrid<TItem>);

    private string? RowClass
        => Grid.GetRowClass(Item);

    // A row that cannot be selected says nothing of it, so it is never announced as an unselected choice.
    private string? AriaSelected
        => (View.EnableCheckedSelection || View.EnableItemSelection) && View.IsSelectable(Item)
            ? View.IsSelected(Item) ? "true" : "false"
            : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        if (Grid is null || View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataGridRow<TItem>)} must be rendered inside a {nameof(FormaDataGrid<TItem>)}.");
        }

        // The first draw is not asked about, so what it draws by is recorded here.
        if (!_gate.IsRecorded)
        {
            _gate.Record(Item, Position, ViewRevision, Context.CultureState.Revision, GetState());
        }
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var state = GetState();

        if (!_gate.Differs(Item, Position, ViewRevision, Context.CultureState.Revision, state) && !Grid.IsEditing(Item))
        {
            return false;
        }

        _gate.Record(Item, Position, ViewRevision, Context.CultureState.Revision, state);

        return true;
    }

    private int GetState()
        => DataViewItemGate.GetState(View, Item) | (Grid.IsEditing(Item) ? 1 << DataViewItemGate.SharedBitCount : 0);

}
