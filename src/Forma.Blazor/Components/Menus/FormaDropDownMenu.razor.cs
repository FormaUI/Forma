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
using Microsoft.JSInterop;

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

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private readonly List<FormaDropDownMenuItem> _items = [];

    private bool _openAtLastItem;
    private int _focusedIndex = -1;
    private ElementReference _anchorElement;
    private FormaButton TriggerButton { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;
    private DotNetObjectReference<FormaDropDownMenu> _selfReference = default!;
    private InputScope? _triggerScope;

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
        => _selfReference = DotNetObjectReference.Create(this);

    // The trigger's keys are routes on the trigger, which is in the document whether or not the menu is; the menu's ride on the popover.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _triggerScope = await InputRouter.RegisterUnlessDisposedAsync(TriggerButton.RootElement, TriggerProfile, _selfReference, () => IsDisposed);
        }
    }

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <summary>Closes the menu and returns focus to the trigger, for an item that has just been activated.</summary>
    internal Task RequestActivateAsync()
        => Popover.CloseAsync();

    /// <summary>Opens the menu, or moves through it, as the router matched the key.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return action switch
        {
            OpenFirstAction when !IsOpen => OpenAsync(atLastItem: false),
            OpenLastAction when !IsOpen => OpenAsync(atLastItem: true),
            NextAction => MoveFocusAsync(1),
            PreviousAction => MoveFocusAsync(-1),
            FirstAction => FocusIndexAsync(FindEnabledIndex(0, 1)),
            LastAction => FocusIndexAsync(FindEnabledIndex(_items.Count - 1, -1)),
            _ => Task.CompletedTask,
        };
    }

    private Task HandleMenuActionAsync(InputAction action)
        => HandleInputActionAsync(action.Action, action.Input);

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

    private Task HandleOpenedAsync()
        => FocusIndexAsync(_openAtLastItem ? FindEnabledIndex(_items.Count - 1, -1) : FindEnabledIndex(0, 1));

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
        try
        {
            if (_triggerScope is { } triggerScope)
            {
                _triggerScope = null;
                await triggerScope.DisposeAsync();
            }
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
