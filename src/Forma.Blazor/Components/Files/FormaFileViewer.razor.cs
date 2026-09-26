namespace Forma.Blazor.Components.Files;

using System.Globalization;

using Forma.Blazor.Components.Interaction;
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

/// <summary>Renders whatever resource a source address resolves to, through the first provider that claims it.</summary>
public sealed partial class FormaFileViewer : FormaElementBase
{
    private const string ZoomedClass = "zoomed";
    private const string NextAction = "file-viewer.next";
    private const string PreviousAction = "file-viewer.previous";

    private static readonly int[] ZoomSteps = [100, 150, 200, 300, 400];

    // The arrow along the reading direction steps to the next sibling and the one against it to the previous.
    private static readonly InputProfile LeftToRightProfile = CreateProfile(next: "ArrowRight", previous: "ArrowLeft");
    private static readonly InputProfile RightToLeftProfile = CreateProfile(next: "ArrowLeft", previous: "ArrowRight");

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

    /// <summary>Providers the host registered, tried before the browser's own handling.</summary>
    [Inject] private IEnumerable<IResourceProvider> RegisteredProviders { get; set; } = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private PanGesture _pan = default!;

    private Scroll _scroll = default!;

    private Wheel _wheel = default!;

    private DotNetObjectReference<FormaFileViewer>? _selfReference;

    private InputScope? _navigationScope;

    private Task? _navigationRegistration;

    private bool _isNavigationRegistered;

    private ElementReference _imageFrameElement;

    private string? _resolvedSource;

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
        => Resource?.DisplaySize;

    private string DisplayName
        => Resource?.Name ?? Source;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _pan = new PanGesture(Context.JS);
        _scroll = new Scroll(Context.JS);
        _wheel = new Wheel(Context.JS);
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
        _isDragAttached = false;
        _isWheelAttached = false;
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
        // The registration and the image's behaviors are independent, so they are made side by side.
        var navigation = SyncNavigationScopeAsync();

        if (Resource is ImageResourceInfo)
        {
            if (!_isDragAttached)
            {
                _isDragAttached = await _pan.AttachAsync(_imageFrameElement, ZoomedClass);
            }

            // Ctrl with the wheel zooms the image.
            if (ZoomOnWheelGesture && !_isWheelAttached)
            {
                _selfReference ??= DotNetObjectReference.Create(this);
                _isWheelAttached = await _wheel.AttachAsync(_imageFrameElement, ScrollWheel.Tells(WheelModifier.Ctrl), _selfReference);
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
    }

    /// <summary>Zooms a step in for a wheel rolled up and a step out for one rolled down.</summary>
    [JSInvokable]
    public Task HandleWheelAsync(int direction)
        => InvokeAsync(() =>
        {
            _hasZoomChanged |= direction < 0 ? _zoom.TryZoomIn() : _zoom.TryZoomOut();
            StateHasChanged();
        });

    /// <summary>Steps to the sibling the router's action names.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return action switch
        {
            NextAction => NavigateAsync(1),
            PreviousAction => NavigateAsync(-1),
            _ => Task.CompletedTask,
        };
    }

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

    // The viewer owns the arrows only while they have somewhere to go, so a lone image keeps them for scrolling its frame.
    // A registration the router refused is not asked for again until the siblings change.
    private Task SyncNavigationScopeAsync()
    {
        if (IsNavigable && !_isNavigationRegistered)
        {
            _isNavigationRegistered = true;
            _selfReference ??= DotNetObjectReference.Create(this);
            _navigationRegistration = RegisterNavigationAsync(CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeftProfile : LeftToRightProfile);

            return _navigationRegistration;
        }

        if (!IsNavigable && _isNavigationRegistered)
        {
            _isNavigationRegistered = false;

            return ReleaseNavigationScopeAsync();
        }

        return Task.CompletedTask;
    }

    private async Task RegisterNavigationAsync(InputProfile profile)
    {
        var navigationScope = await InputRouter.RegisterUnlessDisposedAsync(RootElement, profile, _selfReference!, () => IsDisposed);

        if (navigationScope is null)
        {
            return;
        }

        // The siblings may have gone, or the viewer with them, while the registration was being made.
        if (!_isNavigationRegistered || IsDisposed)
        {
            await navigationScope.DisposeAsync();
            return;
        }

        _navigationScope = navigationScope;
    }

    private async Task ReleaseNavigationScopeAsync()
    {
        if (_navigationScope is { } navigationScope)
        {
            _navigationScope = null;
            await navigationScope.DisposeAsync();
        }
    }

    private static InputProfile CreateProfile(string next, string previous)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(NextAction, new KeyBinding(next)),
                new InputRoute(PreviousAction, new KeyBinding(previous)),
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

            // A registration still in flight settles before the reference it carries goes.
            if (_navigationRegistration is { } navigationRegistration)
            {
                await Task.WhenAny(navigationRegistration);
            }

            await ReleaseNavigationScopeAsync();
        }
        finally
        {
            _selfReference?.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
