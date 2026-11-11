namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Browser.Interop.Components;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A raised backdrop with a box of its own that the reader can move, resize and maximize within its bounds.</summary>
/// <remarks>
/// The frame stands in its layout until it is given a placement or maximized; then it stands in the viewport within its
/// bounds, moved by its drag region and the arrow keys and resized at its edges and with Shift and the arrow keys. Under a
/// cascaded <see cref="FrameBox.Parent"/>, it drives the box of the parent that presents it.
/// </remarks>
public sealed partial class FormaFrame : FormaElementBase
{
    /// <summary>The class the resize handles carry, one per edge and corner.</summary>
    public const string HandleClass = "forma-frame-handle";

    /// <summary>The class the drag region carries.</summary>
    public const string DragRegionClass = "forma-frame-drag-region";

    /// <summary>How far one arrow press moves or resizes the frame, in CSS pixels: one spacing step of four.</summary>
    private const double StepPixels = 16;

    private static readonly string[] Edges = ["n", "s", "e", "w", "ne", "nw", "se", "sw"];

    private Frame _frame = default!;
    private DotNetObjectReference<FormaFrame> _selfReference = default!;
    private FrameAttachment? _attachment;

    /// <summary>The name the drag region announces the frame by, where <see langword="null"/> is "the frame".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The region a drag moves the frame by, which takes the arrow keys while focused.</summary>
    [Parameter] public RenderFragment? DragRegion { get; set; }

    /// <summary>The content the frame holds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Where the frame stands and how large it is, where <see langword="null"/> stands it in its layout.</summary>
    [Parameter] public FramePlacement? Placement { get; set; }

    /// <summary>Raised when the reader moves or resizes the frame, and once when it is first measured.</summary>
    [Parameter] public EventCallback<FramePlacement> PlacementChanged { get; set; }

    /// <summary>Whether the frame fills its bounds.</summary>
    [Parameter] public bool Maximized { get; set; }

    /// <summary>Whether the frame is held at its placement and size.</summary>
    [Parameter] public bool Locked { get; set; }

    // The parent that presents the frame cascades Parent; the frame cascades Self to what it holds.
    [CascadingParameter] private FrameBox Box { get; set; }

    /// <summary>What the reader may do to the frame, how far, and where it is held.</summary>
    [Parameter] public FrameOptions Options { get; set; } = FrameOptions.Default;

    /// <summary>Whether the frame is hidden.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the frame is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    // The frame has a box to move once it stands in the viewport, or while it drives the parent that presents it.
    private bool IsInteractive
        => Box is FrameBox.Parent || Placement is not null || Maximized;

    private bool IsHeld
        => Maximized || Locked;

    private bool IsMovable
        => IsInteractive && Options.Movable && !IsHeld;

    private bool ShowsHandles
        => IsInteractive && Options.Resizable && !IsHeld;

    private bool TakesArrowKeys
        => IsInteractive && !IsHeld && (Options.Movable || Options.Resizable);

    private bool IsPlaced
        => Box is FrameBox.Self && Placement is not null;

    private string Name
        => Label ?? L["the frame"];

    private Dictionary<string, object>? DragRegionAttributes
    {
        get
        {
            if (!TakesArrowKeys)
            {
                return null;
            }

            var (label, keys) = (Options.Movable, Options.Resizable) switch
            {
                (true, true) => (L["Move {Name} with the arrow keys; hold Shift to resize it", Name], "ArrowUp ArrowDown ArrowLeft ArrowRight Shift+ArrowUp Shift+ArrowDown Shift+ArrowLeft Shift+ArrowRight"),
                (true, false) => (L["Move {Name} with the arrow keys", Name], "ArrowUp ArrowDown ArrowLeft ArrowRight"),
                _ => (L["Resize {Name} with Shift and the arrow keys", Name], "Shift+ArrowUp Shift+ArrowDown Shift+ArrowLeft Shift+ArrowRight"),
            };

            return new()
            {
                ["tabindex"] = "0",
                ["role"] = "group",
                ["aria-label"] = label,
                ["aria-keyshortcuts"] = keys,
            };
        }
    }

    // The element the frame is held within, once the host's reference to it has been captured.
    private ElementReference? BoundsElement
        => Options.Bounds.Element is { Id: not null } element ? element : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _frame = new Frame(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Options.MinResizeSize is { } minimum && Options.MaxResizeSize is { } maximum && (minimum.Width > maximum.Width || minimum.Height > maximum.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(Options), maximum, $"{nameof(FrameOptions.MaxResizeSize)} cannot be smaller than {nameof(FrameOptions.MinResizeSize)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var attachment = new FrameAttachment(IsInteractive, Box, BoundsElement);

        if (_attachment == attachment)
        {
            return;
        }

        _attachment = attachment;
        await _frame.DetachAsync();

        if (!IsInteractive)
        {
            return;
        }

        var box = Box is FrameBox.Parent ? "parent" : "self";
        var isAttached = await _frame.AttachAsync(RootElement, _selfReference, box, DragRegionClass, HandleClass, StepPixels, BoundsElement);

        if (isAttached && !Maximized)
        {
            await _frame.MeasureAsync();
        }
    }

    /// <summary>Takes the box's placement as the browser reports it.</summary>
    [JSInvokable]
    public Task ReportPlacementAsync(double left, double top, double width, double height)
        => InvokeAsync(async () =>
        {
            var placement = new FramePlacement(left, top, width, height);

            if (Box is FrameBox.Self)
            {
                if (placement == Placement)
                {
                    return;
                }

                Placement = placement;
                StateHasChanged();
            }

            await PlacementChanged.InvokeAsync(placement);
        });

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-frame")
            .Add("has-placement", IsPlaced)
            .Add("is-maximized", Maximized)
            .Add("is-locked", Locked)
            .Add("is-movable", IsMovable)
            .Add("is-disabled", Disabled);

    // The placement is written as physical left and top, as the pointer's viewport coordinates are.
    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (IsPlaced && !Maximized)
        {
            var placement = Placement!;
            builder
                .Add("left", CssLength.FromPx(placement.Left))
                .Add("top", CssLength.FromPx(placement.Top))
                .Add("width", CssLength.FromPx(placement.Width))
                .Add("height", CssLength.FromPx(placement.Height));
        }

        if (Options.MinResizeSize is { } minimum)
        {
            builder
                .Add("min-width", CssLength.FromPx(minimum.Width))
                .Add("min-height", CssLength.FromPx(minimum.Height));
        }

        if (Options.MaxResizeSize is { } maximum)
        {
            builder
                .Add("max-width", CssLength.FromPx(maximum.Width))
                .Add("max-height", CssLength.FromPx(maximum.Height));
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _frame.DisposeAsync();
        _selfReference.Dispose();
        await base.DisposeAsyncCore();
    }
}
