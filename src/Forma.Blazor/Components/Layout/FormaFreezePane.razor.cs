namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Common.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A scroll box whose content moves along one axis while the parts marked <see cref="FrozenClass"/> stay at its start, moved by the scrollbar or by an arrow at each end shown only while there is somewhere to go; parts marked <see cref="StopClass"/> are where the content comes to rest.</summary>
public sealed partial class FormaFreezePane : FormaElementBase
{
    /// <summary>The class a part of the content carries to stay put at the pane's start while the rest scrolls.</summary>
    public const string FrozenClass = "forma-freeze-pane-frozen";

    /// <summary>The class a part of the content carries to be a stop: scrolling comes to rest with it drawn up to the frozen parts, and each arrow press moves to the next one.</summary>
    public const string StopClass = "forma-freeze-pane-stop";

    /// <summary>The content that scrolls.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>How the content moves.</summary>
    [Parameter] public FreezePaneOptions Options { get; set; } = FreezePaneOptions.Default;

    /// <summary>Raised as the reader drags or keys the frozen region's edge, with the extent asked for in pixels; the content decides what to resize to honor it, and the edge is drawn only while this is set.</summary>
    [Parameter] public EventCallback<double> FrozenExtentChanged { get; set; }

    // One small spacing step, the distance an arrow key moves the frozen edge.
    private const double FrozenEdgeKeyStep = 8;

    private static readonly ScrollBoundaryReport Unmeasured = new(false, false, 0, 0, 0);

    private Scroll _scroll = default!;
    private Wheel _wheel = default!;
    private DotNetObjectReference<FormaFreezePane>? _selfReference;
    private ElementReference _viewport;
    private bool _isMeasured;
    private ScrollBoundaryReport _report = Unmeasured;
    private ScrollBehaviors? _attached;
    private bool _isSyncing;

    private bool ShowsArrows
        => Options.Navigation == ScrollNavigation.Arrows;

    private bool CanScrollBack
        => _report.CanScrollBack;

    private bool CanScrollForward
        => _report.CanScrollForward;

    private double FrozenExtent
        => _report.FrozenExtent;

    private double CornerExtent
        => _report.CornerExtent;

    private bool ShowsFrozenEdge
        => !IsBlockAxis && FrozenExtent > 0 && FrozenExtentChanged.HasDelegate;

    private string FrozenEdgeLabel
        => Options.FrozenEdgeLabel ?? L["Resize Frozen Region"];

    private bool IsBlockAxis
        => Options.Axis == ScrollAxis.Block;

    private string BackLabel
        => Options.ScrollBackLabel ?? (IsBlockAxis ? L["Scroll Up"] : L["Scroll Back"]);

    private string ForwardLabel
        => Options.ScrollForwardLabel ?? (IsBlockAxis ? L["Scroll Down"] : L["Scroll Forward"]);

    private Glyph BackGlyph
        => IsBlockAxis ? Glyph.ChevronUp : Glyph.ChevronLeft;

    private Glyph ForwardGlyph
        => IsBlockAxis ? Glyph.ChevronDown : Glyph.ChevronRight;

    private ScrollBehaviors Behaviors
        => new() { Boundary = new ScrollBoundary(Options.Axis, $".{FrozenClass}") };

    // The wheel moves inline content only; along the block axis the browser already scrolls by it.
    private bool WantsWheel
        => !IsBlockAxis && Options.PageOnWheelGesture;

    /// <summary>Takes how far the content can still move each way, how far its frozen parts reach and how much of the viewport is in view; called from the script when that changes.</summary>
    [JSInvokable]
    public Task HandleOverflowAsync(ScrollBoundaryReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (_isMeasured && _report == report)
        {
            return Task.CompletedTask;
        }

        _isMeasured = true;
        _report = report;

        // The report arrives off the renderer, so the rerender has to be marshalled back onto it.
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _scroll = new Scroll(Context.JS);
        _wheel = new Wheel(Context.JS);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            // The options can change again while an attach is in flight, so the loop re-reads them after each one.
            for (var behaviors = Behaviors; behaviors != _attached; behaviors = Behaviors)
            {
                if (!await AttachAsync(behaviors))
                {
                    return;
                }

                _attached = behaviors;
            }

            if (WantsWheel && !_wheel.IsAttached)
            {
                await _wheel.AttachAsync(_viewport, ScrollWheel.MovesTarget);
            }
            else if (!WantsWheel && _wheel.IsAttached)
            {
                await _wheel.DetachAsync();
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async ValueTask<bool> AttachAsync(ScrollBehaviors behaviors)
    {
        _selfReference ??= DotNetObjectReference.Create(this);

        return await _scroll.AttachAsync(_viewport, behaviors, _selfReference);
    }

    private async Task ScrollAsync(int direction)
        => await _scroll.StepAsync(_viewport, direction, Options.Axis, new ScrollStops($".{StopClass}", FrozenExtent));

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-freeze-pane")
            .Add(IsBlockAxis ? "forma-freeze-pane-block" : "forma-freeze-pane-inline")
            .Add(ShowsArrows ? "forma-freeze-pane-arrows" : "forma-freeze-pane-scrollbar")
            .Add("is-measured", _isMeasured)
            .Add("has-frozen", FrozenExtent > 0)
            .Add("is-scrolled", CanScrollBack);

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (FrozenExtent > 0)
        {
            builder
                .Add("--forma-freeze-pane-frozen-extent", CssLength.FromPx(FrozenExtent))
                .Add("--forma-freeze-pane-corner-extent", CssLength.FromPx(CornerExtent));
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _scroll.DisposeAsync();
        await _wheel.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
