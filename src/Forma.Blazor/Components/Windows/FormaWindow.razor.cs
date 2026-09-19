namespace Forma.Blazor.Components.Windows;

using Forma.Blazor.Components.Layout;
using Forma.Framework.Browser.Interop.Components;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Toolbar;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A chrome floated into the top layer, whose frame moves, resizes and maximizes the window. It is a window of its workspace: activated, stacked, minimized and closed with the windows around it and the windows it opened, and blocking them while modal.</summary>
public sealed partial class FormaWindow : FormaElementBase, IManagedWindow
{
    private const string CloseCommand = "window.close";
    private const string MaximizeCommand = "window.maximize";
    private const string MinimizeCommand = "window.minimize";
    private const string LockCommand = "window.lock";

    private ElementReference _scrim;
    private Window _window = default!;
    private DotNetObjectReference<FormaWindow> _selfReference = default!;
    private InputScope? _inputScope;
    private (bool Modal, WindowPressAway PressAway)? _scopeInputs;
    private WindowAttachment? _attachment;
    private WindowOrigin _origin = default!;
    private WindowDeclaration? _declaration;
    private bool _isHidden;
    private bool _isBlocked;
    private bool _isApplying;

    [Inject] private InputRouter InputRouter { get; set; } = default!;
    [Inject] private WindowManager InjectedManager { get; set; } = default!;

    [CascadingParameter] private WindowManager? CascadedManager { get; set; }
    [CascadingParameter] private WindowOrigin? CascadedOrigin { get; set; }

    /// <summary>The title shown in the title bar and read as the window's name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The application or feature instance the window belongs to; unset, it is the parent's, else the cascaded one, else the workspace's own.</summary>
    [Parameter] public WindowOrigin? Origin { get; set; }

    /// <summary>The window that opened this one; it closes and minimizes with it.</summary>
    [Parameter] public IWindow? Parent { get; set; }

    /// <summary>Where the window stands and how large it is; unset, it opens centered at its default size.</summary>
    [Parameter] public FramePlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes the window, and once when it first opens without a placement.</summary>
    [Parameter] public EventCallback<FramePlacement> PlacementChanged { get; set; }

    /// <summary>Whether the window fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Raised when the reader maximizes or restores the window from its title bar.</summary>
    [Parameter] public EventCallback<bool> MaximizedChanged { get; set; }

    /// <summary>Whether the window is held where it is: neither moved, resized nor maximized.</summary>
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

    /// <summary>Raised before the window closes, on its own or with a window above it; setting <see cref="WindowClosingEventArgs.Cancel"/> keeps every window of that close open.</summary>
    [Parameter] public EventCallback<WindowClosingEventArgs> OnClosing { get; set; }

    /// <summary>Raised once the window closes, from its title bar, with Escape, with its parent, or through its workspace.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>The band beneath the title bar, generally text.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The window's content.</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>The band at the foot, generally actions or text.</summary>
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

    // The window is a scope while attached, active while it is the workspace's current window and focus is inside it.
    // Escape closes the focused window and nothing else; a modal one bars everything beneath it; a press away reaches
    // the window only when it is to send the window back.
    private InputProfile Profile
        => new()
        {
            Modal = Modal,
            Escape = DismissBehavior.Dismiss,
            CloseRequest = DismissBehavior.Dismiss,
            PressAway = !Modal && Options.PressAway is WindowPressAway.SendToBack ? PressAwayBehavior.PassThrough : PressAwayBehavior.Ignore,
        };

    // What the profile is built from, so a profile is sent again only when one of them changed.
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

    /// <summary>Moves this window to the front of the stacking order without activating it.</summary>
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
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

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
            await _window.DetachAsync();
            await ReleaseScopeAsync();

            var attached = await _window.AttachAsync(RootElement, IsPresented, Modal, ShowsScrim ? _scrim : null);

            if (!attached)
            {
                return;
            }

            await RegisterScopeAsync();

            // A window that opens is the one the reader is working in.
            if (!Active && !Disabled && IsPresented)
            {
                await ActivateAsync();
            }

            return;
        }

        if (_inputScope is { } inputScope && ScopeInputs != _scopeInputs)
        {
            _scopeInputs = ScopeInputs;
            await inputScope.UpdateAsync(Profile);
        }
    }

    /// <summary>Answers the router's dismissal: a press away sends the window back when its options say so, and anything else closes it when it is closable.</summary>
    [JSInvokable]
    public async Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
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
    ValueTask IManagedWindow.SetActiveAsync(bool active)
        => new(InvokeAsync(async () =>
        {
            if (Active != active)
            {
                Active = active;
                await RaiseAsync(ActiveChanged, active);
            }

            if (active)
            {
                if (_inputScope is { } inputScope)
                {
                    await inputScope.ActivateAsync();
                }

                await _window.FocusAsync();
            }
            else if (_inputScope is { } inputScope)
            {
                await inputScope.ReleaseAsync();
            }

            StateHasChanged();
        }));

    ValueTask IManagedWindow.SetMinimizedAsync(bool minimized)
        => new(InvokeAsync(async () =>
        {
            if (Minimized != minimized)
            {
                Minimized = minimized;
                await RaiseAsync(MinimizedChanged, minimized);
            }

            await PresentAsync();
        }));

    ValueTask IManagedWindow.SetHiddenAsync(bool hidden)
        => new(InvokeAsync(() =>
        {
            _isHidden = hidden;

            return PresentAsync();
        }));

    ValueTask IManagedWindow.SetBlockedAsync(bool blocked)
        => new(InvokeAsync(async () =>
        {
            _isBlocked = blocked;

            if (blocked && _inputScope is { } inputScope)
            {
                await inputScope.ReleaseAsync();
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

    private async Task SetMaximizedAsync(bool maximized)
    {
        Maximized = maximized;
        await MaximizedChanged.InvokeAsync(maximized);
    }

    private async Task SetLockedAsync(bool locked)
    {
        Locked = locked;
        await LockedChanged.InvokeAsync(locked);
    }

    // A press anywhere inside makes the window current, whether or not what was pressed can take focus; one already current comes forward from wherever it was sent.
    private async Task HandlePointerDownAsync()
    {
        if (IsInert)
        {
            return;
        }

        if (!Active)
        {
            await ActivateAsync();
        }
        else
        {
            await BringToFrontAsync();
        }
    }

    // Focus arriving makes the window current; focus already current only re-arms its scope.
    private async Task HandleFocusInAsync()
    {
        if (!Active)
        {
            await ActivateAsync();
        }
        else if (_inputScope is { } inputScope)
        {
            await inputScope.ActivateAsync();
        }
    }

    private Task HandleFocusOutAsync()
        => _inputScope is { } inputScope ? inputScope.ReleaseAsync().AsTask() : Task.CompletedTask;

    private WindowDeclaration Declare()
        => new(Title, Icon, Parent?.RuntimeId, Origin, CascadedOrigin, Minimized, Disabled, Modal);

    private WindowOrigin ResolveOrigin()
        => Origin ?? Parent?.Origin ?? CascadedOrigin ?? Manager.Origin;

    // A change the workspace makes is reported through the callback with the refresh held off, since the workspace already knows.
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
        StateHasChanged();

        if (_attachment is not null)
        {
            await _window.PresentAsync(IsPresented);
        }

        if (!IsPresented && _inputScope is { } inputScope)
        {
            await inputScope.ReleaseAsync();
        }
    }

    private async Task RegisterScopeAsync()
    {
        var inputs = ScopeInputs;
        var inputScope = await InputRouter.RegisterAsync(RootElement, Profile, _selfReference);

        if (inputScope is null)
        {
            return;
        }

        // Another pass may have registered the window while the registration was being made.
        if (_inputScope is not null)
        {
            await inputScope.DisposeAsync();
            return;
        }

        _inputScope = inputScope;
        _scopeInputs = inputs;
    }

    private async Task ReleaseScopeAsync()
    {
        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            _scopeInputs = null;
            await inputScope.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-window")
            .Add("is-active", Active)
            .Add("is-maximized", Maximized)
            .Add("has-placement", Placement is not null);

    // Viewport coordinates from the pointer are physical, so the placement is written as left and top rather than
    // as logical insets.
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
        if (_declaration is not null)
        {
            await Manager.UnregisterAsync(this);
        }

        await ReleaseScopeAsync();
        await _window.DisposeAsync();
        _selfReference.Dispose();
        await base.DisposeAsyncCore();
    }
}
