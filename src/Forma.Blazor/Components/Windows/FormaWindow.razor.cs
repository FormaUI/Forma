namespace Forma.Blazor.Components.Windows;

using System.Globalization;

using Forma.Framework.Common.Interop.Components;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A raised frame with chrome: a title bar carrying the title, an icon and commands, then an optional header band, the body, and an optional footer band. It stands docked in its parent's layout or floats over the page, where the reader moves it by its title bar, resizes it at its edges, and maximizes it to its bounds. It is a window of its workspace: activated, stacked, minimized and closed with the windows around it and the windows it opened, and blocking them while modal.</summary>
public sealed partial class FormaWindow : ComponentBase, IManagedWindow, IAsyncDisposable
{
    /// <summary>The class the resize handles carry, one per edge and corner.</summary>
    public const string HandleClass = "forma-window-handle";

    private const string TitleBarClass = "forma-window-titlebar";
    private const string CloseCommand = "window.close";
    private const string MaximizeCommand = "window.maximize";
    private const string MinimizeCommand = "window.minimize";
    private const string DockCommand = "window.dock";

    /// <summary>How far one arrow press moves or resizes a floating window, in CSS pixels: one spacing step of four.</summary>
    private const double StepPixels = 16;

    private static readonly string[] Edges = ["n", "s", "e", "w", "ne", "nw", "se", "sw"];

    private readonly string _titleId = $"forma-window-title-{Guid.NewGuid():N}";
    private ElementReference _element;
    private ElementReference _backdrop;
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
    private bool _renderedMaximized;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocale<FormaWindow> L { get; set; } = default!;
    [Inject] private InputRouter InputRouter { get; set; } = default!;
    [Inject] private WindowManager InjectedManager { get; set; } = default!;

    [CascadingParameter] private WindowManager? CascadedManager { get; set; }
    [CascadingParameter] private WindowOrigin? CascadedOrigin { get; set; }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The title shown in the title bar and read as the window's name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The application or feature instance the window belongs to; unset, it is the parent's, else the cascaded one, else the workspace's own.</summary>
    [Parameter] public WindowOrigin? Origin { get; set; }

    /// <summary>The window that opened this one; it closes and minimizes with it.</summary>
    [Parameter] public IWindow? Parent { get; set; }

    /// <summary>Whether the window is docked in its parent's layout or floats over the page.</summary>
    [Parameter] public WindowMode Mode { get; set; } = WindowMode.Docked;

    /// <summary>Raised when the reader docks or undocks the window from its title bar.</summary>
    [Parameter] public EventCallback<WindowMode> ModeChanged { get; set; }

    /// <summary>Where a floating window stands and how large it is; unset, it opens centered at its default size.</summary>
    [Parameter] public WindowPlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes a floating window, and once when it first floats without a placement.</summary>
    [Parameter] public EventCallback<WindowPlacement> PlacementChanged { get; set; }

    /// <summary>Whether a floating window fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Raised when the reader maximizes or restores the window from its title bar.</summary>
    [Parameter] public EventCallback<bool> MaximizedChanged { get; set; }

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

    /// <summary>Whether the window refuses interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The host's own commands, shown in the title bar before the window's.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Commands { get; set; } = [];

    /// <summary>Raised with the command of a host item pressed in the title bar.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>What the reader may do to the window from its chrome, and where it is held.</summary>
    [Parameter] public WindowOptions Options { get; set; } = WindowOptions.Default;

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

    /// <summary>Additional attributes applied to the window element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The workspace the window belongs to: the one cascaded to it, else the circuit's.</summary>
    public WindowManager Manager
        => CascadedManager ?? InjectedManager;

    WindowOrigin IWindow.Origin
        => _origin;

    bool IWindow.IsMinimized
        => Minimized;

    bool IWindow.IsDisabled
        => Disabled;

    bool IWindow.IsModal
        => Modal;

    bool IWindow.IsFloating
        => IsFloating;

    private bool IsFloating
        => Mode is WindowMode.Floating;

    private bool IsMaximized
        => IsFloating && Maximized;

    private bool IsPresented
        => !Minimized && !_isHidden;

    private bool IsInert
        => Disabled || _isBlocked;

    private bool ShowsHandles
        => IsFloating && Options.Resizable && !Maximized;

    // A root modal blocks the whole workspace, which a backdrop beneath it makes visible; a child modal blocks its parent alone.
    private bool ShowsBackdrop
        => IsFloating && Modal && Parent is null;

    private string Role
        => IsFloating ? "dialog" : "region";

    private string? AriaModal
        => IsFloating ? (Modal ? "true" : "false") : null;

    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-window",
            IsFloating ? "forma-window-floating" : "forma-window-docked",
            Active ? "is-active" : null,
            IsMaximized ? "is-maximized" : null,
            IsFloating && Placement is not null ? "has-placement" : null);

    private string TitleBarCssClass
        => IsFloating ? $"{TitleBarClass} is-movable" : TitleBarClass;

    private bool TakesArrowKeys
        => IsFloating && !Maximized;

    private string? TitleBarLabel
        => !TakesArrowKeys ? null
            : Options.Resizable ? L["Move {Window} with the arrow keys; hold Shift to resize it", Title]
            : L["Move {Window} with the arrow keys", Title];

    private string? TitleBarKeyShortcuts
        => !TakesArrowKeys ? null
            : Options.Resizable ? "ArrowUp ArrowDown ArrowLeft ArrowRight Shift+ArrowUp Shift+ArrowDown Shift+ArrowLeft Shift+ArrowRight"
            : "ArrowUp ArrowDown ArrowLeft ArrowRight";

    // A floating window is a scope only while it floats, active while it is the workspace's current window and focus is
    // inside it. Escape closes the focused window and nothing else; a modal one bars everything beneath it; a press
    // away reaches the window only when it is to send the window back.
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

    // The element the window is held within, once the host's reference to it has been captured.
    private ElementReference? BoundsElement
        => Options.Bounds.Element is { Id: not null } element ? element : null;

    // Viewport coordinates from the pointer are physical, so the placement is written as left and top rather than
    // as logical insets.
    private string? Style
    {
        get
        {
            var hostStyle = AdditionalAttributes.HostValue("style");

            if (!IsFloating || IsMaximized)
            {
                return hostStyle;
            }

            var parts = new List<string>(3);

            if (Placement is { } placement)
            {
                parts.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"left: {placement.Left}px; top: {placement.Top}px; width: {placement.Width}px; height: {placement.Height}px;"));
            }

            if (Options.MinResizeSize is { } minimum)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"min-width: {minimum.Width}px; min-height: {minimum.Height}px;"));
            }

            if (Options.MaxResizeSize is { } maximum)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"max-width: {maximum.Width}px; max-height: {maximum.Height}px;"));
            }

            if (!string.IsNullOrWhiteSpace(hostStyle))
            {
                parts.Add(hostStyle);
            }

            return parts.Count > 0 ? string.Join(' ', parts) : null;
        }
    }

    private IReadOnlyList<FormaToolbarItem> TitleBarItems
    {
        get
        {
            var items = new List<FormaToolbarItem>(Commands.Count + 4);
            items.AddRange(Commands);

            if (Options.Dockable)
            {
                items.Add(new FormaToolbarToggle
                {
                    Id = DockCommand,
                    Command = DockCommand,
                    Title = L["Undock"],
                    Icon = Glyph.Unpin,
                    IsActive = IsFloating,
                    AlternateTitle = L["Dock"],
                    AlternateIcon = Glyph.Pin,
                });
            }

            if (Options.Minimizable)
            {
                items.Add(new FormaToolbarCommand
                {
                    Id = MinimizeCommand,
                    Command = MinimizeCommand,
                    Title = L["Minimize"],
                    Icon = Glyph.WindowMinimize,
                });
            }

            if (Options.Maximizable && IsFloating)
            {
                items.Add(new FormaToolbarToggle
                {
                    Id = MaximizeCommand,
                    Command = MaximizeCommand,
                    Title = L["Maximize"],
                    Icon = Glyph.Maximize,
                    IsActive = Maximized,
                    AlternateTitle = L["Restore"],
                    AlternateIcon = Glyph.Minimize,
                });
            }

            if (Options.Closable)
            {
                items.Add(new FormaToolbarCommand
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
        _window = new Window(JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        if (Options.MinResizeSize is { } minimum && Options.MaxResizeSize is { } maximum && (minimum.Width > maximum.Width || minimum.Height > maximum.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(Options), maximum, $"{nameof(WindowOptions.MaxResizeSize)} cannot be smaller than {nameof(WindowOptions.MinResizeSize)}.");
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
        var attachment = new WindowAttachment(Mode, Modal, BoundsElement, ShowsBackdrop);

        if (_attachment != attachment)
        {
            _attachment = attachment;
            _renderedMaximized = IsMaximized;
            await _window.DetachAsync();
            await ReleaseScopeAsync();

            var attached = await _window.AttachAsync(_element, _selfReference, TitleBarClass, HandleClass, StepPixels, IsFloating, IsPresented, Modal, BoundsElement, ShowsBackdrop ? _backdrop : null);

            if (!attached || !IsFloating)
            {
                return;
            }

            if (Placement is null)
            {
                await _window.MeasureAsync();
            }

            await RegisterScopeAsync();

            // A window that takes to floating is the one the reader is working in.
            if (!Active && !Disabled && IsPresented)
            {
                await ActivateAsync();
            }

            return;
        }

        if (_renderedMaximized != IsMaximized)
        {
            _renderedMaximized = IsMaximized;
            await _window.FitAsync();
        }

        if (_inputScope is { } inputScope && ScopeInputs != _scopeInputs)
        {
            _scopeInputs = ScopeInputs;
            await inputScope.UpdateAsync(Profile);
        }
    }

    /// <summary>Takes the window's placement as the browser reports it, after a drag, a keyboard step, or the first measurement.</summary>
    [JSInvokable]
    public async Task ReportPlacementAsync(double left, double top, double width, double height)
    {
        var placement = new WindowPlacement(left, top, width, height);

        if (placement == Placement)
        {
            return;
        }

        Placement = placement;
        await InvokeAsync(StateHasChanged);
        await PlacementChanged.InvokeAsync(placement);
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
        => IsFloating && IsPresented ? new(InvokeAsync(() => _window.RaiseAsync().AsTask())) : ValueTask.CompletedTask;

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
            case DockCommand:
                await SetModeAsync(IsFloating ? WindowMode.Docked : WindowMode.Floating);
                break;
            default:
                await OnCommand.InvokeAsync(command);
                break;
        }
    }

    private async Task SetMaximizedAsync(bool maximized)
    {
        Maximized = maximized;
        await MaximizedChanged.InvokeAsync(maximized);
    }

    private async Task SetModeAsync(WindowMode mode)
    {
        Mode = mode;
        await ModeChanged.InvokeAsync(mode);
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
        else if (IsFloating)
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
        => new(Title, Icon, Parent?.Id, Origin, CascadedOrigin, Mode, Minimized, Disabled, Modal);

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

    private async Task PresentAsync()
    {
        if (_attachment is not null)
        {
            await _window.PresentAsync(IsPresented);
        }

        if (!IsPresented && _inputScope is { } inputScope)
        {
            await inputScope.ReleaseAsync();
        }

        StateHasChanged();
    }

    private async Task RegisterScopeAsync()
    {
        var inputs = ScopeInputs;
        var inputScope = await InputRouter.RegisterAsync(_element, Profile, _selfReference);

        if (inputScope is null)
        {
            return;
        }

        // The window may have docked, or another pass may have registered it, while the registration was being made.
        if (!IsFloating || _inputScope is not null)
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
    public async ValueTask DisposeAsync()
    {
        if (_declaration is not null)
        {
            await Manager.UnregisterAsync(this);
        }

        await ReleaseScopeAsync();
        await _window.DisposeAsync();
        _selfReference.Dispose();
    }
}
