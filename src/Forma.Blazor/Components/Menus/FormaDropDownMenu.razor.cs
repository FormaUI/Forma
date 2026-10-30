namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Framework.Browser.Interop.Behaviors;
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

    private readonly List<IDropDownMenuEntry> _entries = [];

    private bool _openAtLastItem;
    private int _focusedIndex = -1;
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
        => _trigger = new InputRegistration(InputRouter, HandleActionAsync);

    // The trigger's keys route on the trigger, and the menu's on the popover.
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _trigger.SyncAsync(TriggerButton.RootElement, TriggerProfile);

    // An entry registers as it is first rendered, which places a group among its siblings before its own items render.
    internal void RegisterEntry(IDropDownMenuEntry entry)
        => _entries.Add(entry);

    internal void UnregisterEntry(IDropDownMenuEntry entry)
        => _entries.Remove(entry);

    // Every item in the order the menu shows it, a group's in the group's place.
    private List<FormaDropDownMenuItem> GetItems()
        => [.. _entries.SelectMany(entry => entry.Items)];

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
            FirstAction => FocusIndexAsync(FindEnabledIndex(0, 1)),
            LastAction => FocusIndexAsync(FindEnabledIndex(GetItems().Count - 1, -1)),
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
        => FocusIndexAsync(_openAtLastItem ? FindEnabledIndex(GetItems().Count - 1, -1) : FindEnabledIndex(0, 1));

    private Task MoveFocusAsync(int delta)
        => FocusIndexAsync(FindEnabledIndex(_focusedIndex + delta, delta));

    /// <summary>Finds the index of the first enabled item at or past <paramref name="start"/> in the direction of <paramref name="step"/>, wrapping around the ends.</summary>
    private int? FindEnabledIndex(int start, int step)
    {
        var items = GetItems();

        for (var visited = 0; visited < items.Count; visited++)
        {
            var index = ((start + visited * step) % items.Count + items.Count) % items.Count;

            if (!items[index].Disabled)
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
        await GetItems()[_focusedIndex].FocusAsync();
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-trigger");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _trigger.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
