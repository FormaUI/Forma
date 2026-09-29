namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

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
    private DataViewRows<TItem> _rows = default!;
    private GridColumns _gridColumns = default!;
    private DotNetObjectReference<FormaDataCards<TItem>>? _selfReference;
    private Virtualize<DataViewRow<TItem>>? _virtualizer;
    private ElementReference _rulerElement;
    private bool _isRulerAttached;
    private bool _areColumnsKnown;
    private bool _isRefreshWanted;
    private int _initialRowIndex;

    // The field a card's box names to the input base; the selection is not a form field and nothing binds it.
    private bool ItemSelection { get; }

    private bool IsActive
        => View.ActiveViewKey == ViewKey;

    private bool HasSelection
        => View.EnableCheckedSelection || View.EnableItemSelection;

    private bool IsVirtualizing
        => View.Virtualize && !View.EnablePagination;

    // A row the virtualization has not fetched yet holds a row's height, so the rows beneath it stand where they will.
    private string PlaceholderStyle
        => string.Create(CultureInfo.InvariantCulture, $"block-size: {View.ItemHeight}px");

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => _virtualizer is { } virtualizer
            ? new ValueTask(virtualizer.ScrollToItemAsync(_rows.GetRowIndex(index), cancellationToken))
            : ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RefreshAsync()
    {
        _rows.Update();

        return _virtualizer is { } virtualizer ? new ValueTask(virtualizer.RefreshDataAsync()) : ValueTask.CompletedTask;
    }

    /// <summary>Lays the cards out in rows of <paramref name="count"/>, the columns the width holds at the card size.</summary>
    /// <param name="count">How many columns the pack lays out.</param>
    [JSInvokable]
    public Task HandleGridColumnsAsync(int count)
    {
        _rows.Columns = count;
        _isRefreshWanted |= _rows.Update() && _virtualizer is not null;

        // The rows open on the initial item once the columns that place it are known.
        if (!_areColumnsKnown)
        {
            _areColumnsKnown = true;
            _initialRowIndex = _rows.GetRowIndex(View.InitialItemIndex);
        }

        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataCards<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _rows = new DataViewRows<TItem>(View);
        _gridColumns = new GridColumns(Context.JS);
        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["Cards"], Glyph.GridView, this));
        View.Subscribe(HandleViewChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _collectionName = CollectionName.From(AdditionalAttributes);
        _isRefreshWanted |= IsVirtualizing && _rows.Update() && _virtualizer is not null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The ruler is measured while it shows; another view or paging takes it off the page with the rows it measured.
        var showsRuler = IsActive && IsVirtualizing && View.HasLoaded;

        if (showsRuler && !_isRulerAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);
            _isRulerAttached = await _gridColumns.AttachAsync(_rulerElement, _selfReference);
        }
        else if (!showsRuler && _isRulerAttached)
        {
            await _gridColumns.DetachAsync();
            _isRulerAttached = false;
            _virtualizer = null;
        }

        if (_isRefreshWanted && _virtualizer is { } virtualizer)
        {
            _isRefreshWanted = false;
            // The virtualization fetches the rows without drawing them, leaving the draw to its host.
            await virtualizer.RefreshDataAsync();
            StateHasChanged();
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

    private string GetGroupNameId(int index)
        => $"{ElementId}-group-{index}";

    // A fold, a new count or a new grouping lays the rows out again, and the rows on screen are fetched again after the render.
    private Task HandleViewChangedAsync()
    {
        _isRefreshWanted |= IsVirtualizing && _rows.Update() && _virtualizer is not null;

        return InvokeAsync(StateHasChanged);
    }

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
    protected override async ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(ViewKey);
        await _gridColumns.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
