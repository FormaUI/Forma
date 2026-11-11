namespace Forma.Blazor.Components.Files;

using System.Globalization;

using Forma.Blazor.Models;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A viewer that previews the resource a source address resolves to.</summary>
/// <remarks>
/// The providers the host registered are tried in order, and the first to claim the address resolves it; the browser's
/// own provider, which claims what a browser renders unaided, is tried last.
/// </remarks>
public sealed partial class FormaFileViewer : FormaElementBase
{
    private const string ZoomedClass = "zoomed";
    private const string NextAction = "file-viewer.next";
    private const string PreviousAction = "file-viewer.previous";
    private const string ZoomAction = "file-viewer.zoom";
    private const string PanAction = "file-viewer.pan";

    private static readonly int[] ZoomSteps = [100, 150, 200, 300, 400];

    // The arrow along the reading direction steps to the next sibling and the one against it to the previous.
    private static readonly InputProfile LeftToRightProfile = CreateProfile(nextKey: "ArrowRight", previousKey: "ArrowLeft");
    private static readonly InputProfile RightToLeftProfile = CreateProfile(nextKey: "ArrowLeft", previousKey: "ArrowRight");

    private static readonly BrowserResourceProvider BrowserProvider = new();

    /// <summary>The address of the resource to preview.</summary>
    [Parameter, EditorRequired] public string Source { get; set; } = default!;

    /// <summary>The addresses <see cref="Source"/> sits among, which the reader steps through.</summary>
    [Parameter] public IReadOnlyList<string>? Siblings { get; set; }

    /// <summary>The callback invoked with the address in <see cref="Siblings"/> the reader steps to.</summary>
    [Parameter] public EventCallback<string> OnNavigate { get; set; }

    /// <summary>Whether the resource's name and size show beside its preview.</summary>
    [Parameter] public bool ShowMetadata { get; set; }

    /// <summary>Whether a wheel turn with Ctrl held over the image zooms it.</summary>
    [Parameter] public bool ZoomOnWheelGesture { get; set; } = true;

    /// <summary>The template for an unsupported resource, given its address.</summary>
    [Parameter] public RenderFragment<string>? UnsupportedTemplate { get; set; }

    /// <summary>The providers the host registered.</summary>
    [Inject] private IEnumerable<IResourceProvider> RegisteredProviders { get; set; } = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private PanGesture _pan = default!;

    private Scroll _scroll = default!;

    private Wheel _wheel = default!;

    private DotNetObjectReference<FormaFileViewer>? _selfReference;

    private InputRegistration _navigation = default!;

    private ElementReference _imageFrameElement;

    private ResolutionRequest? _resolvedRequest;

    private CancellationTokenSource? _resolveCancellation;

    private bool _isDragAttached;

    private bool _isWheelAttached;

    private readonly ZoomScale _zoom = new(ZoomSteps);

    private bool _hasZoomChanged;

    private ResourceInfo? Resource { get; set; }

    private bool IsNavigable
        => Siblings is { Count: > 1 };

    private bool IsZoomedIn
        => _zoom.IsZoomedIn;

    private int ZoomPercent
        => _zoom.Percent;

    private string? DisplaySize
        => Resource?.FormatSize(Context.CultureState.Culture);

    private string DisplayName
        => Resource?.Name ?? Source;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _pan = new PanGesture(Context.JS);
        _scroll = new Scroll(Context.JS);
        _wheel = new Wheel(Context.JS);
        _navigation = new InputRegistration(InputRouter, HandleNavigationActionAsync);
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);

        return ResolveAsync();
    }

    // Resolves once per address and cultures, since a provider may convert a document into text in the culture it resolves in.
    private async Task ResolveAsync()
    {
        var request = new ResolutionRequest(Source, Context.CultureState.Culture, Context.CultureState.UICulture);

        if (request == _resolvedRequest)
        {
            return;
        }

        var isNewSource = _resolvedRequest?.Source != request.Source;

        _resolvedRequest = request;

        // A new address starts the view afresh; the same one in another culture keeps the reader's zoom and what is shown until its new resolve arrives.
        if (isNewSource)
        {
            _zoom.Reset();
            _isDragAttached = false;
            _isWheelAttached = false;

            // The address alone gives the name and extension while the source resolves or when no provider claims it.
            Resource = new ResourceInfo(request.Source);
        }

        var token = StartResolving();

        try
        {
            // A slower resolve for an address or cultures the reader has already left must not replace the current one.
            if (await ResolveResourceAsync(request.Source, token) is { } resolvedResource && _resolvedRequest == request)
            {
                Resource = resolvedResource;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private CancellationToken StartResolving()
    {
        var abandonedCancellation = _resolveCancellation;

        _resolveCancellation = new CancellationTokenSource();

        abandonedCancellation?.Cancel();
        abandonedCancellation?.Dispose();

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
        // The arrows belong to the viewer only while it can navigate; the registration runs beside the image's behaviors.
        var navigation = _navigation.SyncAsync(RootElement, Context.CultureState.IsRightToLeft ? RightToLeftProfile : LeftToRightProfile, shouldRegister: IsNavigable);

        if (Resource is ImageResourceInfo)
        {
            // The pan and the zoom are routes the scripts declare themselves, so the host's policy reaches the router before they do.
            await InputRouter.ConfigureAsync();

            if (!_isDragAttached)
            {
                // Claimed before the await, so a render arriving mid-call attaches no second pan.
                _isDragAttached = true;
                _isDragAttached = await _pan.AttachAsync(_imageFrameElement, PanAction, ZoomedClass);
            }

            if (ZoomOnWheelGesture && !_isWheelAttached)
            {
                _selfReference ??= DotNetObjectReference.Create(this);

                // Claimed before the await, so a render arriving mid-call attaches no second wheel.
                _isWheelAttached = true;
                _isWheelAttached = await _wheel.AttachAsync(_imageFrameElement, ScrollWheel.Tells(ZoomAction, WheelModifier.Ctrl), _selfReference);
            }
            else if (!ZoomOnWheelGesture && _isWheelAttached)
            {
                _isWheelAttached = false;
                await _wheel.DetachAsync();
            }

            if (_hasZoomChanged)
            {
                _hasZoomChanged = false;
                await _scroll.ToCenterAsync(_imageFrameElement);
            }
        }

        await navigation;

        // A change of culture renders the viewer again without setting a parameter, so a resolution in the cultures it left starts again here.
        if (_resolvedRequest is { } resolvedRequest && (resolvedRequest.Culture != Context.CultureState.Culture || resolvedRequest.UICulture != Context.CultureState.UICulture))
        {
            await ResolveAsync();
            StateHasChanged();
        }
    }

    /// <summary>Zooms a step in for a wheel rolled up and a step out for one rolled down.</summary>
    [JSInvokable]
    public Task HandleWheelAsync(int direction)
        => InvokeAsync(() =>
        {
            _hasZoomChanged |= direction < 0 ? _zoom.TryZoomIn() : _zoom.TryZoomOut();
            StateHasChanged();
        });

    private Task HandleNavigationActionAsync(string action, InputInfo input)
        => action switch
        {
            NextAction => NavigateAsync(1),
            PreviousAction => NavigateAsync(-1),
            _ => Task.CompletedTask,
        };

    private void ZoomIn()
        => _hasZoomChanged |= _zoom.TryZoomIn();

    private void ZoomOut()
        => _hasZoomChanged |= _zoom.TryZoomOut();

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

    private static InputProfile CreateProfile(string nextKey, string previousKey)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(NextAction, new KeyBinding(nextKey)),
                new InputRoute(PreviousAction, new KeyBinding(previousKey)),
            ],
        };

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-file-viewer");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            if (_resolveCancellation is not null)
            {
                await _resolveCancellation.CancelAsync();
            }

            _resolveCancellation?.Dispose();

            await _pan.DisposeAsync();
            await _scroll.DisposeAsync();
            await _wheel.DisposeAsync();
            await _navigation.DisposeAsync();
        }
        finally
        {
            _selfReference?.Dispose();
        }

        await base.DisposeAsyncCore();
    }

    // What a resolve was started for: the address and the cultures it runs in, which the providers convert into.
    private sealed record ResolutionRequest(string Source, CultureInfo Culture, CultureInfo UICulture);
}
