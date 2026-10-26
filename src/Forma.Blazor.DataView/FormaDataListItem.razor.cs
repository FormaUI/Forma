namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>One item of a list: its box where selection is on, its thumbnail, its name and the caller's secondary text.</summary>
/// <typeparam name="TItem">The item the list item shows.</typeparam>
/// <remarks>The list item draws again only when its item, its place, what the list draws every item by or its own state changed.</remarks>
public sealed partial class FormaDataListItem<TItem> : FormaComponentBase
    where TItem : notnull
{
    private DataViewItemGate _gate;

    /// <summary>The list the item is drawn for.</summary>
    [Parameter, EditorRequired] public FormaDataList<TItem> List { get; set; } = default!;

    /// <summary>The item the list item shows.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; } = default!;

    /// <summary>The item's place in the whole set, where a virtualizing list draws it.</summary>
    [Parameter] public int? Position { get; set; }

    /// <summary>The token the selection behavior names the item by, which the list asks for each time it draws.</summary>
    [Parameter] public int Token { get; set; }

    /// <summary>What the list draws every item by.</summary>
    [Parameter] public int ViewRevision { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field an item's box names to the input base as its value expression.
    private bool ItemSelection { get; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataList<TItem>);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        if (List is null || View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataListItem<TItem>)} must be rendered inside a {nameof(FormaDataList<TItem>)}.");
        }

        // The first draw is not asked about, so what it draws by is recorded here.
        if (!_gate.IsRecorded)
        {
            _gate.Record(Item, Position, ViewRevision, CultureState.Revision, DataViewItemGate.GetState(View, Item));
        }
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var state = DataViewItemGate.GetState(View, Item);

        if (!_gate.Differs(Item, Position, ViewRevision, CultureState.Revision, state))
        {
            return false;
        }

        _gate.Record(Item, Position, ViewRevision, CultureState.Revision, state);

        return true;
    }
}
