namespace Forma.Framework.Culture.Blazor.WebView;

using System.Runtime.CompilerServices;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>A web view's registration with its own document's culture coordinator under a culture per web view, started as the web view's first component initializes: the web view's culture state takes each pair its document accepts.</summary>
/// <remarks>
/// The document starts from the state's pair under the switch's policy, as a standalone app's page starts, and orders the web view's
/// switches and landings itself. A registration that fails is logged by the registration, and the web view goes on in the pair it
/// started in.
/// </remarks>
internal sealed class WebViewCultureStart : ICultureScopeStart
{
    // The web view each culture state started, so a state two web views share is told from one each holds.
    private static readonly ConditionalWeakTable<ICultureState, WebViewCultureStart> Starts = [];

    private readonly CultureCoordinatorRegistration _registration;
    private readonly ICultureState _cultureState;
    private readonly CultureSwitchPolicy _policy;
    private readonly ICultureAddressing? _addressing;
    private readonly NavigationManager _navigation;
    private bool _isStarted;

    /// <summary>Creates the web view's start, checking that the switch's policy and the culture addresses suit a web view.</summary>
    /// <param name="registration">The web view's registration with its document's culture coordinator.</param>
    /// <param name="cultureState">The web view's culture state.</param>
    /// <param name="policy">How a switch persists the pair the web view accepts.</param>
    /// <param name="addressing">The culture addresses the web view's culture follows, where culture routing is registered.</param>
    /// <param name="navigation">The web view's navigation, which names the application's root.</param>
    /// <exception cref="InvalidOperationException">The policy persists the pair in a cookie, or culture routing switches by a full load.</exception>
    public WebViewCultureStart(CultureCoordinatorRegistration registration, ICultureState cultureState, CultureSwitchPolicy policy, ICultureAddressing? addressing, NavigationManager navigation)
    {
        ApplicationCultureCoordinator.RequireStatePersistence(policy);
        ApplicationCultureCoordinator.RequireInPlaceSwitch(addressing);

        _registration = registration;
        _cultureState = cultureState;
        _policy = policy;
        _addressing = addressing;
        _navigation = navigation;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Another web view holds the same culture state, where each holds one of its own.</exception>
    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        if (!Starts.TryAdd(_cultureState, this))
        {
            throw new InvalidOperationException($"Two web views resolve the same culture state, while under a culture per web view each holds one of its own: register {nameof(ICultureState)} as scoped, or coordinate the web views' culture across the application.");
        }

        _isStarted = true;

        _ = RegisterAsync(new CultureCoordinatorStart
        {
            Marker = CulturePublication.GetMarkerAttributes(_cultureState.Culture, _cultureState.UICulture),
            Policy = CulturePublication.GetPolicyAttributes(_policy, _addressing?.GetMap(new Uri(_navigation.BaseUri).AbsolutePath), isWebView: true),
            Direction = _cultureState.IsRightToLeft ? "rtl" : "ltr",
        });
    }

    private async Task RegisterAsync(CultureCoordinatorStart start)
        => await _registration.RegisterAsync(start).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}
