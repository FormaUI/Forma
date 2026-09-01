namespace Forma.Blazor.Components.Files;

using System.Globalization;

using Forma.Blazor.Interop.Behaviours;
using Forma.Blazor.Interop.Operations;
using Forma.Blazor.Resources;
using Forma.Features.Files;
using Forma.Features.Files.Providers;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>Renders whatever resource a source address resolves to, through the first provider that claims it.</summary>
public sealed partial class FormaFileViewer : ComponentBase, IAsyncDisposable
{
    private const string ZoomedClass = "zoomed";

    private static readonly int[] ZoomSteps = [100, 150, 200, 300, 400];

    // Last in the chain: it claims only what a browser renders unaided, leaving richer formats to a registered provider.
    private static readonly BrowserResourceProvider BrowserProvider = new();

    /// <summary>Address of the resource to preview.</summary>
    [Parameter, EditorRequired] public string Source { get; set; } = default!;

    /// <summary>The addresses <see cref="Source"/> sits among, enabling previous/next navigation.</summary>
    [Parameter] public IReadOnlyList<string>? Siblings { get; set; }

    /// <summary>Raised when the reader steps to another address in <see cref="Siblings"/>; the host owns switching <see cref="Source"/>.</summary>
    [Parameter] public EventCallback<string> OnNavigate { get; set; }

    /// <summary>Shows the resource's name and size alongside its preview.</summary>
    [Parameter] public bool ShowMetadata { get; set; }

    /// <summary>Zooms the image with a Ctrl-held wheel turn over it.</summary>
    [Parameter] public bool ZoomOnWheelGesture { get; set; } = true;

    /// <summary>Replaces the built-in fallback shown when no provider claims the address.</summary>
    [Parameter] public RenderFragment<string>? UnsupportedTemplate { get; set; }

    [Inject] private IStringLocale<FormaFileViewer> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Providers the host registered, tried before the browser's own handling.</summary>
    [Inject] private IEnumerable<IResourceProvider> RegisteredProviders { get; set; } = default!;

    private PanGesture _pan = default!;

    private WheelScroll _wheelScroll = default!;

    private Scroll _scroll = default!;

    private DotNetObjectReference<FormaFileViewer>? _selfReference;

    private ElementReference _imageFrameElement;

    private string? _resolvedSource;

    private CancellationTokenSource? _resolveCancellation;

    private bool _dragAttached;

    private bool _wheelAttached;

    private readonly SteppedZoom _zoom = new(ZoomSteps);

    private bool _zoomChanged;

    private ResourceInfo? Resource { get; set; }

    private bool IsNavigable
        => Siblings is { Count: > 1 };

    private bool IsZoomed
        => _zoom.IsZoomed;

    private int ZoomPercent
        => _zoom.Percent;

    private string? DisplaySize
        => Resource?.DisplaySize;

    private string DisplayName
        => Resource?.Name ?? Source;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _pan = new PanGesture(JS);
        _wheelScroll = new WheelScroll(JS);
        _scroll = new Scroll(JS);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);

        // Resolves once per address.
        if (_resolvedSource == Source)
        {
            return;
        }

        var requested = Source;

        _zoom.Reset();
        _dragAttached = false;
        _wheelAttached = false;
        _resolvedSource = requested;

        // The name and extension come free from the address, so an unclaimed or still-resolving source still reads properly.
        Resource = new ResourceInfo(requested);

        var token = StartResolving();

        try
        {
            // A slower resolve for an address the reader has already left must not replace the current one.
            if (await ResolveResourceAsync(requested, token) is { } resolved && _resolvedSource == requested)
            {
                Resource = resolved;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private CancellationToken StartResolving()
    {
        var abandoned = _resolveCancellation;

        _resolveCancellation = new CancellationTokenSource();

        abandoned?.Cancel();
        abandoned?.Dispose();

        return _resolveCancellation.Token;
    }

    private async Task<ResourceInfo?> ResolveResourceAsync(string source, CancellationToken cancellationToken)
    {
        foreach (var provider in RegisteredProviders)
        {
            if (await provider.GetResourceAsync(source, cancellationToken) is { } resource)
            {
                return resource;
            }
        }

        return await BrowserProvider.GetResourceAsync(source, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Resource is not ImageResourceInfo)
        {
            return;
        }

        if (!_dragAttached)
        {
            _dragAttached = await _pan.AttachAsync(_imageFrameElement, ZoomedClass);
        }

        // Ctrl with the wheel zooms the image.
        if (ZoomOnWheelGesture && !_wheelAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);
            _wheelAttached = await _wheelScroll.AttachAsync(_imageFrameElement, _selfReference, WheelModifier.Ctrl);
        }
        else if (!ZoomOnWheelGesture && _wheelAttached)
        {
            _wheelAttached = false;
            await _wheelScroll.DetachAsync();
        }

        if (_zoomChanged)
        {
            _zoomChanged = false;
            await _scroll.ToCenterAsync(_imageFrameElement);
        }
    }

    /// <summary>Zooms a step in for a wheel rolled up and a step out for one rolled down.</summary>
    [JSInvokable]
    public Task HandleWheelAsync(int direction)
        => InvokeAsync(() =>
        {
            _zoomChanged |= direction < 0 ? _zoom.In() : _zoom.Out();
            StateHasChanged();
        });

    private void ZoomIn()
        => _zoomChanged |= _zoom.In();

    private void ZoomOut()
        => _zoomChanged |= _zoom.Out();

    private bool CanNavigate(int delta)
        => Siblings is { Count: > 1 } siblings && IndexOfSource(siblings) is { } index
        && index + delta >= 0 && index + delta < siblings.Count;

    private int? IndexOfSource(IReadOnlyList<string> siblings)
    {
        for (var index = 0; index < siblings.Count; index++)
        {
            if (string.Equals(siblings[index], Source, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return null;
    }

    private Task NavigateAsync(int delta)
    {
        if (Siblings is not { Count: > 1 } siblings || IndexOfSource(siblings) is not { } index)
        {
            return Task.CompletedTask;
        }

        var target = index + delta;

        return target < 0 || target >= siblings.Count
            ? Task.CompletedTask
            : OnNavigate.InvokeAsync(siblings[target]);
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // Left/right read as previous/next in reading order, which reverses under RTL.
        var isRightToLeft = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        var delta = args.Key switch
        {
            "ArrowLeft" => isRightToLeft ? 1 : -1,
            "ArrowRight" => isRightToLeft ? -1 : 1,
            _ => 0,
        };

        return delta == 0 ? Task.CompletedTask : NavigateAsync(delta);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_resolveCancellation is not null)
        {
            await _resolveCancellation.CancelAsync();
        }

        _resolveCancellation?.Dispose();

        await _pan.DisposeAsync();
        await _wheelScroll.DisposeAsync();
        await _scroll.DisposeAsync();
        _selfReference?.Dispose();
    }
}
