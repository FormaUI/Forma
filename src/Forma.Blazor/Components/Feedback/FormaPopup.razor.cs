namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>An anchored popup shown in the browser's top layer, registered with the input router while open so Escape and a press away dismiss it, and returning focus to its trigger after keyboard dismissal.</summary>
public sealed partial class FormaPopup : ComponentBase, IAsyncDisposable
{
    /// <summary>The element the popup hangs from.</summary>
    [Parameter, EditorRequired] public ElementReference Anchor { get; set; }

    /// <summary>Whether Tab moves between the popup's own controls instead of leaving it.</summary>
    [Parameter] public bool KeepsFocus { get; set; }

    /// <summary>Whether closing returns focus to the trigger when focus is inside the popup at the time. A press away never moves focus.</summary>
    [Parameter] public bool ReturnsFocus { get; set; } = true;

    /// <summary>Whether a press away from the popup and its trigger dismisses it.</summary>
    [Parameter] public bool PressAway { get; set; } = true;

    /// <summary>The popup's content, rendered only while it is open.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised whenever the popup opens or closes, so a component drawing the trigger's expanded state redraws it.</summary>
    [Parameter] public EventCallback<bool> OnOpenChanged { get; set; }

    /// <summary>Raised once the popup is open and positioned, for content that takes focus or attaches a behaviour of its own.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>Raised for every key pressed inside the popup other than Escape, which the router answers by dismissing it.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>Additional attributes applied to the popup element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private InputRouter Router { get; set; } = default!;

    private AnchoredPopup _popup = default!;
    private DotNetObjectReference<FormaPopup> _selfReference = default!;
    private InputRegistration? _registration;
    private InputProfile? _registeredProfile;
    private int _opening;
    private Task _sync = Task.CompletedTask;
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

    private InputProfile Profile
        => new()
        {
            Escape = DismissBehavior.Dismiss,
            PressAway = PressAway ? PressAwayBehavior.Dismiss : PressAwayBehavior.Ignore,
        };

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
        _opening++;
        await SetOpenAsync(true);
    }

    /// <summary>Closes the popup, returning focus to the trigger when <see cref="ReturnsFocus"/> and focus is inside it.</summary>
    public Task CloseAsync()
        => CloseAsync(ReturnsFocus);

    /// <summary>Opens the popup from the trigger, or closes it when it is open.</summary>
    public Task ToggleAsync(ElementReference trigger)
        => IsOpen ? CloseAsync() : OpenAsync(trigger);

    /// <summary>Closes the popup as the router dismissed it: a press away leaves focus where it is, and anything else returns it to the trigger.</summary>
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

    // Escape belongs to the router, which dismisses the popup wherever focus is.
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
        await _popup.SyncAsync(IsOpen, _element, Anchor, _trigger, async () => await OnOpened.InvokeAsync(), KeepsFocus);

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
        else if (_registration is null)
        {
            await RegisterAsync();
        }
        else if (_registeredProfile != Profile && await _registration.UpdateAsync(Profile))
        {
            _registeredProfile = Profile;
        }
    }

    // The popup is a scope only while open, with its trigger counted as inside it so a press there toggles rather than dismisses.
    private async Task RegisterAsync()
    {
        var opening = _opening;
        var profile = Profile;
        var registration = await Router.RegisterAsync(_element, profile, _selfReference, within: [_trigger]);

        if (registration is null)
        {
            return;
        }

        // The popup may have closed, or closed and reopened from another trigger, while the registration was being made.
        if (!IsOpen || opening != _opening)
        {
            await registration.DisposeAsync();
            return;
        }

        _registration = registration;
        _registeredProfile = profile;
        await registration.ActivateAsync();
    }

    private async Task ReleaseAsync()
    {
        if (_registration is { } registration)
        {
            _registration = null;
            _registeredProfile = null;
            await registration.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
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
    }
}
