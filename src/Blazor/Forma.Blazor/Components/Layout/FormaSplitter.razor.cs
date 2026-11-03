namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Framework.Browser.Interop.Components;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A draggable divider between two panes, reporting the size the pane before it should take.</summary>
/// <remarks>
/// The browser holds the pointer from the press and tells the splitter how far it has traveled once a frame, so the drag
/// follows the pointer and ends on its release wherever it is let go; Escape cancels the drag and returns the size it
/// started from.
/// </remarks>
public sealed partial class FormaSplitter : FormaElementBase
{
    private const string GrowAction = "splitter.grow";
    private const string ShrinkAction = "splitter.shrink";
    private const string MinAction = "splitter.min";
    private const string MaxAction = "splitter.max";
    private const string ResizeAction = "splitter.resize";

    // A vertical bar moves along the inline axis, where the arrow along the reading direction grows the pane before it; a horizontal bar moves down the block axis.
    private static readonly InputProfile VerticalLeftToRightProfile = CreateProfile(grow: "ArrowRight", shrink: "ArrowLeft");
    private static readonly InputProfile VerticalRightToLeftProfile = CreateProfile(grow: "ArrowLeft", shrink: "ArrowRight");
    private static readonly InputProfile HorizontalProfile = CreateProfile(grow: "ArrowDown", shrink: "ArrowUp");

    /// <summary>The size of the pane the splitter governs, in the caller's unit.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Raised as the splitter is dragged or keyed, with the clamped size.</summary>
    [Parameter] public EventCallback<double> ValueChanged { get; set; }

    /// <summary>The smallest size the pane may take.</summary>
    [Parameter] public double Min { get; set; }

    /// <summary>The largest size the pane may take.</summary>
    [Parameter] public double Max { get; set; }

    /// <summary>How much one arrow key moves the divider.</summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>The pixels one unit of <see cref="Value"/> spans.</summary>
    [Parameter] public double PixelsPerUnit { get; set; } = 16;

    /// <summary>Which way the bar runs, and so which axis it is dragged along.</summary>
    [Parameter] public SplitterOrientation Orientation { get; set; } = SplitterOrientation.Vertical;

    /// <summary>The accessible name of the divider.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Raised with whether a drag is resizing the pane, when the drag starts and when it ends.</summary>
    [Parameter] public EventCallback<bool> OnResizingChanged { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private Splitter _splitter = default!;
    private DotNetObjectReference<FormaSplitter>? _selfReference;
    private InputRegistration _inputRegistration = default!;
    private bool? _attachedInline;
    private double _dragStartValue;
    private bool _isResizing;

    private bool IsVertical
        => Orientation is SplitterOrientation.Vertical;

    private string AriaOrientation
        => IsVertical ? "vertical" : "horizontal";

    private InputProfile Profile
        => IsVertical
            ? Context.CultureState.IsRightToLeft ? VerticalRightToLeftProfile : VerticalLeftToRightProfile
            : HorizontalProfile;

    private static string FormatRounded(double value)
        => Math.Round(value).ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _splitter = new Splitter(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
        _inputRegistration = new InputRegistration(InputRouter, HandleBarActionAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Min, Max);
    }

    // The orientation, and with it the keys and the axis the bar is dragged along, can change after the registration was made.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _inputRegistration.SyncAsync(RootElement, Profile);

        if (_attachedInline == IsVertical)
        {
            return;
        }

        // Claimed before the await, so a render arriving mid-call attaches no second gesture.
        var isInline = IsVertical;
        _attachedInline = isInline;
        await _splitter.DetachAsync();

        if (!await _splitter.AttachAsync(RootElement, _selfReference!, isInline, ResizeAction) && _attachedInline == isInline)
        {
            _attachedInline = null;
        }
    }

    /// <summary>Starts resizing the pane, as the bar is pressed.</summary>
    [JSInvokable]
    public Task HandleResizeStartAsync()
        => InvokeAsync(async () =>
        {
            _isResizing = true;
            _dragStartValue = Value;

            await OnResizingChanged.InvokeAsync(true);
            StateHasChanged();
        });

    /// <summary>Resizes the pane by the pointer's <paramref name="travel"/> from where the bar was pressed, in pixels along the axis the bar moves on.</summary>
    [JSInvokable]
    public Task HandleResizeAsync(double travel)
        => InvokeAsync(async () =>
        {
            if (!_isResizing)
            {
                return;
            }

            // The pane sits before the divider, so in a right-to-left layout it grows as the pointer moves the other way.
            var direction = IsVertical && Context.CultureState.IsRightToLeft ? -1 : 1;

            if (await SetValueAsync(_dragStartValue + (travel / PixelsPerUnit * direction)))
            {
                StateHasChanged();
            }
        });

    /// <summary>Ends resizing the pane, as the bar is let go, or returns the size the drag started from where it was canceled.</summary>
    [JSInvokable]
    public Task HandleResizeEndAsync(bool isCanceled)
        => InvokeAsync(async () =>
        {
            if (!_isResizing)
            {
                return;
            }

            _isResizing = false;

            if (isCanceled)
            {
                await SetValueAsync(_dragStartValue);
            }

            await OnResizingChanged.InvokeAsync(false);
            StateHasChanged();
        });

    private async Task HandleBarActionAsync(string action, InputInfo input)
    {
        double? target = action switch
        {
            GrowAction => Value + Step,
            ShrinkAction => Value - Step,
            MinAction => Min,
            MaxAction => Max,
            _ => null,
        };

        if (target is { } size && await SetValueAsync(size))
        {
            StateHasChanged();
        }
    }

    /// <summary>Takes the size, clamped to the bounds, and reports whether it differs from the current one.</summary>
    private async Task<bool> SetValueAsync(double value)
    {
        var clamped = Math.Clamp(value, Min, Max);

        if (clamped.Equals(Value))
        {
            return false;
        }

        Value = clamped;

        await ValueChanged.InvokeAsync(clamped);

        return true;
    }

    private static InputProfile CreateProfile(string grow, string shrink)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(GrowAction, new KeyBinding(grow)),
                new InputRoute(ShrinkAction, new KeyBinding(shrink)),
                new InputRoute(MinAction, new KeyBinding("Home")),
                new InputRoute(MaxAction, new KeyBinding("End")),
            ],
        };

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-splitter")
            .Add(IsVertical ? "forma-splitter-vertical" : "forma-splitter-horizontal")
            .Add("is-resizing", _isResizing);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await _splitter.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
