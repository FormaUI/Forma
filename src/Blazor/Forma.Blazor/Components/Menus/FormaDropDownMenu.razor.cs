namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;

/// <summary>A trigger button that opens an anchored menu of commands.</summary>
public sealed partial class FormaDropDownMenu : FormaElementBase
{
    private const string OpenFirstAction = "menu.open-first";
    private const string OpenLastAction = "menu.open-last";
    private const string NextAction = "menu.next";
    private const string PreviousAction = "menu.previous";
    private const string FirstAction = "menu.first";
    private const string LastAction = "menu.last";
    private const string FindAction = "menu.find";

    private static readonly InputProfile TriggerProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(OpenFirstAction, new KeyBinding("ArrowDown")),
            new InputRoute(OpenLastAction, new KeyBinding("ArrowUp")),
        ],
    };

    private static readonly IReadOnlyList<InputRoute> MenuRoutes =
    [
        new InputRoute(NextAction, new KeyBinding("ArrowDown")),
        new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
        new InputRoute(FirstAction, new KeyBinding("Home")),
        new InputRoute(LastAction, new KeyBinding("End")),
        new InputRoute(FindAction, new TypingBinding()),
    ];

    /// <summary>The content of the trigger button.</summary>
    [Parameter, EditorRequired] public RenderFragment Trigger { get; set; } = default!;

    /// <summary>The non-interactive content above the items, such as the name of what the menu acts on.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The menu's items, groups and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The appearance of the trigger button.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.Default;

    /// <summary>The inline edge of the trigger the menu lines up with.</summary>
    [Parameter] public PopoverAlignment Alignment { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    [Inject] private TimeProvider Clock { get; set; } = default!;

    private readonly List<FormaDropDownMenuItem> _items = [];
    private FormaDropDownMenuItem? _focusedItem;
    private DocumentOrder _documentOrder = default!;
    private ElementLabels _elementLabels = default!;
    private TypeAhead _typeAhead = default!;

    private bool _openAtLastItem;
    private ElementReference _anchorElement;
    private FormaButton TriggerButton { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;
    private InputRegistration _trigger = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => TriggerButton is null ? default : TriggerButton.RootElement;

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
    {
        _trigger = new InputRegistration(InputRouter, HandleActionAsync);
        _documentOrder = new DocumentOrder(Context.JS);
        _elementLabels = new ElementLabels(Context.JS);
        _typeAhead = new TypeAhead(Clock);
    }

    // The trigger's keys route on the trigger, and the menu's on the popover.
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _trigger.SyncAsync(TriggerButton.RootElement, TriggerProfile);

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger.</summary>
    internal Task RequestActivateAsync()
        => Popover.CloseAsync();

    private Task HandleActionAsync(string action, InputInfo input)
    {
        return action switch
        {
            OpenFirstAction when !IsOpen => OpenAsync(atLastItem: false),
            OpenLastAction when !IsOpen => OpenAsync(atLastItem: true),
            NextAction => MoveFocusAsync(1),
            PreviousAction => MoveFocusAsync(-1),
            FirstAction => FocusEndAsync(atLastItem: false),
            LastAction => FocusEndAsync(atLastItem: true),
            FindAction when input is InputKeyInfo { Key: var character } => FindAsync(character),
            _ => Task.CompletedTask,
        };
    }

    private Task HandleMenuActionAsync(InputAction action)
        => HandleActionAsync(action.Action, action.Input);

    private Task ToggleAsync()
    {
        _openAtLastItem = false;

        return Popover.ToggleAsync(TriggerButton.RootElement);
    }

    // Items register while the popover renders, so the item to focus is found once it has opened.
    private async Task OpenAsync(bool atLastItem)
    {
        _openAtLastItem = atLastItem;
        await Popover.OpenAsync(TriggerButton.RootElement);
    }

    private Task HandleOpenedAsync()
        => FocusEndAsync(_openAtLastItem);

    private async Task FocusEndAsync(bool atLastItem)
    {
        var items = await GetItemsAsync();

        await FocusAsync(atLastItem ? FindEnabled(items, items.Count - 1, -1) : FindEnabled(items, 0, 1));
    }

    private async Task MoveFocusAsync(int delta)
    {
        var items = await GetItemsAsync();
        var index = _focusedItem is null ? -1 : items.IndexOf(_focusedItem);
        var start = index < 0 ? (delta > 0 ? 0 : items.Count - 1) : index + delta;

        await FocusAsync(FindEnabled(items, start, delta));
    }

    // The item whose label the typed characters begin takes the focus, its label read as the page shows it.
    private async Task FindAsync(string character)
    {
        var items = await GetItemsAsync();

        if (await _elementLabels.ReadAsync([.. items.Select(item => item.RootElement)]) is not { } labels || labels.Length != items.Count)
        {
            return;
        }

        var current = _focusedItem is null ? -1 : items.IndexOf(_focusedItem);

        if (_typeAhead.Find(character, labels, current, Context.CultureState.Culture, index => !items[index].Disabled) is { } index)
        {
            await FocusAsync(items[index]);
        }
    }

    // The order is read at each move, since the page can reorder the items it keeps; where the browser can't be asked,
    // they keep the order they registered in.
    private async Task<List<FormaDropDownMenuItem>> GetItemsAsync()
    {
        List<FormaDropDownMenuItem> items = [.. _items];

        return await _documentOrder.SortAsync([.. items.Select(item => item.RootElement)]) is { } places && places.Length == items.Count
            ? [.. places.Select(place => items[place])]
            : items;
    }

    /// <summary>Finds the first enabled item at or past <paramref name="start"/> in the direction of <paramref name="step"/>, wrapping around the ends.</summary>
    private static FormaDropDownMenuItem? FindEnabled(List<FormaDropDownMenuItem> items, int start, int step)
    {
        for (var visitedCount = 0; visitedCount < items.Count; visitedCount++)
        {
            var index = ((start + visitedCount * step) % items.Count + items.Count) % items.Count;

            if (!items[index].Disabled)
            {
                return items[index];
            }
        }

        return null;
    }

    // A click can close the menu while the focus waits on the browser, taking its items out of the page, so only an item of
    // the open menu takes it.
    private async Task FocusAsync(FormaDropDownMenuItem? item)
    {
        if (item is null || !IsOpen || !_items.Contains(item))
        {
            return;
        }

        _focusedItem = item;
        await item.FocusAsync();
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-trigger");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _trigger.DisposeAsync();
        await _documentOrder.DisposeAsync();
        await _elementLabels.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
