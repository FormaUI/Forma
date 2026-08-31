namespace Forma.Blazor.Components.Bars;

using Forma.Blazor.Interop.Behaviours;
using Forma.Blazor.Interop.Operations;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>One bar of commands, scrolling them behind arrow buttons when they outgrow the width available.</summary>
public sealed partial class FormaCommandBar : ComponentBase, IAsyncDisposable
{
    /// <summary>The bar's commands.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Content pinned past the end of the track, staying put while the commands scroll.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>How the bar's track scrolls.</summary>
    [Parameter] public CommandBarOptions Options { get; set; } = CommandBarOptions.Default;

    /// <summary>Extra classes applied alongside forma-command-bar.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Hides the bar while keeping it mounted, so its scroll-boundary watcher and other JS-attached state survive across toggles.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <summary>Additional attributes applied to the bar element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaCommandBar> L { get; set; } = default!;

    private ScrollBoundaryWatcher _boundaryWatcher = default!;
    private WheelScroll _wheelScroll = default!;
    private Scroll _scroll = default!;
    private DotNetObjectReference<FormaCommandBar>? _selfReference;
    private ElementReference _barElement;
    private ElementReference _trackElement;
    private bool _canScrollBack;
    private bool _canScrollForward;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-bar" : $"forma-command-bar {Class}";

    /// <summary>Reports how far the bar can still be scrolled in each direction; called from the watcher when that changes.</summary>
    [JSInvokable]
    public Task HandleOverflowAsync(bool canScrollBack, bool canScrollForward)
    {
        if (_canScrollBack == canScrollBack && _canScrollForward == canScrollForward)
        {
            return Task.CompletedTask;
        }

        _canScrollBack = canScrollBack;
        _canScrollForward = canScrollForward;

        // The watcher fires off the renderer, so the rerender has to be marshalled back onto it.
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _boundaryWatcher = new ScrollBoundaryWatcher(JS);
        _wheelScroll = new WheelScroll(JS);
        _scroll = new Scroll(JS);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfReference = DotNetObjectReference.Create(this);

        await _boundaryWatcher.AttachAsync(_trackElement, _selfReference);

        // A wheel reports vertically alone, so its turn anywhere over the bar is read as movement along the track.
        if (Options.PageOnWheelGesture)
        {
            await _wheelScroll.AttachAsync(_barElement, _trackElement);
        }
    }

    private async Task ScrollAsync(int direction)
        => await _scroll.ByPageAsync(_trackElement, direction);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _boundaryWatcher.DisposeAsync();
        await _wheelScroll.DisposeAsync();
        await _scroll.DisposeAsync();

        _selfReference?.Dispose();
    }
}
