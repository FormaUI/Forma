namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>One card of a set of cards: its thumbnail, its box where selection is on, its name and what the caller places beneath.</summary>
/// <typeparam name="TItem">The item the card shows.</typeparam>
/// <remarks>The card draws again only when its item, its place, what the cards draw every card by or its own state changed.</remarks>
public sealed partial class FormaDataCard<TItem> : FormaComponentBase
    where TItem : notnull
{
    private DataViewItemGate _gate;

    /// <summary>The cards the card is drawn for.</summary>
    [Parameter, EditorRequired] public FormaDataCards<TItem> Cards { get; set; } = default!;

    /// <summary>The item the card shows.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; } = default!;

    /// <summary>The item's place in the whole set, where virtualizing cards draw it.</summary>
    [Parameter] public int? Position { get; set; }

    /// <summary>The token the selection behavior names the item by, which the cards ask for each time they draw.</summary>
    [Parameter] public int Token { get; set; }

    /// <summary>What the cards draw every card by.</summary>
    [Parameter] public int ViewRevision { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field a card's box names to the input base as its value expression.
    private bool ItemSelection { get; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataCards<TItem>);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        if (Cards is null || View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataCard<TItem>)} must be rendered inside a {nameof(FormaDataCards<TItem>)}.");
        }

        // The first draw is not asked about, so what it draws by is recorded here.
        if (!_gate.IsRecorded)
        {
            _gate.Record(Item, Position, ViewRevision, Context.CultureState.Revision, DataViewItemGate.GetState(View, Item));
        }
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var state = DataViewItemGate.GetState(View, Item);

        if (!_gate.Differs(Item, Position, ViewRevision, Context.CultureState.Revision, state))
        {
            return false;
        }

        _gate.Record(Item, Position, ViewRevision, Context.CultureState.Revision, state);

        return true;
    }
}
