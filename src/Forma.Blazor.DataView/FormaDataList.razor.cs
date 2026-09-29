namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>A view over a data view drawing the items down the height, each a thumbnail where the caller templates one, its name and the caller's secondary text, flowing on into further columns, or one row each where the data view virtualizes.</summary>
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

    private CollectionName _collectionName = CollectionName.None;
    private DataViewRows<TItem> _rows = default!;
    private Virtualize<DataViewRow<TItem>>? _virtualizer;
    private bool _isRefreshWanted;

    // The field an item's box names to the input base; the selection is not a form field and nothing binds it.
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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataList<TItem>)} must be declared inside a {nameof(FormaDataView<TItem>)}.");
        }

        _rows = new DataViewRows<TItem>(View);
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
            // The virtualization fetches the rows without drawing them, leaving the draw to its host.
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
