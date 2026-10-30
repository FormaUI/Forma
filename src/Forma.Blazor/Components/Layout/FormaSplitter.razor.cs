namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A draggable divider between two panes, reporting the size the pane before it should take.</summary>
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

    private Pointer _pointer = default!;
    private InputRegistration _inputRegistration = default!;
    private double _dragOrigin;
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
        _pointer = new Pointer(Context.JS);
        _inputRegistration = new InputRegistration(InputRouter, HandleBarActionAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Min, Max);
    }

    // The orientation, and with it the keys, can change after the registration was made.
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _inputRegistration.SyncAsync(RootElement, Profile);

    private async Task HandleBarActionAsync(string action, InputInfo input)
    {
        if (action == ResizeAction && input is InputPointerInfo press)
        {
            await BeginResizeAsync(press);
            StateHasChanged();
            return;
        }

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

    // The pointer captured here delivers the rest of the resize.
    private async Task BeginResizeAsync(InputPointerInfo press)
    {
        _isResizing = true;
        _dragOrigin = IsVertical ? press.ClientX : press.ClientY;
        _dragStartValue = Value;

        await OnResizingChanged.InvokeAsync(true);
        await _pointer.CaptureAsync(RootElement, press.PointerId);
    }

    private async Task HandlePointerMoveAsync(PointerEventArgs args)
    {
        if (!_isResizing)
        {
            return;
        }

        var travelled = (IsVertical ? args.ClientX : args.ClientY) - _dragOrigin;

        // The pane sits before the divider, so in a right-to-left layout it grows as the pointer moves the other way.
        var direction = IsVertical && Context.CultureState.IsRightToLeft ? -1 : 1;

        await SetValueAsync(_dragStartValue + (travelled / PixelsPerUnit * direction));
    }

    private async Task HandlePointerUpAsync(PointerEventArgs args)
    {
        if (!_isResizing)
        {
            return;
        }

        _isResizing = false;

        await OnResizingChanged.InvokeAsync(false);
        await _pointer.ReleaseAsync(RootElement, args.PointerId);
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
                new InputRoute(ResizeAction, new PointerBinding { Button = 0 }),
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
        await _pointer.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
