namespace Forma.Framework.Culture.Blazor.WebView;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

/// <summary>The application's culture coordinator under Blazor Hybrid, which orders every switch across the application's web views and applies each pair it accepts to the application's culture state, the process's default cultures and every web view.</summary>
/// <remarks>
/// Switches are accepted one at a time, each numbered in the sequence of the application's run. The state applies a pair on the
/// switching flow, and each web view then takes it on its own UI thread, in the order the coordinator accepted them, where its
/// document follows it. A pair applied through the coordinator as the integration enters the same sequence, whatever version it
/// came with.
/// </remarks>
internal sealed class ApplicationCultureCoordinator : ICultureStateIntegration
{
    private readonly Lock _lock = new();
    private readonly ICultureStateIntegration _integration;
    private readonly List<WebViewCultureRegistration> _webViews = [];
    private readonly string _documentKey = $"application-{Guid.CreateVersion7()}";
    private int _number;

    /// <summary>Creates the coordinator over the application's culture state, checking that the switch's policy and the culture addresses suit a web view.</summary>
    /// <param name="cultureState">The application's culture state, which is its own integration.</param>
    /// <param name="policy">How a switch persists the pair the application accepts.</param>
    /// <param name="addressing">The culture addresses the web views' culture follows, where culture routing is registered.</param>
    /// <exception cref="InvalidOperationException">The policy persists the pair in a cookie, or culture routing switches by a full load.</exception>
    public ApplicationCultureCoordinator(ICultureState cultureState, CultureSwitchPolicy policy, ICultureAddressing? addressing)
    {
        ArgumentNullException.ThrowIfNull(cultureState);
        ArgumentNullException.ThrowIfNull(policy);

        RequireStatePersistence(policy);
        RequireInPlaceSwitch(addressing);

        CultureState = cultureState;
        _integration = cultureState as ICultureStateIntegration
            ?? throw new InvalidOperationException($"The registered {nameof(ICultureState)}, {cultureState.GetType().Name}, doesn't implement {nameof(ICultureStateIntegration)}.");
        DocumentIntegration = new DocumentPairIntegration(this);
    }

    /// <summary>The application's culture state.</summary>
    public ICultureState CultureState { get; }

    /// <summary>The integration a web view's document applies a pair it accepted through, a landing at another culture's address, which the coordinator takes as a switch.</summary>
    public ICultureStateIntegration DocumentIntegration { get; }

    /// <summary>Rejects a policy that persists the pair in a cookie, which no request carries to a server under Blazor Hybrid.</summary>
    /// <param name="policy">How a switch persists the pair the application accepts.</param>
    /// <exception cref="InvalidOperationException">The policy persists the pair in a cookie.</exception>
    public static void RequireStatePersistence(CultureSwitchPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.Persistence is not CultureSwitchPersistence.State)
        {
            throw new InvalidOperationException("Under Blazor Hybrid no request carries a culture cookie to a server, so a switch persists the pair in the culture state alone and the application keeps the user's choice in its own settings: set CultureSwitchPolicy.Persistence to CultureSwitchPersistence.State.");
        }
    }

    /// <summary>Rejects culture routing that switches by a full load, which restarts a web view's session.</summary>
    /// <param name="addressing">The culture addresses a web view's culture follows, where culture routing is registered.</param>
    /// <exception cref="InvalidOperationException">Culture routing switches by a full load.</exception>
    internal static void RequireInPlaceSwitch(ICultureAddressing? addressing)
    {
        if (addressing?.GetMap("/") is { SwitchesInPlace: false })
        {
            throw new InvalidOperationException("Culture routing under Blazor Hybrid switches in place, since a web view reloads only by restarting its session, which loses the user's work: set CultureRouterOptions.Switch to CultureSwitchMode.InPlace.");
        }
    }

    /// <inheritdoc />
    /// <remarks>The pair enters the application's sequence as a switch does.</remarks>
    public Task ApplyAsync(AcceptedCulture acceptedCulture)
    {
        ArgumentNullException.ThrowIfNull(acceptedCulture);

        return SwitchAsync(acceptedCulture.Culture, acceptedCulture.UICulture);
    }

    /// <summary>Accepts the pair of <paramref name="culture"/> and <paramref name="uiCulture"/> as the application's next switch.</summary>
    /// <returns>A task that completes once every subscriber of the state has been notified.</returns>
    public Task SwitchAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        lock (_lock)
        {
            var revision = CultureState.Revision;
            var notification = _integration.ApplyAsync(new AcceptedCulture(culture, uiCulture, new CultureVersion(_documentKey, ++_number)));

            if (CultureState.Revision == revision)
            {
                return notification;
            }

            CultureInfo.DefaultThreadCurrentCulture = CultureState.Culture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureState.UICulture;

            foreach (var webView in _webViews.ToArray())
            {
                webView.Follow(CultureState.Culture, CultureState.UICulture);
            }

            return notification;
        }
    }

    /// <summary>Registers <paramref name="webView"/>, whose UI thread takes the accepted pair at once and every pair accepted after it.</summary>
    /// <returns>The accepted pair the web view's document starts from.</returns>
    public (CultureInfo Culture, CultureInfo UICulture) Register(WebViewCultureRegistration webView)
    {
        lock (_lock)
        {
            _webViews.Add(webView);
            webView.ApplyToThread(CultureState.Culture, CultureState.UICulture);

            return (CultureState.Culture, CultureState.UICulture);
        }
    }

    /// <summary>Releases <paramref name="webView"/>, leaving the culture state and the accepted pair to the application.</summary>
    public void Release(WebViewCultureRegistration webView)
    {
        lock (_lock)
        {
            _webViews.Remove(webView);
        }
    }

    // A document names its pair alone, so a pair the state holds by name, with formats of its own, stays.
    private Task AcceptDocumentPairAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        lock (_lock)
        {
            return culture.Name == CultureState.Culture.Name && uiCulture.Name == CultureState.UICulture.Name
                ? Task.CompletedTask
                : SwitchAsync(culture, uiCulture);
        }
    }

    /// <summary>Takes a pair a web view's document accepted as the application's next switch.</summary>
    /// <remarks>The pair a document starts from, number zero in its sequence, is the one the coordinator handed it as it registered, which a switch accepted since may have moved past, so it switches nothing.</remarks>
    private sealed class DocumentPairIntegration(ApplicationCultureCoordinator coordinator) : ICultureStateIntegration
    {
        /// <inheritdoc />
        public Task ApplyAsync(AcceptedCulture acceptedCulture)
        {
            ArgumentNullException.ThrowIfNull(acceptedCulture);

            return acceptedCulture.Version.Number == 0 ? Task.CompletedTask : coordinator.AcceptDocumentPairAsync(acceptedCulture.Culture, acceptedCulture.UICulture);
        }
    }
}
