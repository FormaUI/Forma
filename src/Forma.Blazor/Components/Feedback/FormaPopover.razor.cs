namespace Forma.Blazor.Components.Feedback;

using Forma.Blazor.Components.Interaction;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>An anchored popover shown in the browser's top layer, registered with the input router while open so Escape and a press away dismiss it and its content's routes are answered, and returning focus to its trigger after keyboard dismissal.</summary>
public sealed partial class FormaPopover : FormaElementBase
{
    /// <summary>The element the popover hangs from.</summary>
    [Parameter, EditorRequired] public ElementReference Anchor { get; set; }

    /// <summary>Whether Tab moves between the popover's own controls instead of leaving it.</summary>
    [Parameter] public bool KeepsFocus { get; set; }

    /// <summary>The inline edge of the anchor the popover lines up with.</summary>
    [Parameter] public PopoverAlignment Alignment { get; set; }

    /// <summary>Whether closing returns focus to the trigger when focus is inside the popover at the time. A press away never moves focus.</summary>
    [Parameter] public bool ReturnsFocus { get; set; } = true;

    /// <summary>Whether a press away from the popover and its trigger dismisses it; the press then belongs to whatever was pressed, so one press closes the popover and acts.</summary>
    [Parameter] public bool PressAway { get; set; } = true;

    /// <summary>The popover's content, rendered only while it is open.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised whenever the popover opens or closes, so a component drawing the trigger's expanded state redraws it.</summary>
    [Parameter] public EventCallback<bool> OnOpenChanged { get; set; }

    /// <summary>Raised once the popover is open and positioned, for content that takes focus or attaches a behavior of its own.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>The routes the popover's content answers while it is open.</summary>
    [Parameter] public IReadOnlyList<InputRoute> InputRoutes { get; set; } = [];

    /// <summary>Raised with each action the router resolves to the popover.</summary>
    [Parameter] public EventCallback<InputAction> OnInputAction { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private AnchoredPopover _popup = default!;
    private InputRegistration _inputRegistration = default!;
    private InputProfile _inputProfile = default!;
    private ElementReference _trigger;

    /// <summary>Whether the popover is open.</summary>
    public bool IsOpen { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _popup = new AnchoredPopover(Context.JS);
        _inputRegistration = new InputRegistration(InputRouter, HandleInputActionAsync, HandleDismissAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var profile = new InputProfile
        {
            Escape = DismissBehavior.Dismiss,
            PressAway = PressAway ? PressAwayBehavior.PassThrough : PressAwayBehavior.Ignore,
            InputRoutes = InputRoutes,
        };

        // The registration pushes a profile again only when it is another instance, so one is kept until a parameter changes it.
        if (_inputProfile != profile)
        {
            _inputProfile = profile;
        }
    }

    /// <summary>Opens the popover, remembering the trigger as where focus returns when it closes.</summary>
    public async Task OpenAsync(ElementReference trigger)
    {
        if (IsOpen)
        {
            return;
        }

        _trigger = trigger;
        await SetOpenAsync(true);
    }

    /// <summary>Closes the popover, returning focus to the trigger when <see cref="ReturnsFocus"/> and focus is inside it.</summary>
    public Task CloseAsync()
        => CloseAsync(ReturnsFocus);

    /// <summary>Opens the popover from the trigger, or closes it when it is open.</summary>
    public Task ToggleAsync(ElementReference trigger)
        => IsOpen ? CloseAsync() : OpenAsync(trigger);

    // Performs an action the router resolved to the popover's content.
    private Task HandleInputActionAsync(string action, InputInfo input)
        => OnInputAction.InvokeAsync(new InputAction(action, input));

    // Closes the popover as the router dismissed it: a press away leaves focus where it is, and anything else returns it to the trigger.
    private Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => action is DismissBehavior.Ignore
            ? Task.CompletedTask
            : CloseAsync(ReturnsFocus && reason is not DismissReason.PressAway);

    private async Task CloseAsync(bool returnsFocus)
    {
        if (!IsOpen)
        {
            return;
        }

        // The scope goes before the content does, so nothing is routed to a popover on its way out.
        await _inputRegistration.SyncAsync(RootElement, _inputProfile, wanted: false);

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

    // The popover is an active scope only while open, with its trigger counted as inside it so a press there toggles rather than dismisses.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _popup.SyncAsync(IsOpen, RootElement, Anchor, _trigger, async () => await OnOpened.InvokeAsync(), KeepsFocus, Alignment);
        await _inputRegistration.SyncAsync(RootElement, _inputProfile, wanted: IsOpen, active: IsOpen, within: [_trigger]);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-popover")
            .Add("forma-popover-open", IsOpen);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await _popup.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
