namespace Forma.Framework.Culture.Blazor.WebView;

using System.ComponentModel;
using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

/// <summary>A web view's registration with the application's culture coordinator, started as the web view's first component initializes: the web view's UI thread takes every pair the coordinator accepts, and its document follows it.</summary>
/// <remarks>
/// The registration holds the synchronization context the first component initialized in, the web view's UI thread, and posts each
/// pair to it with the caller's flow suppressed, so the cultures stay on the thread for every event it dispatches after, and the
/// document follows the pair from there, in the order the coordinator accepted them. The document starts from the accepted pair
/// under a policy that follows the coordinator, as a standalone app's page starts. A web view whose thread no longer takes work,
/// or whose document can't follow or leaves a follow unanswered for a minute, the time Blazor Server gives a call into script by
/// default, leaves the coordinator, as one never disposed after its native window closed does, whose document answers nothing.
/// </remarks>
/// <param name="coordinator">The application's culture coordinator.</param>
/// <param name="documentRegistration">The web view's registration with its document's culture coordinator.</param>
/// <param name="cultureState">The culture state the web view's scope resolves, which is the application's.</param>
/// <param name="policy">How a switch persists the pair the application accepts.</param>
/// <param name="addressing">The culture addresses the web view's culture follows, where culture routing is registered.</param>
/// <param name="navigation">The web view's navigation, which names the application's root.</param>
/// <param name="timeProvider">The clock a follow left unanswered is measured by.</param>
/// <param name="logger">The logger the registration writes what it absorbs to.</param>
internal sealed class WebViewCultureRegistration(
    ApplicationCultureCoordinator coordinator,
    CultureCoordinatorRegistration documentRegistration,
    ICultureState cultureState,
    CultureSwitchPolicy policy,
    ICultureAddressing? addressing,
    NavigationManager navigation,
    TimeProvider timeProvider,
    ILogger<WebViewCultureRegistration> logger) : ICultureScopeStart, IDisposable
{
    private static readonly TimeSpan FollowTimeout = TimeSpan.FromMinutes(1);

    private SynchronizationContext? _context;
    private bool _isStarted;
    private volatile bool _isReleased;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The web view's scope resolves a culture state of its own, where the application holds one for every web view.</exception>
    public void Start()
    {
        if (_isStarted || _isReleased)
        {
            return;
        }

        if (!ReferenceEquals(cultureState, coordinator.CultureState))
        {
            throw new InvalidOperationException($"Each web view resolves a culture state of its own, while under Blazor Hybrid the application holds one culture across its web views: register {nameof(ICultureState)} as a singleton, and register Forma's WebView half before anything that registers the culture state for each scope.");
        }

        _isStarted = true;
        _context = SynchronizationContext.Current;
        var (culture, uiCulture) = coordinator.Register(this);

        _ = RegisterDocumentAsync(new CultureCoordinatorStart
        {
            Marker = CulturePublication.GetMarkerAttributes(culture, uiCulture),
            Policy = CulturePublication.GetPolicyAttributes(policy, addressing?.GetMap(new Uri(navigation.BaseUri).AbsolutePath), follows: true, isWebView: true),
            Direction = uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr",
        });
    }

    /// <summary>Sets the cultures of <paramref name="culture"/> and <paramref name="uiCulture"/> on the web view's UI thread.</summary>
    public void ApplyToThread(CultureInfo culture, CultureInfo uiCulture)
        => Post(() => SetCultures(culture, uiCulture));

    /// <summary>Sets the cultures of <paramref name="culture"/> and <paramref name="uiCulture"/> on the web view's UI thread, and has the web view's document follow them there.</summary>
    public void Follow(CultureInfo culture, CultureInfo uiCulture)
        => Post(() =>
        {
            SetCultures(culture, uiCulture);
            _ = FollowDocumentAsync(culture, uiCulture);
        });

    /// <inheritdoc />
    public void Dispose()
        => Release();

    private static void SetCultures(CultureInfo culture, CultureInfo uiCulture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
    }

    private static void Run(object? action)
        => ((Action)action!)();

    // Without a context of its own, the web view's components run wherever they're dispatched, which the caller's flow stands for.
    private void Post(Action action)
    {
        if (_context is not { } context)
        {
            action();

            return;
        }

        try
        {
            if (ExecutionContext.IsFlowSuppressed())
            {
                context.Post(Run, action);

                return;
            }

            using (ExecutionContext.SuppressFlow())
            {
                context.Post(Run, action);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException or InvalidAsynchronousStateException)
        {
            CultureWebViewLog.ThreadStopped(logger, exception);
            Release();
        }
    }

    // A registration that fails is logged by the document's registration, and the web view's first delivery leaves the coordinator.
    private async Task RegisterDocumentAsync(CultureCoordinatorStart start)
        => await documentRegistration.RegisterAsync(start).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    private async Task FollowDocumentAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        if (_isReleased)
        {
            return;
        }

        // A web view its scope released meanwhile has no document left to follow, which is no failure.
        using var timeout = new CancellationTokenSource(FollowTimeout, timeProvider);
        var isFollowing = await documentRegistration.FollowAsync(culture, uiCulture, timeout.Token).ConfigureAwait(false);

        if (isFollowing || _isReleased)
        {
            return;
        }

        CultureWebViewLog.DocumentDidNotFollow(logger, uiCulture.Name);
        Release();
    }

    private void Release()
    {
        _isReleased = true;
        coordinator.Release(this);
    }
}
