namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>One row of a grid: the item's selection control where selection is on, then a cell per column.</summary>
/// <typeparam name="TItem">The item the row shows.</typeparam>
public sealed partial class FormaDataGridRow<TItem> : FormaComponentBase
    where TItem : notnull
{
    /// <summary>The item the row shows.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; } = default!;

    [CascadingParameter] private FormaDataGrid<TItem> Grid { get; set; } = default!;

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field a row's box names to the input base; the selection is not a form field and nothing binds it.
    private bool RowSelection { get; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataGrid<TItem>);

    private string? RowClass
        => Grid.GetRowClass(Item);

    private string? AriaSelected
        => View.EnableCheckedSelection || View.EnableItemSelection
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
    }

}
