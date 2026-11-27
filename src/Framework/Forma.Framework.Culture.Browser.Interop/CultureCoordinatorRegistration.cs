namespace Forma.Framework.Culture.Browser.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Coordination.Browser.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A user scope's registration with the page's culture coordinator, which keeps its culture state on the pair the page accepted.</summary>
/// <remarks>
/// Each attempt registers the state under the scope's identity, recovering a registration the browser still holds, and the state adopts the
/// pair the coordinator accepted last; the coordinator then delivers each pair it accepts, which the state applies in the order of their
/// versions. The registration is registering while an attempt is under way, registered once it completed, and unavailable after it failed
/// or the connection to the browser went down, until the next attempt.
/// </remarks>
public sealed class CultureCoordinatorRegistration : SwitchCoordinatorRegistration
{
    private const string ModulePath = "./_content/forma/culture/scripts/CultureCoordinator.js";

    // The longest locale name the platforms hold, LOCALE_NAME_MAX_LENGTH.
    private const int MaximumCultureNameLength = 85;

    private readonly ICultureStateIntegration _integration;
    private readonly ICultureProvider? _cultureProvider;
    private readonly ICultureDestinationSource? _destinationSource;
    private readonly DotNetObjectReference<CultureCoordinatorReceiver> _receiverReference;
    private readonly string _identity = Guid.CreateVersion7().ToString("N");
    private int _requestCount;
    private int _landingCount;
    private int _followCount;

    /// <summary>Creates the registration of the user scope's culture state, applied through its integration, over the scope's JS runtime.</summary>
    /// <param name="integration">The integration that applies each accepted pair to the scope's culture state.</param>
    /// <param name="js">The scope's JS runtime.</param>
    /// <param name="logger">The logger the registration writes what it absorbs to.</param>
    /// <param name="cultureProvider">The application's culture provider, which gives the culture for each name the coordinator delivers, where the application registered one.</param>
    /// <param name="destinationSource">The destinations a switch navigates to, where pages have addresses of their own in each culture.</param>
    public CultureCoordinatorRegistration(ICultureStateIntegration integration, IJSRuntime js, ILogger<CultureCoordinatorRegistration> logger, ICultureProvider? cultureProvider = null, ICultureDestinationSource? destinationSource = null)
        : base("culture", ModulePath, new JSEnvironment(js, logger, Guid.CreateVersion7()))
    {
        ArgumentNullException.ThrowIfNull(integration);

        _integration = integration;
        _cultureProvider = cultureProvider;
        _destinationSource = destinationSource;
        _receiverReference = DotNetObjectReference.Create(new CultureCoordinatorReceiver(this));
    }

    /// <summary>Creates the registration of a user scope's culture state from the scope's services, over the JS runtime its platform registered.</summary>
    /// <param name="services">The scope's services.</param>
    /// <returns>The registration.</returns>
    public static CultureCoordinatorRegistration Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new CultureCoordinatorRegistration(
            services.GetRequiredService<ICultureStateIntegration>(),
            services.GetJSRuntime(),
            services.GetRequiredService<ILogger<CultureCoordinatorRegistration>>(),
            services.GetService<ICultureProvider>(),
            services.GetService<ICultureDestinationSource>());
    }

    /// <summary>Registers the culture state with the page's coordinator, as each connection to the browser comes up, and adopts the pair the page accepted.</summary>
    /// <param name="start">What the coordinator starts from where no server published the page's culture marker, as on a standalone app's page; a page whose server published it starts from its own.</param>
    /// <returns>A task that completes once the state has adopted the accepted pair, faulted where the registration failed.</returns>
    public Task RegisterAsync(CultureCoordinatorStart? start = null)
        => StartRegistrationAsync(module =>
        {
            // A runtime in the browser is a WebAssembly one, whose registration a closing circuit leaves in place.
            var runtime = OperatingSystem.IsBrowser() ? "webassembly" : "server";

            return module.InvokeAsync<IJSObjectReference>("register", _identity, _receiverReference, runtime, start, _destinationSource?.ResolvesDestinations ?? false);
        });

    /// <summary>Requests a switch to <paramref name="culture"/> and <paramref name="uiCulture"/> from the coordinator, completing once the coordinator accepted it and delivered it to this registration's state.</summary>
    /// <remarks>Where culture addresses decide the culture, the switch is a navigation to the pair's address: a navigation canceled or replaced by another leaves it canceled, and one that didn't land within the culture routing's switch timeout, as one a navigation handler refused doesn't, leaves it timed out. Each request carries its place among this registration's requests, taken as the request is made, before it waits for the registration, so a request reaching the coordinator after a later one leaves it canceled, changing nothing.</remarks>
    internal async Task RequestAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        var order = Interlocked.Increment(ref _requestCount);
        var registration = await GetRegistrationAsync().ConfigureAwait(false);
        var direction = uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
        var outcome = await registration.InvokeAsync<string>("request", culture.Name, uiCulture.Name, direction, order).ConfigureAwait(false);

        if (outcome == "canceled")
        {
            throw new TaskCanceledException($"The switch to the culture '{culture.Name}' with the UI culture '{uiCulture.Name}' changed nothing: a switch requested after it reached the page's culture coordinator first, or the navigation it made didn't land.");
        }

        if (outcome == "timed-out")
        {
            throw new TimeoutException($"The switch to the culture '{culture.Name}' with the UI culture '{uiCulture.Name}' navigated, but the navigation didn't land within the culture routing's switch timeout; a navigation handler may have refused it. The page still follows the navigation should it land.");
        }
    }

    /// <summary>The address, relative to the application's root and with its culture segment, the page at <paramref name="relativeAddress"/> leads to in the route <paramref name="token"/> names, or <see langword="null"/> for that page's own address there.</summary>
    /// <remarks>A source that throws leaves the page's own address in the route, so the switch still navigates.</remarks>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    internal async Task<string?> ResolveDestinationAsync(string? relativeAddress, string? token)
    {
        if (_destinationSource is not { ResolvesDestinations: true } destinationSource)
        {
            return null;
        }

        if (relativeAddress is null || token is null)
        {
            CultureBrowserInteropLog.DestinationRefused(JS.Logger, JS.RuntimeId);

            return null;
        }

        try
        {
            return await destinationSource.ResolveAsync(relativeAddress, token, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CultureBrowserInteropLog.DestinationNotResolved(JS.Logger, ex, JS.RuntimeId);

            return null;
        }
    }

    /// <summary>Has the page's document follow the pair of <paramref name="culture"/> and <paramref name="uiCulture"/> an application's coordinator accepted, on a page whose document follows one, as a web view's does.</summary>
    /// <remarks>The document takes the pair's language and direction, and under the address policy the page's address moves into the pair's route, in place of the current history entry and under the mark a navigation handler reads. Each follow carries its place among this registration's follows, taken as the follow is made, before it waits for the registration, so a follow reaching the document after a later one takes nothing.</remarks>
    /// <param name="culture">The culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction.</param>
    /// <param name="cancellationToken">Gives up on a document that doesn't answer.</param>
    /// <returns>Whether the document took the pair; it can't once the registration ended, its connection to the browser went down, or the token gave up on it.</returns>
    public async Task<bool> FollowAsync(CultureInfo culture, CultureInfo uiCulture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        var order = Interlocked.Increment(ref _followCount);
        IJSObjectReference registration;

        try
        {
            registration = await GetRegistrationAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
        {
            return false;
        }

        return await TryInvokeVoidAsync(registration, "follow", cancellationToken, culture.Name, uiCulture.Name, uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr", order).ConfigureAwait(false);
    }

    /// <summary>Reports the interactive landing at the culture route <paramref name="token"/> names, or at a plain address where it is <see langword="null"/>, completing once the coordinator accepted it and this registration's state applied the page's pair.</summary>
    /// <remarks>Each report carries its place among this registration's reports, taken as the report is made, before it waits for the registration, so a report reaching the coordinator after a later one accepts nothing.</remarks>
    public async Task ReportLandingAsync(string? token)
    {
        var order = Interlocked.Increment(ref _landingCount);
        var registration = await GetRegistrationAsync().ConfigureAwait(false);

        await registration.InvokeVoidAsync("reportLanding", token, order).ConfigureAwait(false);
    }

    /// <summary>Applies the pair of the cultures <paramref name="cultureName"/> and <paramref name="uiCultureName"/> name, which the coordinator delivered as number <paramref name="number"/> of the document <paramref name="documentKey"/> names, completing once the state holds it.</summary>
    /// <remarks>Each culture is the application's culture provider's for its name, or the platform's where there is no provider; a provider that fails the delivery is logged, since the coordinator hands the failure to the switch that requested the pair alone. A delivery the coordinator couldn't have made, naming no culture, a culture outside the application's cultures or no place in its document's sequence, is refused and logged, a culture outside them before the provider is asked for it. The state's subscribers are notified after the delivery completes, so what waits on the delivery, a switch or a page's landing, goes on in the new pair as the rest of the page renders again; a notification that fails is logged.</remarks>
    /// <exception cref="InvalidOperationException">The culture provider returned no culture for a name, or a culture under another name.</exception>
    internal Task ApplyAsync(string? cultureName, string? uiCultureName, string? documentKey, int number)
    {
        if (IsDisposed)
        {
            return Task.CompletedTask;
        }

        if (!IsCultureName(cultureName) || !IsCultureName(uiCultureName) || documentKey is null || number < 0)
        {
            CultureBrowserInteropLog.PairRefused(JS.Logger, JS.RuntimeId);

            return Task.CompletedTask;
        }

        if (!IsApplicationCulture(cultureName) || !IsApplicationCulture(uiCultureName))
        {
            CultureBrowserInteropLog.CultureRefused(JS.Logger, JS.RuntimeId);

            return Task.CompletedTask;
        }

        AcceptedCulture acceptedCulture;

        try
        {
            acceptedCulture = new AcceptedCulture(CultureResolution.GetCulture(_cultureProvider, cultureName), CultureResolution.GetCulture(_cultureProvider, uiCultureName), new CultureVersion(documentKey, number));
        }
        catch (Exception ex) when (_cultureProvider is not null)
        {
            CultureBrowserInteropLog.CultureNotProvided(JS.Logger, ex, JS.RuntimeId);

            throw;
        }

        _ = ObserveAsync(_integration.ApplyAsync(acceptedCulture));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _receiverReference.Dispose();
        }

        base.Dispose(disposing);
    }

    // A culture's name is ASCII letters, digits, hyphens and underscores, as BCP 47 tags and .NET's sort suffixes are, and no longer than a locale name.
    private static bool IsCultureName([NotNullWhen(true)] string? name)
        => name is { Length: <= MaximumCultureNameLength } && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    // Every culture an application uses keeps the name of a culture the platform defines, so a name the platform defines no culture by, one
    // it would make up, is outside the application's cultures.
    private static bool IsApplicationCulture(string name)
    {
        try
        {
            return string.Equals(CultureInfo.GetCultureInfo(name, predefinedOnly: true).Name, name, StringComparison.OrdinalIgnoreCase);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private async Task ObserveAsync(Task notification)
    {
        await notification.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (notification.Exception?.InnerException is { } exception)
        {
            CultureBrowserInteropLog.NotificationFailed(JS.Logger, exception, JS.RuntimeId);
        }
    }
}
