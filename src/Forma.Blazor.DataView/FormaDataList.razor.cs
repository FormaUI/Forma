namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A view over a data view drawing the items down the height, each a thumbnail where the caller templates one, its name and the caller's secondary text, flowing on into further columns.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataList<TItem> : FormaElementBase, IDataViewViewport
    where TItem : notnull
{
    private const string ViewKey = "list";

    /// <summary>The secondary text drawn beside an item's name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction drawn before its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>Tightens the rows for names alone.</summary>
    [Parameter] public bool Dense { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field an item's box names to the input base; the selection is not a form field and nothing binds it.
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
            throw new InvalidOperationException($"{nameof(FormaDataList<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["List"], Glyph.List, this));
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // The list flows every item it is given; a window fetched as it scrolls has no flow to join.
        if (IsActive && View.Virtualize && !View.EnablePagination)
        {
            throw new InvalidOperationException($"{nameof(FormaDataList<TItem>)} does not virtualize; page the data view or show it in the grid.");
        }
    }

    private string? GetItemClass(TItem item)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-list-item");
        builder.Add(View.ItemClass?.Invoke(item));

        if (HasSelection)
        {
            builder.Add("forma-data-list-item-selected", View.IsSelected(item));
            builder.Add("forma-data-list-item-disabled", !View.CanInteractWith(item));
        }

        return builder.ToStringOrNull();
    }

    private string? GetAriaSelected(TItem item)
        => HasSelection ? View.IsSelected(item) ? "true" : "false" : null;

    // A press on an item with a marker is the selection's, so a double press or Enter on the marker activates the item instead.
    private Task HandleDoubleClickAsync(TItem item)
        => HasSelection ? Interaction.ActivateAsync(item) : Task.CompletedTask;

    private Task HandleMarkerKeyDownAsync(KeyboardEventArgs args, TItem item)
        => args.Key == "Enter" ? Interaction.ActivateAsync(item) : Task.CompletedTask;

    private Task HandleViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-list")
            .Add("forma-data-list-dense", Dense);
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);

        return base.DisposeAsyncCore();
    }
}
