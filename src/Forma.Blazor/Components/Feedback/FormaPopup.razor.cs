namespace Forma.Blazor.Components.Feedback;

using Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>An anchored popup shown in the browser's top layer, dismissed by Escape or an outside click, and returning focus to its trigger after keyboard dismissal.</summary>
public sealed partial class FormaPopup : ComponentBase, IAsyncDisposable
{
    /// <summary>The element the popup hangs from.</summary>
    [Parameter, EditorRequired] public ElementReference Anchor { get; set; }

    /// <summary>Whether Tab moves between the popup's own controls instead of leaving it.</summary>
    [Parameter] public bool KeepsFocus { get; set; }

    /// <summary>Whether closing returns focus to the trigger when focus is inside the popup at the time. An outside click never moves focus.</summary>
    [Parameter] public bool ReturnsFocus { get; set; } = true;

    /// <summary>The popup's content, rendered only while it is open.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised whenever the popup opens or closes, so a component drawing the trigger's expanded state redraws it.</summary>
    [Parameter] public EventCallback<bool> OnOpenChanged { get; set; }

    /// <summary>Raised once the popup is open and positioned, for content that takes focus or attaches a behaviour of its own.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>Raised for every key pressed inside the popup other than Escape, which closes it wherever focus is.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>Additional attributes applied to the popup element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private AnchoredPopup _popup = default!;
    private DotNetObjectReference<FormaPopup> _selfReference = default!;
    private ElementReference _element;
    private ElementReference _trigger;

    /// <summary>Whether the popup is open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The popup element, for content that attaches a behaviour to it.</summary>
    public ElementReference Element => _element;

    private Dictionary<string, object> RootAttributes
    {
        get
        {
            var attributes = AdditionalAttributes is null
                ? []
                : new Dictionary<string, object>(AdditionalAttributes, StringComparer.Ordinal);

            attributes["class"] = AdditionalAttributes.MergeClass("forma-popup", IsOpen ? "forma-popup-open" : null);

            return attributes;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _popup = new AnchoredPopup(JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <summary>Opens the popup, remembering the trigger as where focus returns when it closes.</summary>
    public async Task OpenAsync(ElementReference trigger)
    {
        if (IsOpen)
        {
            return;
        }

        _trigger = trigger;
        await SetOpenAsync(true);
    }

    /// <summary>Closes the popup, returning focus to the trigger when <see cref="ReturnsFocus"/> and focus is inside it.</summary>
    public Task CloseAsync()
        => CloseAsync(ReturnsFocus);

    /// <summary>Opens the popup from the trigger, or closes it when it is open.</summary>
    public Task ToggleAsync(ElementReference trigger)
        => IsOpen ? CloseAsync() : OpenAsync(trigger);

    /// <summary>Closes the popup and returns focus to its trigger; called by the script when Escape is pressed while the popup is open.</summary>
    [JSInvokable]
    public Task HandleEscapeAsync()
        => CloseAsync();

    private Task DismissAsync()
        => CloseAsync(returnsFocus: false);

    private async Task CloseAsync(bool returnsFocus)
    {
        if (!IsOpen)
        {
            return;
        }

        // Focus is moved while the content is still in the document, so where it is can be seen.
        await _popup.CloseAsync(returnsFocus);
        await SetOpenAsync(false);
    }

    private async Task SetOpenAsync(bool isOpen)
    {
        IsOpen = isOpen;
        StateHasChanged();
        await OnOpenChanged.InvokeAsync(isOpen);
    }

    // Escape reaches the script's own listener, which closes the popup wherever focus is.
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key != "Escape")
        {
            await OnKeyDown.InvokeAsync(args);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await _popup.SyncAsync(_selfReference, IsOpen, _element, Anchor, _trigger, async () => await OnOpened.InvokeAsync(), KeepsFocus);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _popup.DisposeAsync();
        }
        finally
        {
            _selfReference.Dispose();
        }
    }
}
