namespace Forma.Blazor.Components.Layout;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Common.Interop.Components;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A raised frame with chrome: a title bar carrying the title, an icon and commands, then an optional header band, the body, and an optional footer band. It stands docked in its parent's layout or floats over the page, where the reader moves it by its title bar, resizes it at its edges, and maximizes it to the viewport.</summary>
public sealed partial class FormaWindow : ComponentBase, IAsyncDisposable
{
    /// <summary>The class the resize handles carry, one per edge and corner.</summary>
    public const string HandleClass = "forma-window-handle";

    private const string TitleBarClass = "forma-window-titlebar";
    private const string CloseCommand = "window.close";
    private const string MaximizeCommand = "window.maximize";
    private const string DockCommand = "window.dock";

    /// <summary>How far one arrow press moves or resizes a floating window, in CSS pixels: one spacing step of four.</summary>
    private const double StepPixels = 16;

    private static readonly string[] Edges = ["n", "s", "e", "w", "ne", "nw", "se", "sw"];

    // A floating window is a scope only while it floats, active while focus is inside it, so Escape closes the
    // focused window and nothing else; it never bars the page.
    private static readonly InputProfile FloatingProfile = new()
    {
        Escape = DismissBehavior.Dismiss,
        CloseRequest = DismissBehavior.Dismiss,
        PressAway = PressAwayBehavior.Ignore,
    };

    private readonly string _titleId = $"forma-window-title-{Guid.NewGuid():N}";
    private ElementReference _element;
    private Window _window = default!;
    private DotNetObjectReference<FormaWindow> _selfReference = default!;
    private InputScope? _inputScope;
    private bool _isAttached;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocale<FormaWindow> L { get; set; } = default!;
    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <summary>The title shown in the title bar and read as the window's name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>The icon shown before the title.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>Whether the window is docked in its parent's layout or floats over the page.</summary>
    [Parameter] public WindowMode Mode { get; set; } = WindowMode.Docked;

    /// <summary>Raised when the reader docks or undocks the window from its title bar.</summary>
    [Parameter] public EventCallback<WindowMode> ModeChanged { get; set; }

    /// <summary>Where a floating window stands and how large it is; unset, it opens centered at its default size.</summary>
    [Parameter] public WindowPlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes a floating window, and once when it first floats without a placement.</summary>
    [Parameter] public EventCallback<WindowPlacement> PlacementChanged { get; set; }

    /// <summary>Whether a floating window fills the viewport.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Raised when the reader maximizes or restores the window from its title bar.</summary>
    [Parameter] public EventCallback<bool> MaximizedChanged { get; set; }

    /// <summary>The host's own commands, shown in the title bar before the window's.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Commands { get; set; } = [];

    /// <summary>Raised with the command of a host item pressed in the title bar.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>What the reader may do to the window from its chrome.</summary>
    [Parameter] public WindowOptions Options { get; set; } = WindowOptions.Default;

    /// <summary>Raised when the reader closes the window from its title bar or with Escape.</summary>
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

    private bool IsFloating
        => Mode is WindowMode.Floating;

    private bool IsMaximized
        => IsFloating && Maximized;

    private bool ShowsHandles
        => IsFloating && Options.Resizable && !Maximized;

    private string Role
        => IsFloating ? "dialog" : "region";

    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-window",
            IsFloating ? "forma-window-floating" : "forma-window-docked",
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
            var items = new List<FormaToolbarItem>(Commands.Count + 3);
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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _window = new Window(JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        if (Options.MinResizeSize is { } minimum && Options.MaxResizeSize is { } maximum && (minimum.Width > maximum.Width || minimum.Height > maximum.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(Options), maximum, $"{nameof(WindowOptions.MaxResizeSize)} cannot be smaller than {nameof(WindowOptions.MinResizeSize)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsFloating && !_isAttached)
        {
            _isAttached = await _window.AttachAsync(_element, _selfReference, TitleBarClass, HandleClass, StepPixels);

            if (_isAttached && Placement is null)
            {
                await _window.MeasureAsync();
            }

            if (_inputScope is null)
            {
                await RegisterAsync();
            }
        }
        else if (!IsFloating && _isAttached)
        {
            await LeaveFloatingAsync();
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

    /// <summary>Closes the window as the router dismissed it.</summary>
    [JSInvokable]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The router hands every owner the action and the reason.")]
    public Task HandleDismissAsync(DismissBehavior action, DismissReason reason)
        => action is DismissBehavior.Dismiss && Options.Closable ? OnClose.InvokeAsync() : Task.CompletedTask;

    private Task HandleCommandAsync(string command)
        => command switch
        {
            CloseCommand => OnClose.InvokeAsync(),
            MaximizeCommand => SetMaximizedAsync(!Maximized),
            DockCommand => SetModeAsync(IsFloating ? WindowMode.Docked : WindowMode.Floating),
            _ => OnCommand.InvokeAsync(command),
        };

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

    private Task HandleFocusInAsync()
        => _inputScope is { } inputScope ? inputScope.ActivateAsync().AsTask() : Task.CompletedTask;

    private Task HandleFocusOutAsync()
        => _inputScope is { } inputScope ? inputScope.ReleaseAsync().AsTask() : Task.CompletedTask;

    private async Task RegisterAsync()
    {
        var inputScope = await InputRouter.RegisterAsync(_element, FloatingProfile, _selfReference);

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
    }

    private async Task LeaveFloatingAsync()
    {
        _isAttached = false;
        await _window.DetachAsync();

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await LeaveFloatingAsync();
        await _window.DisposeAsync();
        _selfReference.Dispose();
    }
}
