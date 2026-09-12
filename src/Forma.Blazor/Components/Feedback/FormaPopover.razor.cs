namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Common.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>An anchored popover shown in the browser's top layer, registered with the input router while open so Escape and a press away dismiss it, and returning focus to its trigger after keyboard dismissal.</summary>
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

    /// <summary>Raised for every key pressed inside the popover other than Escape, which the router answers by dismissing it.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private AnchoredPopover _popup = default!;
    private DotNetObjectReference<FormaPopover> _selfReference = default!;
    private InputScope? _inputScope;
    private InputProfile? _registeredProfile;
    private int _opening;
    private Task _sync = Task.CompletedTask;
    private ElementReference _trigger;

    /// <summary>Whether the popover is open.</summary>
    public bool IsOpen { get; private set; }

    private InputProfile Profile
        => new()
        {
            Escape = DismissBehavior.Dismiss,
            PressAway = PressAway ? PressAwayBehavior.PassThrough : PressAwayBehavior.Ignore,
        };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _popup = new AnchoredPopover(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <summary>Opens the popover, remembering the trigger as where focus returns when it closes.</summary>
    public async Task OpenAsync(ElementReference trigger)
    {
        if (IsOpen)
        {
            return;
        }

        _trigger = trigger;
        _opening++;
        await SetOpenAsync(true);
    }

    /// <summary>Closes the popover, returning focus to the trigger when <see cref="ReturnsFocus"/> and focus is inside it.</summary>
    public Task CloseAsync()
        => CloseAsync(ReturnsFocus);

    /// <summary>Opens the popover from the trigger, or closes it when it is open.</summary>
    public Task ToggleAsync(ElementReference trigger)
        => IsOpen ? CloseAsync() : OpenAsync(trigger);

    /// <summary>Closes the popover as the router dismissed it: a press away leaves focus where it is, and anything else returns it to the trigger.</summary>
    [JSInvokable]
    public Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => action is DismissBehavior.Ignore
            ? Task.CompletedTask
            : CloseAsync(ReturnsFocus && reason is not DismissReason.PressAway);

    private async Task CloseAsync(bool returnsFocus)
    {
        if (!IsOpen)
        {
            return;
        }

        await ReleaseAsync();

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

    // Escape belongs to the router, which dismisses the popover wherever focus is.
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key != "Escape")
        {
            await OnKeyDown.InvokeAsync(args);
        }
    }

    // Renders can overlap, so each sync waits for the one before it and none registers twice.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _popup.SyncAsync(IsOpen, RootElement, Anchor, _trigger, async () => await OnOpened.InvokeAsync(), KeepsFocus, Alignment);

        var previous = _sync;
        _sync = SyncRegistrationAsync(previous);
        await _sync;
    }

    private async Task SyncRegistrationAsync(Task previous)
    {
        await previous;

        if (!IsOpen)
        {
            await ReleaseAsync();
        }
        else if (_inputScope is null)
        {
            await RegisterAsync();
        }
        else if (_registeredProfile != Profile && await _inputScope.UpdateAsync(Profile))
        {
            _registeredProfile = Profile;
        }
    }

    // The popover is a scope only while open, with its trigger counted as inside it so a press there toggles rather than dismisses.
    private async Task RegisterAsync()
    {
        var opening = _opening;
        var profile = Profile;
        var inputScope = await InputRouter.RegisterAsync(RootElement, profile, _selfReference, within: [_trigger]);

        if (inputScope is null)
        {
            return;
        }

        // The popover may have closed, or closed and reopened from another trigger, while the registration was being made.
        if (!IsOpen || opening != _opening)
        {
            await inputScope.DisposeAsync();
            return;
        }

        _inputScope = inputScope;
        _registeredProfile = profile;
        await inputScope.ActivateAsync();
    }

    private async Task ReleaseAsync()
    {
        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            _registeredProfile = null;
            await inputScope.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-popover")
            .Add("forma-popover-open", IsOpen);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await ReleaseAsync();
            await _popup.DisposeAsync();
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
