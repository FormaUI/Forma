namespace Forma.UI.Blazor.Components.Interactive;

using Forma.UI.Blazor.Components.Buttons;
using Forma.UI.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A trigger button that opens an anchored menu of commands.</summary>
public sealed partial class FormaDropDownMenu : ComponentBase, IAsyncDisposable
{
    private const string PlacementModulePath = "./_content/Forma.UI/scripts/AnchoredPopup.js";
    private const string KeysModulePath = "./_content/Forma.UI/Components/Interactive/FormaDropDownMenu.razor.js";

    /// <summary>Content rendered inside the trigger button.</summary>
    [Parameter, EditorRequired] public RenderFragment Trigger { get; set; } = default!;

    /// <summary>Non-interactive content rendered above the items, such as the name of what the menu acts on.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The menu's items and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Appearance of the trigger button.</summary>
    [Parameter] public FormaButtonOptions TriggerOptions { get; set; } = FormaButtonOptions.Default;

    /// <summary>Additional attributes applied to the trigger button, such as aria-label for an icon-only trigger.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _popupId = $"ddm-popup-{Guid.NewGuid():N}";
    private readonly List<FormaDropDownMenuItem> _items = [];

    private bool _isOpen;
    private bool _placementAttached;
    private bool _openAtLastItem;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private FormaButton _trigger = default!;
    private ElementReference _popupElement;
    private JsModule _placementModule = default!;
    private JsModule _keysModule = default!;
    private IJSObjectReference? _placement;

    protected override void OnInitialized()
    {
        _placementModule = new JsModule(JS, PlacementModulePath);
        _keysModule = new JsModule(JS, KeysModulePath);
    }

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger, for an item that has just been activated.</summary>
    internal async Task RequestActivateAsync()
    {
        // Reached from an item's own event, which only re-renders that item — the parent
        // needs telling explicitly so the popup actually disappears.
        _isOpen = false;
        StateHasChanged();
        await _trigger.Element.FocusAsync();
    }

    // The trigger's own class and ARIA state are folded in beside whatever the host passed,
    // since FormaButton takes a single attribute bag and merges the class itself.
    private Dictionary<string, object> TriggerAttributes
    {
        get
        {
            var attributes = AdditionalAttributes is null
                ? []
                : new Dictionary<string, object>(AdditionalAttributes, StringComparer.Ordinal);

            attributes["class"] = attributes.TryGetValue("class", out var hostClass)
                ? $"forma-dropdown-menu-trigger {hostClass}"
                : "forma-dropdown-menu-trigger";

            attributes["aria-haspopup"] = "menu";
            attributes["aria-expanded"] = _isOpen ? "true" : "false";

            if (_isOpen)
            {
                attributes["aria-controls"] = _popupId;
            }

            return attributes;
        }
    }

    private void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open(atLastItem: false);
        }
    }

    // Items register while the popup renders, so which one to focus is resolved afterwards
    // rather than indexed here against a list that is still empty.
    private void Open(bool atLastItem)
    {
        _isOpen = true;
        _openAtLastItem = atLastItem;
    }

    private void Close()
        => _isOpen = false;

    private void HandleTriggerKeyDown(KeyboardEventArgs args)
    {
        if (_isOpen)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                Open(atLastItem: false);
                break;
            case "ArrowUp":
                Open(atLastItem: true);
                break;
        }
    }

    private async Task HandlePopupKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowDown":
                await MoveFocusAsync(1);
                break;
            case "ArrowUp":
                await MoveFocusAsync(-1);
                break;
            case "Home":
                await FocusIndexAsync(0);
                break;
            case "End":
                await FocusIndexAsync(_items.Count - 1);
                break;
            case "Escape":
                await RequestActivateAsync();
                break;
            case "Tab":
                Close();
                break;
        }
    }

    private Task MoveFocusAsync(int delta)
    {
        if (_items.Count == 0)
        {
            return Task.CompletedTask;
        }

        return FocusIndexAsync(((_focusedIndex + delta) % _items.Count + _items.Count) % _items.Count);
    }

    private async Task FocusIndexAsync(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        _focusedIndex = index;
        await _items[_focusedIndex].FocusAsync();
    }

    // The popup is rebuilt each time it opens, so each open gets its own placement instance
    // bound to that element rather than reusing a stale one.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isOpen == _placementAttached)
        {
            return;
        }

        try
        {
            if (_isOpen)
            {
                _placement = await _placementModule
                    .InvokeAsync<IJSObjectReference>("AnchoredPopup.create", _popupElement, _anchorElement);

                await _placement.InvokeVoidAsync("open");
                await _keysModule.InvokeVoidAsync("attachNavigationKeys", _popupElement);
                await FocusIndexAsync(_openAtLastItem ? _items.Count - 1 : 0);
            }
            else
            {
                await ReleasePlacementAsync();
            }

            _placementAttached = _isOpen;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Interop failed — safe to ignore.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await ReleasePlacementAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Module never loaded — nothing to release.
        }

        await _placementModule.DisposeAsync();
        await _keysModule.DisposeAsync();
    }

    private async ValueTask ReleasePlacementAsync()
    {
        if (_placement is null)
        {
            return;
        }

        var placement = _placement;
        _placement = null;

        await placement.InvokeVoidAsync("close");
        await placement.DisposeAsync();
    }
}
