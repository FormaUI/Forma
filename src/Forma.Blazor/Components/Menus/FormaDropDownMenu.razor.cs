namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A trigger button that opens an anchored menu of commands.</summary>
public sealed partial class FormaDropDownMenu : ComponentBase, IAsyncDisposable
{
    /// <summary>Content rendered inside the trigger button.</summary>
    [Parameter, EditorRequired] public RenderFragment Trigger { get; set; } = default!;

    /// <summary>Non-interactive content rendered above the items, such as the name of what the menu acts on.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The menu's items and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Appearance of the trigger button.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.Default;

    /// <summary>Additional attributes applied to the trigger button, such as aria-label for an icon-only trigger.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _popupId = $"ddm-popup-{Guid.NewGuid():N}";
    private readonly List<FormaDropDownMenuItem> _items = [];

    private bool _openAtLastItem;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private FormaButton _trigger = default!;
    private FormaPopup Popup { get; set; } = default!;
    private NavigationScrollGuard _keyGuard = default!;

    private bool IsOpen
        => Popup is { IsOpen: true };

    /// <inheritdoc />
    protected override void OnInitialized()
        => _keyGuard = new NavigationScrollGuard(JS);

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger, for an item that has just been activated.</summary>
    internal Task RequestActivateAsync()
        => Popup.CloseAsync();

    // The trigger's own class and ARIA state are folded in beside whatever the host passed,
    // since FormaButton takes a single attribute bag and merges the class itself.
    private Dictionary<string, object> TriggerAttributes
    {
        get
        {
            var attributes = AdditionalAttributes is null
                ? []
                : new Dictionary<string, object>(AdditionalAttributes, StringComparer.Ordinal);

            attributes["class"] = AdditionalAttributes.MergeClass("forma-dropdown-menu-trigger");

            attributes["aria-haspopup"] = "menu";
            attributes["aria-expanded"] = IsOpen ? "true" : "false";

            if (IsOpen)
            {
                attributes["aria-controls"] = _popupId;
            }

            return attributes;
        }
    }

    private Task ToggleAsync()
    {
        _openAtLastItem = false;

        return Popup.ToggleAsync(_trigger.Element);
    }

    // Items register while the popup renders, so which one to focus is resolved afterwards
    // rather than indexed here against a list that is still empty.
    private async Task OpenAsync(bool atLastItem)
    {
        _openAtLastItem = atLastItem;
        await Popup.OpenAsync(_trigger.Element);
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsOpen)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                await OpenAsync(atLastItem: false);
                break;
            case "ArrowUp":
                await OpenAsync(atLastItem: true);
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
        }
    }

    private async Task HandleOpenedAsync()
    {
        await _keyGuard.AttachAsync(Popup.Element);
        await FocusIndexAsync(_openAtLastItem ? _items.Count - 1 : 0);
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _keyGuard.DisposeAsync();
}
