namespace Forma.UI.Blazor.Components.Layout;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>One row of commands, scrolling them behind arrow buttons when they outgrow the width available.</summary>
public sealed partial class FormaCommandBarRow : ComponentBase, IAsyncDisposable
{
    /// <summary>The row's commands.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Accessible name for the button scrolling toward the start.</summary>
    [Parameter] public string? ScrollBackLabel { get; set; }

    /// <summary>Accessible name for the button scrolling toward the end.</summary>
    [Parameter] public string? ScrollForwardLabel { get; set; }

    /// <summary>Extra classes applied alongside forma-command-bar-row.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the row element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaCommandBarRow> L { get; set; } = default!;

    private ScrollBoundaryWatcher _watcher = default!;
    private Scroll _scroll = default!;
    private DotNetObjectReference<FormaCommandBarRow>? _self;
    private ElementReference _track;
    private bool _canScrollBack;
    private bool _canScrollForward;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-bar-row" : $"forma-command-bar-row {Class}";

    /// <summary>Reports how far the row can still be scrolled in each direction; called from the watcher when that changes.</summary>
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

    protected override void OnInitialized()
    {
        _watcher = new ScrollBoundaryWatcher(JS);
        _scroll = new Scroll(JS);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _self = DotNetObjectReference.Create(this);

        await _watcher.AttachAsync(_track, _self);
    }

    private async Task ScrollAsync(int direction)
        => await _scroll.ByPageAsync(_track, direction);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _watcher.DisposeAsync();
        await _scroll.DisposeAsync();

        _self?.Dispose();
    }
}
