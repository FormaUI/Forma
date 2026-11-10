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
    private DomObservation<IntersectionWatchOptions, IntersectionState>? _watch;
    private bool _wasBusy;

    /// <summary>Whether the next page is on its way.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Whether the last page asked for failed to load.</summary>
    [Parameter] public bool IsFailed { get; set; }

    /// <summary>Whether reaching the end of the items loads the next page.</summary>
    [Parameter] public bool IsAutomatic { get; set; }

    /// <summary>Loads the next page.</summary>
    [Parameter] public EventCallback OnLoadMore { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataView<>);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var watches = IsAutomatic && !Busy && !IsFailed;

        if (_watch is null)
        {
            if (watches)
            {
                _watch = await Context.DomWatcher.GetElement(_element).Intersection.WatchAsync(HandleInViewAsync, WatchOptions);
            }
        }
        else
        {
            await _watch.SyncAsync(_element, WatchOptions, watches);

            // The page just appended can leave the end of the items in view, which the watch reports only as it changes.
            if (watches && _wasBusy)
            {
                await _watch.RefreshAsync();
            }
        }

        _wasBusy = Busy;
    }

    // The state arrives off the renderer.
    private Task HandleInViewAsync(IntersectionState state)
        => state.IsIntersecting ? InvokeAsync(OnLoadMore.InvokeAsync) : Task.CompletedTask;

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_watch is not null)
        {
            await _watch.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
