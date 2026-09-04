namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Framework.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A draggable divider between two panes, reporting the size the pane before it should take.</summary>
public sealed partial class FormaSplitter : ComponentBase, IAsyncDisposable
{
    /// <summary>The size of the pane the splitter governs, in whatever unit the caller works in.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Raised as the splitter is dragged or keyed, with the clamped size.</summary>
    [Parameter] public EventCallback<double> ValueChanged { get; set; }

    /// <summary>The smallest size the pane may take.</summary>
    [Parameter] public double Min { get; set; }

    /// <summary>The largest size the pane may take.</summary>
    [Parameter] public double Max { get; set; }

    /// <summary>How much one arrow key moves the divider.</summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>How many pixels one unit of Value spans. Defaults to 16, so a caller sizing in rem needs no conversion.</summary>
    [Parameter] public double PixelsPerUnit { get; set; } = 16;

    /// <summary>Which way the bar runs, and so which axis it is dragged along.</summary>
    [Parameter] public SplitterOrientation Orientation { get; set; } = SplitterOrientation.Vertical;

    /// <summary>Accessible name for the divider; what it resizes decides the wording.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Raised when a drag starts and again when it ends, so a host can suppress selection while it runs.</summary>
    [Parameter] public EventCallback<bool> OnResizingChanged { get; set; }

    /// <summary>Extra classes applied alongside forma-splitter.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the divider element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private Pointer _pointer = default!;
    private ElementReference _element;
    private double _dragOrigin;
    private double _dragStartValue;
    private bool _isResizing;

    private bool IsVertical
        => Orientation is SplitterOrientation.Vertical;

    private string AriaOrientation
        => IsVertical ? "vertical" : "horizontal";

    private string CssClass
    {
        get
        {
            var orientation = IsVertical ? "forma-splitter-vertical" : "forma-splitter-horizontal";
            var resizing = _isResizing ? " is-resizing" : string.Empty;

            return string.IsNullOrWhiteSpace(Class)
                ? $"forma-splitter {orientation}{resizing}"
                : $"forma-splitter {orientation}{resizing} {Class}";
        }
    }

    private static string Rounded(double value)
        => Math.Round(value).ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override void OnInitialized()
        => _pointer = new Pointer(JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Min, Max);
    }

    private async Task HandlePointerDownAsync(PointerEventArgs args)
    {
        _isResizing = true;
        _dragOrigin = IsVertical ? args.ClientX : args.ClientY;
        _dragStartValue = Value;

        await OnResizingChanged.InvokeAsync(true);
        await _pointer.CaptureAsync(_element, args.PointerId);
    }

    private async Task HandlePointerMoveAsync(PointerEventArgs args)
    {
        if (!_isResizing)
        {
            return;
        }

        var travelled = (IsVertical ? args.ClientX : args.ClientY) - _dragOrigin;

        // The pane sits before the divider, so in a right-to-left layout it grows as the pointer moves the other way.
        var direction = IsVertical && CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? -1 : 1;

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
        await _pointer.ReleaseAsync(_element, args.PointerId);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        var isRightToLeft = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        var grow = IsVertical ? (isRightToLeft ? "ArrowLeft" : "ArrowRight") : "ArrowDown";
        var shrink = IsVertical ? (isRightToLeft ? "ArrowRight" : "ArrowLeft") : "ArrowUp";

        if (args.Key == grow)
        {
            await SetValueAsync(Value + Step);
        }
        else if (args.Key == shrink)
        {
            await SetValueAsync(Value - Step);
        }
        else if (args.Key == "Home")
        {
            await SetValueAsync(Min);
        }
        else if (args.Key == "End")
        {
            await SetValueAsync(Max);
        }
    }

    private async Task SetValueAsync(double value)
    {
        var clamped = Math.Clamp(value, Min, Max);

        if (clamped.Equals(Value))
        {
            return;
        }

        Value = clamped;

        await ValueChanged.InvokeAsync(clamped);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _pointer.DisposeAsync();
}
