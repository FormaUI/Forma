namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Framework.Icons;
using Forma.Framework.Interop.Behaviours;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A scroll box whose content moves along one axis while the parts marked <see cref="FrozenClass"/> stay at its start, moved by the scrollbar or by an arrow at each end shown only while there is somewhere to go; parts marked <see cref="StopClass"/> are where the content comes to rest.</summary>
public sealed partial class FormaFreezePane : ComponentBase, IAsyncDisposable
{
    /// <summary>The class a part of the content carries to stay put at the pane's start while the rest scrolls.</summary>
    public const string FrozenClass = "forma-freeze-pane-frozen";

    /// <summary>The class a part of the content carries to be a stop: scrolling comes to rest with it drawn up to the frozen parts, and each arrow press moves to the next one.</summary>
    public const string StopClass = "forma-freeze-pane-stop";

    /// <summary>The content that scrolls.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>How the content moves.</summary>
    [Parameter] public FreezePaneOptions Options { get; set; } = FreezePaneOptions.Default;

    /// <summary>Extra classes applied alongside forma-freeze-pane.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Additional attributes applied to the pane element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaFreezePane> L { get; set; } = default!;

    private Scroll _scroll = default!;
    private DotNetObjectReference<FormaFreezePane>? _selfReference;
    private ElementReference _viewport;
    private bool _canScrollBack;
    private bool _canScrollForward;
    private bool _isMeasured;
    private double _frozenExtent;
    private double _cornerExtent;
    private ScrollBehaviours? _attached;
    private bool _isSyncing;

    private bool ShowsArrows
        => Options.Navigation == ScrollNavigation.Arrows;

    private bool IsBlockAxis
        => Options.Axis == ScrollAxis.Block;

    private string CssClass
    {
        get
        {
            var axis = IsBlockAxis ? "forma-freeze-pane-block" : "forma-freeze-pane-inline";
            var navigation = ShowsArrows ? "forma-freeze-pane-arrows" : "forma-freeze-pane-scrollbar";
            var measured = _isMeasured ? " is-measured" : string.Empty;
            var frozen = _frozenExtent > 0 ? " has-frozen" : string.Empty;
            var classes = $"forma-freeze-pane {axis} {navigation}{measured}{frozen}";

            return string.IsNullOrWhiteSpace(Class) ? classes : $"{classes} {Class}";
        }
    }

    private string? FrozenStyle
        => _frozenExtent > 0
            ? string.Create(CultureInfo.InvariantCulture, $"--forma-freeze-pane-frozen-extent: {_frozenExtent}px; --forma-freeze-pane-corner-extent: {_cornerExtent}px")
            : null;

    private string BackLabel
        => Options.ScrollBackLabel ?? (IsBlockAxis ? L["Scroll Up"] : L["Scroll Back"]);

    private string ForwardLabel
        => Options.ScrollForwardLabel ?? (IsBlockAxis ? L["Scroll Down"] : L["Scroll Forward"]);

    private Glyph BackGlyph
        => IsBlockAxis ? Glyph.ChevronUp : Glyph.ChevronRight;

    private Glyph ForwardGlyph
        => IsBlockAxis ? Glyph.ChevronDown : Glyph.ChevronRight;

    // The wheel moves inline content only; along the block axis the browser already scrolls by it.
    private ScrollBehaviours Behaviours
        => new()
        {
            Boundary = new ScrollBoundary(Options.Axis, $".{FrozenClass}"),
            Wheel = !IsBlockAxis && Options.PageOnWheelGesture ? ScrollWheel.MovesTarget : null,
        };

    /// <summary>Takes how far the content can still move each way, how far its frozen parts reach in from the start and how far the frozen corner reaches across the axis; called from the script when that changes.</summary>
    [JSInvokable]
    public Task HandleOverflowAsync(bool canScrollBack, bool canScrollForward, double frozenExtent, double cornerExtent)
    {
        if (_isMeasured && _canScrollBack == canScrollBack && _canScrollForward == canScrollForward && _frozenExtent == frozenExtent && _cornerExtent == cornerExtent)
        {
            return Task.CompletedTask;
        }

        _isMeasured = true;
        _canScrollBack = canScrollBack;
        _canScrollForward = canScrollForward;
        _frozenExtent = frozenExtent;
        _cornerExtent = cornerExtent;

        // The report arrives off the renderer, so the rerender has to be marshalled back onto it.
        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
        => _scroll = new Scroll(JS);

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
            for (var behaviours = Behaviours; behaviours != _attached; behaviours = Behaviours)
            {
                if (!await AttachAsync(behaviours))
                {
                    return;
                }

                _attached = behaviours;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async ValueTask<bool> AttachAsync(ScrollBehaviours behaviours)
    {
        _selfReference ??= DotNetObjectReference.Create(this);

        return await _scroll.AttachAsync(_viewport, behaviours, _selfReference);
    }

    private async Task ScrollAsync(int direction)
        => await _scroll.StepAsync(_viewport, direction, Options.Axis, new ScrollStops($".{StopClass}", _frozenExtent));

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _scroll.DisposeAsync();
        _selfReference?.Dispose();
    }
}
