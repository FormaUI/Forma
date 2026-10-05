namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>A view over a data view drawing the items down the height and on into further columns, each a thumbnail, its name and the caller's secondary text.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataList<TItem> : FormaElementBase, IDataViewViewport, IDataViewPositions<TItem>
    where TItem : notnull
{
    private const string ViewKey = "list";

    /// <summary>The secondary text drawn beside an item's name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The item's depiction drawn before its name, given the item.</summary>
    [Parameter] public RenderFragment<TItem>? ThumbnailTemplate { get; set; }

    /// <summary>Whether the rows tighten to the line of an item's name.</summary>
    [Parameter] public bool Dense { get; set; }

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    private CollectionName _collectionName = CollectionName.None;
    private DataViewRows<TItem> _rows = default!;
    private Virtualize<DataViewRow<TItem>>? _virtualizer;
    private bool _isRefreshWanted;

    // The field an item's box names to the input base as its value expression.
    private bool ItemSelection { get; }

    private bool IsActive
        => View.ActiveViewKey == ViewKey;

    private bool HasSelection
        => View.EnableCheckedSelection || View.EnableItemSelection;

    private bool IsVirtualizing
        => View.Virtualize && !View.EnablePagination;

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
    int? IDataViewPositions<TItem>.GetEdgePosition(bool last)
        => _rows.GetEdgePosition(last);

    /// <inheritdoc />
    ValueTask<IReadOnlyList<TItem>> IDataViewPositions<TItem>.ReadAsync(int from, int to, CancellationToken cancellationToken)
        => _rows.ReadAsync(from, to, cancellationToken);

    /// <inheritdoc />
    ValueTask IDataViewPositions<TItem>.ScrollToPositionAsync(int position, CancellationToken cancellationToken)
        => ScrollToItemAsync(position, cancellationToken);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataList<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _rows = new DataViewRows<TItem>(View,
            () => _virtualizer is not null,
            () => _virtualizer!.RefreshDataAsync(),
            (index, token) => _virtualizer!.ScrollToItemAsync(index, cancellationToken: token),
            InvokeAsync, StateHasChanged);
        Interaction.RegisterView(new DataViewRegistration(ViewKey, L["List"], Glyph.List, this));
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
