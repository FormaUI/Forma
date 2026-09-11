namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A trigger button that opens an anchored menu of commands.</summary>
public sealed partial class FormaDropDownMenu : FormaElementBase
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

    private readonly List<FormaDropDownMenuItem> _items = [];

    private bool _openAtLastItem;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private FormaButton TriggerButton { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;
    private NavigationGuard _keyGuard = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => TriggerButton is null ? default : TriggerButton.RootElement;

    private string ElementId
        => Id ?? $"ddm-{Context.RuntimeId:N}";

    private string PopoverId
        => $"{ElementId}-popover";

    private bool IsOpen
        => Popover is { IsOpen: true };

    private string AriaExpanded
        => IsOpen ? "true" : "false";

    private string? AriaControls
        => IsOpen ? PopoverId : null;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _keyGuard = new NavigationGuard(Context.JS);

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger, for an item that has just been activated.</summary>
    internal Task RequestActivateAsync()
        => Popover.CloseAsync();

    private Task ToggleAsync()
    {
        _openAtLastItem = false;

        return Popover.ToggleAsync(TriggerButton.RootElement);
    }

    // Items register while the popover renders, so which one to focus is resolved afterwards
    // rather than indexed here against a list that is still empty.
    private async Task OpenAsync(bool atLastItem)
    {
        _openAtLastItem = atLastItem;
        await Popover.OpenAsync(TriggerButton.RootElement);
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
        await _keyGuard.AttachAsync(Popover.RootElement);
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
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-trigger");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _keyGuard.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
