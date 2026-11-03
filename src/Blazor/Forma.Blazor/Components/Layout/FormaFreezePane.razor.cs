namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>A scroll box whose content moves along one axis while the parts marked <see cref="FrozenClass"/> stay at its start.</summary>
/// <remarks>
/// The content is moved by the scrollbar, or by an arrow at each end shown while the content can still move that way.
/// Parts marked <see cref="StopClass"/> are where the content comes to rest.
/// </remarks>
public sealed partial class FormaFreezePane : FormaElementBase
{
    /// <summary>The class that holds a part of the content at the pane's start while the rest scrolls.</summary>
    public const string FrozenClass = "forma-freeze-pane-frozen";

    /// <summary>The class that makes a part of the content a stop.</summary>
    /// <remarks>Scrolling comes to rest with a stop drawn up to the frozen parts, and each arrow press moves to the next stop.</remarks>
    public const string StopClass = "forma-freeze-pane-stop";

    /// <summary>The content that scrolls.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>How the content moves.</summary>
    [Parameter] public FreezePaneOptions Options { get; set; } = FreezePaneOptions.Default;

    /// <summary>Raised with the frozen region's extent in pixels as the reader drags or keys its edge.</summary>
    /// <remarks>The frozen region's edge is drawn while this callback is set.</remarks>
    [Parameter] public EventCallback<double> FrozenExtentChanged { get; set; }

    // One small spacing step, the distance an arrow key moves the frozen edge.
    private const double FrozenEdgeKeyStep = 8;

    private const string ScrollAction = "freeze-pane.scroll";
    private const string ScrollBackAction = "freeze-pane.scroll-back";
    private const string ScrollForwardAction = "freeze-pane.scroll-forward";
    private const string BackArrowClass = "forma-freeze-pane-arrow-back";
    private const string ForwardArrowClass = "forma-freeze-pane-arrow-forward";

    private ElementReference _viewport;
    private DomObservation<ScrollWatchOptions, ScrollState>? _scrollState;
    private DomInteraction<ScrollInteractionOptions>? _scrolling;
    private ScrollState? _state;

    private bool IsMeasured
        => _state is not null;

    private bool ShowsArrows
        => Options.Navigation == ScrollNavigation.Arrows;

    private bool CanScrollBack
        => _state?.CanScrollBack ?? false;

    private bool CanScrollForward
        => _state?.CanScrollForward ?? false;

    private double FrozenExtent
        => _state?.FrozenExtent ?? 0;

    private double CornerExtent
        => _state?.CornerExtent ?? 0;

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

    private ScrollWatchOptions WatchOptions
        => new() { Axis = Options.Axis, FrozenSelector = $".{FrozenClass}", ReportsViewportExtent = ShowsFrozenEdge };

    // The arrows are given whether or not they are drawn.
    private ScrollInteractionOptions InteractionOptions
        => new()
        {
            Steps = new ScrollStepOptions { Axis = Options.Axis, StopSelector = $".{StopClass}", FrozenSelector = $".{FrozenClass}" },
            Buttons = new ScrollButtons
            {
                Container = RootElement,
                BackSelector = $".{BackArrowClass}",
                ForwardSelector = $".{ForwardArrowClass}",
                BackAction = ScrollBackAction,
                ForwardAction = ScrollForwardAction,
            },
            WheelAction = WantsWheel ? ScrollAction : null,
        };

    // The wheel moves inline content only; along the block axis the browser already scrolls by it.
    private bool WantsWheel
        => !IsBlockAxis && Options.PageOnWheelGesture;

    private bool WantsInteraction
        => WantsWheel || ShowsArrows;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var scroll = Context.DomEngine.GetElement(_viewport).Scroll;
            var watching = scroll.WatchAsync(HandleScrollStateAsync, WatchOptions);
            var attaching = scroll.AttachAsync(InteractionOptions, WantsInteraction);

            _scrollState = await watching;
            _scrolling = await attaching;
        }

        // A render made while the first requests were settling is caught up here.
        if (_scrollState is { } scrollState && _scrolling is { } scrolling)
        {
            await Task.WhenAll(
                scrollState.SyncAsync(_viewport, WatchOptions),
                scrolling.SyncAsync(_viewport, InteractionOptions, WantsInteraction));
        }
    }

    // The state arrives off the renderer.
    private Task HandleScrollStateAsync(ScrollState state)
    {
        if (_state == state)
        {
            return Task.CompletedTask;
        }

        _state = state;

        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-freeze-pane")
            .Add(IsBlockAxis ? "forma-freeze-pane-block" : "forma-freeze-pane-inline")
            .Add(ShowsArrows ? "forma-freeze-pane-arrows" : "forma-freeze-pane-scrollbar")
            .Add("forma-freeze-pane-cross-axis", Options.ScrollsCrossAxis)
            .Add("is-measured", IsMeasured)
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
}
