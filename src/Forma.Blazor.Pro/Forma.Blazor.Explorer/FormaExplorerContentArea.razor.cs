namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Common.Interop.Behaviors;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The view switch, infinite scroll, and pagination for the items an explorer shows.</summary>
public sealed partial class FormaExplorerContentArea : FormaComponentBase
{
    [CascadingParameter] private IExplorerConfigurationView Configuration { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerContentView View { get; set; } = default!;

    [CascadingParameter] private IExplorerContentInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private SentinelObserver<FormaExplorerContentArea> _sentinel = default!;

    private ElementReference _sentinelElement;

    private ElementReference _scrollElement;

    private bool _isLoadingPage;

    private bool IsInfiniteScrolling
        => Configuration.Options.InfiniteScroll != ExplorerInfiniteScrollMode.Off;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Configuration is null || Location is null || View is null || Interaction is null || Search is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerContentArea)} must be rendered inside a {nameof(FormaExplorer)}.");
        }

        _sentinel = new SentinelObserver<FormaExplorerContentArea>(Context.JS);
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => SyncSentinelAsync();

    private Task SyncSentinelAsync()
    {
        // Only automatic scrolling watches the sentinel; the manual mode waits for the button to be pressed.
        var wantsSentinel = Configuration.Options.InfiniteScroll == ExplorerInfiniteScrollMode.Automatic && View.Paging.HasMorePages;

        return _sentinel.SyncAsync(this, wantsSentinel, _sentinelElement, _scrollElement);
    }

    /// <summary>Appends the next page of items, as the reader scrolls toward the end of the current one.</summary>
    [JSInvokable]
    public async Task HandleReachedAsync()
    {
        if (_isLoadingPage || !IsInfiniteScrolling || !View.Paging.HasMorePages)
        {
            return;
        }

        // An observer reports crossings, not a standing state, so it is re-armed to keep filling a viewport the new page did not.
        _sentinel.Invalidate();
        _isLoadingPage = true;

        StateHasChanged();

        try
        {
            await Interaction.RequestNextPageAsync();
        }
        finally
        {
            _isLoadingPage = false;
        }

        StateHasChanged();
    }

    private Task HandlePageChangedAsync(int page)
        => Interaction.ChangePageAsync(page);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _sentinel.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
