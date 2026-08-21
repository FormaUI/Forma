namespace Forma.UI.Blazor.Components.Layout;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>One row of commands, scrolling them behind arrow buttons when they outgrow the width available.</summary>
public sealed partial class FormaCommandBarRow : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.UI/Components/Layout/FormaCommandBarRow.razor.js";

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

    private JsModule _module = default!;
    private DotNetObjectReference<FormaCommandBarRow>? _self;
    private ElementReference _track;
    private bool _isObserving;
    private bool _canScrollBack;
    private bool _canScrollForward;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-bar-row" : $"forma-command-bar-row {Class}";

    /// <summary>Reports how far the row can still be scrolled in each direction; called from the observer when that changes.</summary>
    [JSInvokable]
    public Task ReportOverflowAsync(bool canScrollBack, bool canScrollForward)
    {
        if (_canScrollBack == canScrollBack && _canScrollForward == canScrollForward)
        {
            return Task.CompletedTask;
        }

        _canScrollBack = canScrollBack;
        _canScrollForward = canScrollForward;

        // The observer fires off the renderer, so the rerender has to be marshalled back onto it.
        return InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _self = DotNetObjectReference.Create(this);
        _isObserving = true;

        await _module.TryInvokeVoidAsync("observe", _track, _self);
    }

    private async Task ScrollAsync(int direction)
        => await _module.TryInvokeVoidAsync("scrollByPage", _track, direction);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // A prerendered row never observes, and issuing interop while statically rendered throws.
        if (_isObserving)
        {
            await _module.TryInvokeVoidAsync("unobserve", _track);
        }

        await _module.DisposeAsync();

        _self?.Dispose();
    }
}
