namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>The action after an appending data view's items, which loads the next page, and loads it as the reader reaches it where the data view appends automatically.</summary>
public sealed partial class FormaDataViewMore : FormaComponentBase
{
    // The end of the items counts as reached a little before it scrolls into view, so the next page is on its way as the reader arrives.
    private static readonly IntersectionWatchOptions WatchOptions = new() { RootMarginPixels = 200 };

    private ElementReference _element;
    private Task<DomObservation<IntersectionWatchOptions, IntersectionState>>? _watching;
    private bool _isWatching;
    private int _measuredItemCount;

    /// <summary>Whether the next page is on its way.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Whether the last page asked for failed to load.</summary>
    [Parameter] public bool IsFailed { get; set; }

    /// <summary>Whether reaching the end of the items loads the next page.</summary>
    [Parameter] public bool IsAutomatic { get; set; }

    /// <summary>Loads the next page.</summary>
    [Parameter] public EventCallback OnLoadMore { get; set; }

    /// <summary>How many items the data view holds.</summary>
    [Parameter] public int ItemCount { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataView<>);

    // The end is watched while reaching it loads the next page and no page is on its way.
    private bool ShouldWatch
        => IsAutomatic && !Busy && !IsFailed;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The watch is made once, by the first render that wants it, and a render arriving while it is still being made waits for it.
        if (_watching is null)
        {
            if (!ShouldWatch)
            {
                return;
            }

            _isWatching = true;
            _measuredItemCount = ItemCount;
            _watching = Context.DomWatcher.GetElement(_element).Intersection.WatchAsync(HandleInViewAsync, WatchOptions);
        }

        var watch = await _watching;

        // Renders waiting on the watch can resume out of turn, so each brings it in step with the latest render rather than its own.
        var watches = ShouldWatch;
        var wasWatching = _isWatching;
        _isWatching = watches;
        await watch.SyncAsync(_element, WatchOptions, watches);

        if (!watches || !_isWatching)
        {
            return;
        }

        // A page drawn since the end was last measured can leave it in view, which the watch reports only as it changes; a watch attached
        // again measures the end afresh as it attaches, so only one left as it was is asked to.
        var measures = wasWatching && watch.IsActive && ItemCount != _measuredItemCount;
        _measuredItemCount = ItemCount;

        if (measures)
        {
            await watch.RefreshAsync();
        }
    }

    // The state arrives off the renderer.
    private Task HandleInViewAsync(IntersectionState state)
        => state.IsIntersecting ? InvokeAsync(LoadMoreAsync) : Task.CompletedTask;

    private Task LoadMoreAsync()
        => OnLoadMore.InvokeAsync();

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_watching is { IsCompletedSuccessfully: true } watching)
        {
            await (await watching).DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
