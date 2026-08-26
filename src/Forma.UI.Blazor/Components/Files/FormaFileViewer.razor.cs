namespace Forma.UI.Blazor.Components.Files;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;
using Forma.UI.Blazor.Resources;
using Forma.UI.Features.Files;
using Forma.UI.Features.Files.Providers;

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

    /// <summary>Replaces the built-in fallback shown when no provider claims the address.</summary>
    [Parameter] public RenderFragment<string>? UnsupportedTemplate { get; set; }

    [Inject] private IStringLocale<FormaFileViewer> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Providers the host registered, tried before the browser's own handling.</summary>
    [Inject] private IEnumerable<IResourceProvider> RegisteredProviders { get; set; } = default!;

    private PanGesture _pan = default!;

    private Scroll _scroll = default!;

    private ElementReference _imageFrame;

    private string? _resolvedSource;

    private CancellationTokenSource? _resolving;

    private bool _dragAttached;

    private int _zoomIndex;

    private bool _zoomChanged;

    private ResourceInfo? Resource { get; set; }

    private bool IsNavigable
        => Siblings is { Count: > 1 };

    private bool IsZoomed
        => _zoomIndex > 0;

    private int ZoomPercent
        => ZoomSteps[_zoomIndex];

    private string? DisplaySize
        => Resource?.DisplaySize;

    private string DisplayName
        => Resource?.Name ?? Source;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _pan = new PanGesture(JS);
        _scroll = new Scroll(JS);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);

        // Resolving can cost a provider a fetch and a conversion, so it happens per address rather than per render.
        if (_resolvedSource == Source)
        {
            return;
        }

        var requested = Source;

        _zoomIndex = 0;
        _dragAttached = false;
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
        var abandoned = _resolving;

        _resolving = new CancellationTokenSource();

        abandoned?.Cancel();
        abandoned?.Dispose();

        return _resolving.Token;
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
            _dragAttached = await _pan.AttachAsync(_imageFrame, ZoomedClass);
        }

        if (_zoomChanged)
        {
            _zoomChanged = false;
            await _scroll.ToCenterAsync(_imageFrame);
        }
    }

    private void ZoomIn()
        => SetZoomIndex(_zoomIndex + 1);

    private void ZoomOut()
        => SetZoomIndex(_zoomIndex - 1);

    private void SetZoomIndex(int index)
    {
        if (index < 0 || index >= ZoomSteps.Length || index == _zoomIndex)
        {
            return;
        }

        _zoomIndex = index;
        _zoomChanged = true;
    }

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
        if (_resolving is not null)
        {
            await _resolving.CancelAsync();
        }

        _resolving?.Dispose();

        await _pan.DisposeAsync();
        await _scroll.DisposeAsync();
    }
}
