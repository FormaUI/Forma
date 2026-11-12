namespace Forma.Blazor.Components.Windows;

using Forma.Blazor.Components.Layout;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Components;
using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Toolbar;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A workspace window: a chrome floated into the top layer, whose frame moves, resizes and maximizes the window.</summary>
public sealed partial class FormaWindow : FormaElementBase, IManagedWindow
{
    private const string CloseCommand = "window.close";
    private const string MaximizeCommand = "window.maximize";
    private const string MinimizeCommand = "window.minimize";
    private const string LockCommand = "window.lock";

    private static readonly FocusWatchOptions FocusOptions = new();

    private ElementReference _scrim;
    private Window _window = default!;
    private DotNetObjectReference<FormaWindow>? _selfReference;
    private DomObservation<FocusWatchOptions, FocusState>? _focusState;
    private InputRegistration _inputRegistration = default!;
    private InputProfile _inputProfile = default!;
    private (bool Modal, WindowPressAway PressAway)? _scopeInputs;
    private WindowAttachment? _attachment;
    private bool _isAttached;
    private bool _holdsInput;
    private WindowOrigin _origin = default!;
    private WindowDeclaration? _declaration;
    private bool _isHidden;
    private bool _isBlocked;
    private bool _isApplying;

    [Inject] private InputRouter InputRouter { get; set; } = default!;
    [Inject] private WindowManager InjectedManager { get; set; } = default!;

    [CascadingParameter] private WindowManager? CascadedManager { get; set; }
    [CascadingParameter] private WindowOrigin? CascadedOrigin { get; set; }

    /// <inheritdoc />
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <inheritdoc />
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The application or feature instance the window belongs to, where null takes the parent's, the cascaded one or the workspace's own.</summary>
    [Parameter] public WindowOrigin? Origin { get; set; }

    /// <inheritdoc />
    [Parameter] public IWindow? Parent { get; set; }

    /// <summary>Where the window stands and how large it is, where null opens it centered at its default size.</summary>
    [Parameter] public FramePlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes the window, and once when it first opens without a placement.</summary>
    [Parameter] public EventCallback<FramePlacement> PlacementChanged { get; set; }

    /// <summary>Whether the window fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Raised when the reader maximizes or restores the window from its title bar.</summary>
    [Parameter] public EventCallback<bool> MaximizedChanged { get; set; }

    /// <summary>Whether the window is held at its placement, size and maximization.</summary>
    [Parameter] public bool Locked { get; set; }

    /// <summary>Raised when the reader locks or unlocks the window from its title bar.</summary>
    [Parameter] public EventCallback<bool> LockedChanged { get; set; }

    /// <summary>Whether the window is minimized: out of its presentation, its content kept, its descendants hidden with it.</summary>
    [Parameter] public bool Minimized { get; set; }

    /// <summary>Raised when the window is minimized or restored.</summary>
    [Parameter] public EventCallback<bool> MinimizedChanged { get; set; }

    /// <summary>Whether this is the workspace's current window.</summary>
    [Parameter] public bool Active { get; set; }

    /// <summary>Raised when the window becomes the current one or stops being so.</summary>
    [Parameter] public EventCallback<bool> ActiveChanged { get; set; }

    /// <summary>Whether the window blocks its parent's windows, or the whole workspace as a root, while it is shown.</summary>
    [Parameter] public bool Modal { get; set; }

    /// <summary>Whether the window is disabled: it takes no input and reads as inactive.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The host's own commands, shown in the title bar before the window's.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Commands { get; set; } = [];

    /// <summary>Raised with the command of a host item pressed in the title bar.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>What the reader may do to the window from its title bar, and how it answers a press outside it.</summary>
    [Parameter] public WindowOptions Options { get; set; } = WindowOptions.Default;

    /// <summary>What the reader may do to the window's frame, how far, and where it is held.</summary>
    [Parameter] public FrameOptions FrameOptions { get; set; } = FrameOptions.Default;

    /// <summary>Raised before the window closes, on its own or with a window above it.</summary>
    /// <remarks>Setting <see cref="WindowClosingEventArgs.Cancel"/> keeps every window of that close open.</remarks>
    [Parameter] public EventCallback<WindowClosingEventArgs> OnClosing { get; set; }

    /// <summary>Raised once the window closes.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>The band beneath the title bar.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The window's content.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>The band at the foot of the window.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>The workspace the window belongs to: the one cascaded to it, else the circuit's.</summary>
    public WindowManager Manager
        => CascadedManager ?? InjectedManager;

    Guid IWindow.RuntimeId
        => Context.RuntimeId;

    WindowOrigin IWindow.Origin
        => _origin;

    bool IWindow.IsMinimized
        => Minimized;

    bool IWindow.IsDisabled
        => Disabled;

    bool IWindow.IsModal
        => Modal;

    bool IWindow.IsFloating
        => true;

    private bool IsPresented
        => !Minimized && !_isHidden;

    private bool IsInert
        => Disabled || _isBlocked;

    // A root modal blocks the whole workspace, which a scrim beneath it makes visible; a child modal blocks its parent alone.
    private bool ShowsScrim
        => Modal && Parent is null;

    // Escape closes the focused window alone; a modal one bars everything beneath it; a press away reaches the window only to send it back.
    private InputProfile Profile
        => new()
        {
            Modal = Modal,
            Escape = DismissBehavior.Dismiss,
            CloseRequest = DismissBehavior.Dismiss,
            PressAway = !Modal && Options.PressAway is WindowPressAway.SendToBack ? PressAwayBehavior.PassThrough : PressAwayBehavior.Ignore,
        };

    // What the profile is built from, so a profile is built and sent again only when one of them changed.
    private (bool Modal, WindowPressAway PressAway) ScopeInputs
        => (Modal, Options.PressAway);

    // The host's commands, then the window's own: lock, minimize, maximize, close.
    private IReadOnlyList<ToolbarItem> TitleBarItems
    {
        get
        {
            var items = new List<ToolbarItem>(Commands.Count + 4);
            items.AddRange(Commands);

            if (Options.Lockable)
            {
                items.Add(new ToolbarToggle
                {
                    Id = LockCommand,
                    Command = LockCommand,
                    Title = L["Lock"],
                    Icon = Glyph.LockOpen,
                    IsActive = Locked,
                    AlternateTitle = L["Unlock"],
                    AlternateIcon = Glyph.Lock,
                });
            }

            if (Options.Minimizable)
            {
                items.Add(new ToolbarCommand
                {
                    Id = MinimizeCommand,
                    Command = MinimizeCommand,
                    Title = L["Minimize"],
                    Icon = Glyph.WindowMinimize,
                });
            }

            if (Options.Maximizable)
            {
                items.Add(new ToolbarToggle
                {
                    Id = MaximizeCommand,
                    Command = MaximizeCommand,
                    Title = L["Maximize"],
                    Icon = Glyph.Maximize,
                    IsActive = Maximized,
                    IsDisabled = Locked,
                    AlternateTitle = L["Restore"],
                    AlternateIcon = Glyph.Minimize,
                });
            }

            if (Options.Closable)
            {
                items.Add(new ToolbarCommand
                {
                    Id = CloseCommand,
                    Command = CloseCommand,
                    Title = L["Close"],
                    Icon = Glyph.Cross,
                });
            }

            return items;
        }
    }

    /// <summary>Makes this window current, or exactly one window among its descendants when <paramref name="selector"/> is given.</summary>
    public ValueTask<WindowOperationResult> ActivateAsync(IWindowSelector? selector = null)
        => Manager.ActivateAsync(Manager.Within(this, selector));

    /// <summary>Minimizes this window, or the windows among its descendants that <paramref name="selector"/> describes.</summary>
    public ValueTask<WindowOperationResult> MinimizeAsync(IWindowSelector? selector = null)
        => Manager.MinimizeAsync(Manager.Within(this, selector));

    /// <summary>Restores this window from minimization, or the windows among its descendants that <paramref name="selector"/> describes.</summary>
    public ValueTask<WindowOperationResult> RestoreAsync(IWindowSelector? selector = null)
        => Manager.RestoreAsync(Manager.Within(this, selector));

    /// <summary>Closes this window and its descendants, or the windows among its descendants that <paramref name="selector"/> describes, once every window affected has agreed.</summary>
    public ValueTask<WindowOperationResult> CloseAsync(IWindowSelector? selector = null)
        => Manager.CloseAsync(Manager.Within(this, selector));

    /// <summary>Moves this window to the front of the stacking order, leaving the current window as it is.</summary>
    public ValueTask<WindowOperationResult> BringToFrontAsync()
        => Manager.BringToFrontAsync(Manager.Within(this));

    /// <summary>Moves this window to the back of the stacking order.</summary>
    public ValueTask<WindowOperationResult> SendToBackAsync()
        => Manager.SendToBackAsync(Manager.Within(this));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _window = new Window(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
        _inputRegistration = new InputRegistration(InputRouter, onDismiss: HandleDismissAsync);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        if (ScopeInputs != _scopeInputs)
        {
            _scopeInputs = ScopeInputs;
            _inputProfile = Profile;
        }

        var declaration = Declare();

        if (_declaration is null)
        {
            _declaration = declaration;
            _origin = ResolveOrigin();
            await Manager.RegisterAsync(this);
        }
        else if (!_isApplying && declaration != _declaration)
        {
            _declaration = declaration;
            _origin = ResolveOrigin();
            await Manager.RefreshAsync(this);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var attachment = new WindowAttachment(Modal, ShowsScrim);

        if (_attachment != attachment)
        {
            _attachment = attachment;
            _isAttached = false;
            await _window.DetachAsync();

            // The scope goes with the attachment it was made for and comes back with the next.
            await _inputRegistration.SyncAsync(RootElement, _inputProfile, shouldRegister: false);
            _isAttached = await _window.AttachAsync(RootElement, IsPresented, Modal, ShowsScrim ? _scrim : null, _selfReference!);

            // A window that opens becomes the current one.
            if (_isAttached && !Active && !Disabled && IsPresented)
            {
                await ActivateWhileVisibleAsync();
            }
        }

        // Taken away while it attached, it has no context left to watch its focus through.
        if (IsDisposed)
        {
            return;
        }

        if (firstRender)
        {
            _focusState = await Context.DomWatcher.GetElement(RootElement).Focus.WatchAsync(HandleFocusStateAsync);
        }

        if (_focusState is { } focusState)
        {
            await focusState.SyncAsync(RootElement, FocusOptions);
        }

        // The window is a scope while attached, held active while it is the workspace's current window and focus is inside it.
        await _inputRegistration.SyncAsync(RootElement, _inputProfile, shouldRegister: _isAttached, isActive: _holdsInput);
    }

    // Answers the router's dismissal: a press away sends the window back when its options say so, and anything else closes it when it is closable.
    private async Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
    {
        if (action is not DismissBehavior.Dismiss)
        {
            return;
        }

        if (reason is DismissReason.PressAway)
        {
            if (!Modal && Options.PressAway is WindowPressAway.SendToBack)
            {
                await SendToBackAsync();
            }

            return;
        }

        if (Options.Closable)
        {
            await CloseAsync();
        }
    }

    // The workspace calls in from off the renderer, so each of its requests is carried onto the dispatcher first.
    ValueTask IManagedWindow.SetActiveAsync(bool isActive)
        => new(InvokeAsync(async () =>
        {
            if (Active != isActive)
            {
                Active = isActive;
                await RaiseAsync(ActiveChanged, isActive);
            }

            _holdsInput = isActive;

            if (isActive)
            {
                await _window.FocusAsync();
            }

            StateHasChanged();
        }));

    ValueTask IManagedWindow.SetMinimizedAsync(bool isMinimized)
        => new(InvokeAsync(async () =>
        {
            if (Minimized != isMinimized)
            {
                Minimized = isMinimized;
                await RaiseAsync(MinimizedChanged, isMinimized);
            }

            await PresentAsync();
        }));

    ValueTask IManagedWindow.SetHiddenAsync(bool isHidden)
        => new(InvokeAsync(() =>
        {
            _isHidden = isHidden;

            return PresentAsync();
        }));

    ValueTask IManagedWindow.SetBlockedAsync(bool isBlocked)
        => new(InvokeAsync(() =>
        {
            _isBlocked = isBlocked;

            if (isBlocked)
            {
                _holdsInput = false;
            }

            StateHasChanged();
        }));

    ValueTask IManagedWindow.RaiseAsync()
        => IsPresented ? new(InvokeAsync(() => _window.RaiseAsync().AsTask())) : ValueTask.CompletedTask;

    async ValueTask<bool> IManagedWindow.RequestCloseAsync()
    {
        var args = new WindowClosingEventArgs();
        await InvokeAsync(() => OnClosing.InvokeAsync(args));

        return !args.Cancel;
    }

    ValueTask IManagedWindow.PerformCloseAsync()
        => new(InvokeAsync(OnClose.InvokeAsync));

    private async Task HandleCommandAsync(string command)
    {
        switch (command)
        {
            case CloseCommand:
                await CloseAsync();
                break;
            case MinimizeCommand:
                await MinimizeAsync();
                break;
            case MaximizeCommand:
                await SetMaximizedAsync(!Maximized);
                break;
            case LockCommand:
                await SetLockedAsync(!Locked);
                break;
            default:
                await OnCommand.InvokeAsync(command);
                break;
        }
    }

    // The frame reports where the window came to rest, after a drag, a keyboard step, or the first measurement.
    private async Task HandlePlacementChangedAsync(FramePlacement placement)
    {
        if (placement == Placement)
        {
            return;
        }

        Placement = placement;
        await PlacementChanged.InvokeAsync(placement);
    }

    private async Task SetMaximizedAsync(bool isMaximized)
    {
        Maximized = isMaximized;
        await MaximizedChanged.InvokeAsync(isMaximized);
    }

    private async Task SetLockedAsync(bool isLocked)
    {
        Locked = isLocked;
        await LockedChanged.InvokeAsync(isLocked);
    }

    /// <summary>Makes the window current, or brings the current one forward, as the reader presses anywhere inside it.</summary>
    [JSInvokable]
    public async Task HandlePressAsync()
    {
        if (IsInert)
        {
            return;
        }

        if (!Active)
        {
            await ActivateWhileVisibleAsync();
        }
        else
        {
            await BringToFrontAsync();
        }
    }

    // Focus arriving makes the window current; focus already current only re-arms its scope, and focus leaving lets the scope go.
    private Task HandleFocusStateAsync(FocusState state)
        => InvokeAsync(async () =>
        {
            if (!state.IsWithin)
            {
                _holdsInput = false;
            }
            else if (!Active)
            {
                await ActivateWhileVisibleAsync();
            }
            else
            {
                _holdsInput = true;
            }

            StateHasChanged();
        });

    // Opening, a press and the focus arriving each report the window when it was shown, and a minimize may apply before the activation does.
    private ValueTask<WindowOperationResult> ActivateWhileVisibleAsync()
        => Manager.ActivateVisibleAsync(Manager.Within(this));

    private WindowDeclaration Declare()
        => new(Title, Icon, Parent?.RuntimeId, Origin, CascadedOrigin, Minimized, Disabled, Modal);

    private WindowOrigin ResolveOrigin()
        => Origin ?? Parent?.Origin ?? CascadedOrigin ?? Manager.Origin;

    // A change the workspace makes is reported through the callback with the refresh held off.
    private async Task RaiseAsync<TValue>(EventCallback<TValue> callback, TValue value)
    {
        _isApplying = true;

        try
        {
            await callback.InvokeAsync(value);
        }
        finally
        {
            _isApplying = false;
        }
    }

    // The render that takes the hidden attribute off reaches the browser before the script is told to show the window, so the box it measures is the window's own.
    private async Task PresentAsync()
    {
        if (!IsPresented)
        {
            _holdsInput = false;
        }

        StateHasChanged();

        if (_attachment is not null)
        {
            await _window.PresentAsync(IsPresented);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-window")
            .Add("is-active", Active)
            .Add("is-maximized", Maximized)
            .Add("has-placement", Placement is not null);

    // The placement is in physical viewport coordinates, so it is written as left and top.
    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (Maximized || Placement is not { } placement)
        {
            return;
        }

        builder
            .Add("left", CssLength.FromPx(placement.Left))
            .Add("top", CssLength.FromPx(placement.Top))
            .Add("width", CssLength.FromPx(placement.Width))
            .Add("height", CssLength.FromPx(placement.Height));
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        // Releasing the script first ends the window's calls in flight, which a workspace operation the unregister waits for may await.
        await _window.DisposeAsync();

        if (_declaration is not null)
        {
            await Manager.UnregisterAsync(this);
        }

        if (_focusState is { } focusState)
        {
            await focusState.DisposeAsync();
        }

        await _inputRegistration.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
