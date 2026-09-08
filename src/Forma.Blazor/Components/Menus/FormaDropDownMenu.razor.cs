namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Framework.Common.Interop.Behaviours;

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

    /// <summary>The inline edge of the trigger the menu lines up with.</summary>
    [Parameter] public PopoverAlignment Alignment { get; set; }

    /// <summary>Additional attributes applied to the trigger button, such as aria-label for an icon-only trigger.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _popoverId = $"ddm-popover-{Guid.NewGuid():N}";
    private readonly List<FormaDropDownMenuItem> _items = [];

    private bool _openAtLastItem;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private FormaButton _trigger = default!;
    private FormaPopover Popover { get; set; } = default!;
    private NavigationGuard _keyGuard = default!;

    private bool IsOpen
        => Popover is { IsOpen: true };

    /// <inheritdoc />
    protected override void OnInitialized()
        => _keyGuard = new NavigationGuard(JS);

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger, for an item that has just been activated.</summary>
    internal Task RequestActivateAsync()
        => Popover.CloseAsync();

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
                attributes["aria-controls"] = _popoverId;
            }

            return attributes;
        }
    }

    private Task ToggleAsync()
    {
        _openAtLastItem = false;

        return Popover.ToggleAsync(_trigger.Element);
    }

    // Items register while the popover renders, so which one to focus is resolved afterwards
    // rather than indexed here against a list that is still empty.
    private async Task OpenAsync(bool atLastItem)
    {
        _openAtLastItem = atLastItem;
        await Popover.OpenAsync(_trigger.Element);
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

    private async Task HandlePopoverKeyDownAsync(KeyboardEventArgs args)
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
                await FocusIndexAsync(FindEnabledIndex(0, 1));
                break;
            case "End":
                await FocusIndexAsync(FindEnabledIndex(_items.Count - 1, -1));
                break;
        }
    }

    private async Task HandleOpenedAsync()
    {
        await _keyGuard.AttachAsync(Popover.Element);
        await FocusIndexAsync(_openAtLastItem ? FindEnabledIndex(_items.Count - 1, -1) : FindEnabledIndex(0, 1));
    }

    private Task MoveFocusAsync(int delta)
        => FocusIndexAsync(FindEnabledIndex(_focusedIndex + delta, delta));

    /// <summary>The index of the first enabled item at or past <paramref name="start"/> in the direction of <paramref name="step"/>, wrapping around the ends; null when no item is enabled.</summary>
    private int? FindEnabledIndex(int start, int step)
    {
        for (var visited = 0; visited < _items.Count; visited++)
        {
            var index = ((start + visited * step) % _items.Count + _items.Count) % _items.Count;

            if (!_items[index].Disabled)
            {
                return index;
            }
        }

        return null;
    }

    private async Task FocusIndexAsync(int? index)
    {
        if (index is not { } target)
        {
            return;
        }

        _focusedIndex = target;
        await _items[_focusedIndex].FocusAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _keyGuard.DisposeAsync();
}
