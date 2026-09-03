namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Interop.Behaviours;
using Forma.Features.Explorer;
using Forma.Features.Explorer.Content;
using Forma.Features.Explorer.Content.Queries;
using Forma.Features.Explorer.Navigation;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The view switch, infinite scroll, and pagination for the resources an explorer shows.</summary>
public sealed partial class FormaExplorerContentArea : ComponentBase, IAsyncDisposable
{
    [CascadingParameter] private IExplorerConfigurationView Configuration { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerContentView View { get; set; } = default!;

    [CascadingParameter] private IExplorerContentInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

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

        _sentinel = new SentinelObserver<FormaExplorerContentArea>(JS);
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

    /// <summary>Appends the next page of media, as the reader scrolls toward the end of the current one.</summary>
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
    public async ValueTask DisposeAsync()
        => await _sentinel.DisposeAsync();
}
