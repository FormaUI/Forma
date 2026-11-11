namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A view over a data view drawing the items down the height and on into further columns, each a thumbnail, its name and the caller's secondary text.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataList<TItem> : FormaElementBase, IDataViewViewport, IDataViewPositions<TItem>
    where TItem : notnull
{
    private const string ViewKey = "list";

    // What the list is given that is not its own: the host's attributes, which never change how an item is drawn.
    private static readonly string[] _parametersLeftOut = [nameof(Id), nameof(Class), nameof(Style)];

    /// <summary>The secondary text drawn beside an item's name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction drawn before its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>Whether the rows tighten to the line of an item's name.</summary>
    [Parameter] public bool Dense { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private readonly ParameterRevision _parameterRevision = new(_parametersLeftOut);
    private DataViewRegistration _dataViewRegistration = default!;
    private CollectionName _collectionName = CollectionName.None;
    private DataViewRows<TItem> _rows = default!;
    private FormaVirtualize<DataViewRow<TItem>>? _virtualizer;
    private bool _isRefreshWanted;

    internal bool HasSelection
        => View.EnableCheckedSelection || View.EnableItemSelection;

    // What every item is drawn by: it changes when the data view or the list's own parameters changed in a way an item draws differently for.
    private int ItemRevision
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

    /// <inheritdoc />
    int? IDataViewPositions<TItem>.GetEdgePosition(bool isLast)
        => _rows.GetEdgePosition(isLast);

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
            throw new InvalidOperationException($"{nameof(FormaDataList<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _rows = new DataViewRows<TItem>(View, () => _virtualizer, InvokeAsync, StateHasChanged);
        _dataViewRegistration = new DataViewRegistration(ViewKey, () => L["List"], Glyph.List, this);
        Interaction.RegisterView(_dataViewRegistration);
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
        if (_isRefreshWanted && _virtualizer is { } virtualizer)
        {
            _isRefreshWanted = false;
            // The virtualization fetches the rows and leaves drawing them to its host.
            await virtualizer.RefreshDataAsync();
            StateHasChanged();
        }
    }

    internal string? GetItemClass(TItem item)
    {
        var builder = new CssClassBuilder();
        builder.Add("forma-data-list-item");
        builder.Add(View.ItemClass?.Invoke(item));

        builder.Add("forma-data-list-item-selected", HasSelection && View.IsSelected(item));

        // A disabled item is muted whether or not a selection is on, and a selection mutes an item its maximum blocks besides.
        builder.Add("forma-data-list-item-disabled", HasSelection ? View.IsMuted(item) : View.IsDisabled(item));

        return builder.ToStringOrNull();
    }

    // An item that cannot be selected says nothing of it, so it is never announced as an unselected choice.
    internal string? GetAriaSelected(TItem item)
        => HasSelection && View.IsSelectable(item) ? View.IsSelected(item) ? "true" : "false" : null;

    private string GetGroupNameId(int index)
        => $"{ElementId}-group-{index}";

    // Only a virtualizing list reads the focus, for the rows it pins, and it draws again only when those change.
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
            .Add("forma-data-list")
            .Add("forma-data-list-dense", Dense);
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        View.Unsubscribe(HandleViewChangedAsync);
        Interaction.UnregisterView(_dataViewRegistration);

        return base.DisposeAsyncCore();
    }
}
