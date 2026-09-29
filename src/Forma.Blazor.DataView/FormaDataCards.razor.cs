namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>A view over a data view drawing each item as a card, its thumbnail, its name and what the caller places beneath, packed across the width; at its compact density, the thumbnail and the name alone.</summary>
/// <typeparam name="TItem">The item a card shows.</typeparam>
public sealed partial class FormaDataCards<TItem> : FormaElementBase, IDataViewViewport
    where TItem : notnull
{
    private const string ViewKey = "cards";

    /// <summary>The information drawn beneath an item's name, given the item; left out at the compact density.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction the card opens with, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>The item's actions, drawn in a bar at the card's end, given the item; left out at the compact density.</summary>
    [Parameter] public RenderFragment<TItem>? ActionsTemplate { get; set; }

    /// <summary>The narrowest a card packs to; as many pack across as the width holds at it.</summary>
    [Parameter] public CssLength CardSize { get; set; } = CssLength.FromRem(9);

    /// <summary>Draws each card as its thumbnail and name alone, the compact density a consumer shows as thumbnails.</summary>
    [Parameter] public bool Dense { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private CollectionName _collectionName = CollectionName.None;

    // The field a card's box names to the input base; the selection is not a form field and nothing binds it.
    private bool ItemSelection { get; }

    private bool IsActive
        => View.ActiveViewKey == ViewKey;

    private bool HasSelection
        => View.EnableCheckedSelection || View.EnableItemSelection;

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RefreshAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataCards<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["Cards"], Glyph.GridView, this));
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _collectionName = CollectionName.From(AdditionalAttributes);

        // The cards pack every item they are given; a window fetched as it scrolls has no pack to join.
        if (IsActive && View.Virtualize && !View.EnablePagination)
        {
            throw new InvalidOperationException($"{nameof(FormaDataCards<TItem>)} does not virtualize; page the data view or show it in the grid.");
        }
    }

    private string? GetCardClass(TItem item)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-card");
        builder.Add(View.ItemClass?.Invoke(item));

        if (HasSelection)
        {
            builder.Add("forma-data-card-selected", View.IsSelected(item));
            builder.Add("forma-data-card-disabled", !View.CanInteractWith(item));
        }

        return builder.ToStringOrNull();
    }

    private string? GetAriaSelected(TItem item)
        => HasSelection ? View.IsSelected(item) ? "true" : "false" : null;

    // A press on an item with a marker is the selection's, so a double press activates the item instead, as Enter on its marker does.
    private Task HandleDoubleClickAsync(TItem item)
        => HasSelection ? Interaction.ActivateAsync(item) : Task.CompletedTask;

    private string GetGroupNameId(int index)
        => $"{ElementId}-group-{index}";

    private Task HandleViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-cards")
            .Add("forma-data-cards-dense", Dense);
    }

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("--forma-data-cards-size", CardSize);
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);

        return base.DisposeAsyncCore();
    }
}
