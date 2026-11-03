namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

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

    // How far ahead of the scroll container the next page is asked for, in pixels.
    private const double SentinelReach = 200;

    private DomObservation<IntersectionWatchOptions, IntersectionState>? _sentinel;

    private ElementReference _sentinelElement;

    private ElementReference _scrollElement;

    private bool _isLoadingPage;

    private bool IsInfiniteScrolling
        => Configuration.Options.InfiniteScroll != ExplorerInfiniteScrollMode.Off;

    private bool WatchesSentinel
        => Configuration.Options.InfiniteScroll == ExplorerInfiniteScrollMode.Automatic && View.Paging.HasMorePages;

    private IntersectionWatchOptions SentinelOptions
        => new() { Root = _scrollElement, RootMarginPixels = SentinelReach };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Configuration is null || Location is null || View is null || Interaction is null || Search is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerContentArea)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // The observation starts inactive, so _sentinel holds it before its first state arrives.
            _sentinel = await Context.DomWatcher.GetElement(_sentinelElement).Intersection.WatchAsync(HandleSentinelAsync, SentinelOptions, shouldObserve: false);
        }

        if (_sentinel is { } sentinel)
        {
            await sentinel.SyncAsync(_sentinelElement, SentinelOptions, WatchesSentinel);
        }
    }

    // The state arrives off the renderer.
    private Task HandleSentinelAsync(IntersectionState state)
        => state.IsIntersecting ? InvokeAsync(LoadNextPageAsync) : Task.CompletedTask;

    private async Task LoadNextPageAsync()
    {
        if (_isLoadingPage || !IsInfiniteScrolling || !View.Paging.HasMorePages)
        {
            return;
        }

        _isLoadingPage = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await Interaction.RequestNextPageAsync();
        }
        finally
        {
            _isLoadingPage = false;
        }

        // The observation reports changes alone, and the new page can leave the sentinel in view.
        await InvokeAsync(StateHasChanged);

        if (_sentinel is { } sentinel)
        {
            await sentinel.RefreshAsync();
        }
    }

    private Task HandlePageChangedAsync(int page)
        => Interaction.ChangePageAsync(page);

}
