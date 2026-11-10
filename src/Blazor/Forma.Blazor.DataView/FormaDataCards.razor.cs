namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.DataView.Interop;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A view over a data view drawing each item as a card, its thumbnail, its name and what the caller places beneath, packed across the width; at its compact density, the thumbnail and the name alone.</summary>
/// <typeparam name="TItem">The item a card shows.</typeparam>
public sealed partial class FormaDataCards<TItem> : FormaElementBase, IDataViewViewport, IDataViewPositions<TItem>
    where TItem : notnull
{
    private const string ViewKey = "cards";

    // What the cards are given that is not their own: the host's attributes, which never change how a card is drawn.
    private static readonly string[] _parametersLeftOut = [nameof(Id), nameof(Class), nameof(Style)];

    /// <summary>The information drawn beneath an item's name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction the card opens with, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>The item's actions, drawn in a bar at the card's end, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ActionsTemplate { get; set; }

    /// <summary>The narrowest a card packs to.</summary>
    [Parameter] public CssLength CardSize { get; set; } = CssLength.FromRem(9);

    /// <summary>Whether each card draws its thumbnail and name alone, the compact density.</summary>
    [Parameter] public bool Dense { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private readonly ParameterRevision _parameterRevision = new(_parametersLeftOut);
    private CollectionName _collectionName = CollectionName.None;
    private DataViewRows<TItem> _rows = default!;
    private GridColumns _gridColumns = default!;
    private DotNetObjectReference<FormaDataCards<TItem>>? _selfReference;
    private FormaVirtualize<DataViewRow<TItem>>? _virtualizer;
    private ElementReference _rulerElement;
    private bool _isRulerAttached;
    private bool _areColumnsKnown;
    private bool _isRefreshWanted;
    private int _initialRowIndex;

    internal bool HasSelection
        => View.EnableCheckedSelection || View.EnableItemSelection;

    // What every card is drawn by: it changes when the data view or the cards' own parameters changed in a way a card draws differently for.
    private int CardRevision
        => View.Revision + _parameterRevision.Value;

    private bool IsActive
        => View.ActiveViewKey == ViewKey;

    private bool IsVirtualizing
        => View.IsVirtualizing;

    private string PlaceholderStyle
        => string.Create(CultureInfo.InvariantCulture, $"block-size: {View.ItemHeight}px");

    /// <inheritdoc />
    public ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken)
        => _rows.ScrollToAsync(index, cancellationToken);

    /// <inheritdoc />
    public ValueTask RefreshAsync()
    {
        _rows.Reset();
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
    int? IDataViewPositions<TItem>.GetEdgePosition(bool last)
        => _rows.GetEdgePosition(last);

    /// <inheritdoc />
    ValueTask<IReadOnlyList<TItem>> IDataViewPositions<TItem>.ReadAsync(int from, int to, CancellationToken cancellationToken)
        => _rows.ReadAsync(from, to, cancellationToken);

    /// <inheritdoc />
    ValueTask IDataViewPositions<TItem>.ScrollToPositionAsync(int position, CancellationToken cancellationToken)
        => ScrollToItemAsync(position, cancellationToken);

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _parameterRevision.Observe(parameters);

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataCards<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _rows = new DataViewRows<TItem>(View, () => _virtualizer, InvokeAsync, StateHasChanged);
        _gridColumns = new GridColumns(Context.JS);
        Interaction.RegisterView(new DataViewRegistration(ViewKey, () => L["Cards"], Glyph.GridView, this));
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
        // The ruler is measured while it shows, and let go once another view or paging takes it off the page.
        var showsRuler = IsActive && IsVirtualizing && View.HasLoaded;

        if (showsRuler && !_isRulerAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);

            // Claimed before the await, so a render arriving mid-call measures no second ruler.
            _isRulerAttached = true;
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
            // The virtualization fetches the rows and leaves drawing them to its host.
            await virtualizer.RefreshDataAsync();
            StateHasChanged();
        }
    }

    internal string? GetCardClass(TItem item)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-card");
        builder.Add(View.ItemClass?.Invoke(item));

        if (HasSelection)
        {
            builder.Add("forma-data-card-selected", View.IsSelected(item));
            builder.Add("forma-data-card-disabled", View.IsMuted(item));
        }

        return builder.ToStringOrNull();
    }

    // An item that cannot be selected says nothing of it, so it is never announced as an unselected choice.
    internal string? GetAriaSelected(TItem item)
        => HasSelection && View.IsSelectable(item) ? View.IsSelected(item) ? "true" : "false" : null;

    private string GetGroupNameId(int index)
        => $"{ElementId}-group-{index}";

    // Only virtualizing cards read the focus, for the rows they pin, and they draw again only when those change.
    internal EventCallback<FocusEventArgs> GetFocusHandler(TItem item)
        => IsVirtualizing ? NonRenderingHandler.Create<FocusEventArgs>(() => HandleFocusIn(item), DispatchExceptionAsync) : default;

    private void HandleFocusIn(TItem item)
    {
        if (_rows.NoteFocus(item))
        {
            StateHasChanged();
        }
    }

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
