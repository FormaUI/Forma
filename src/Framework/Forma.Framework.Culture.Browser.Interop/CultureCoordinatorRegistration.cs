namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

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
public sealed class CultureCoordinatorRegistration : IAsyncDisposable, IDisposable
{
    private const string ModulePath = "./_content/forma/culture/scripts/CultureCoordinator.js";

    private readonly ICultureStateIntegration _integration;
    private readonly ICultureProvider? _cultureProvider;
    private readonly DotNetObjectReference<CultureCoordinatorReceiver> _receiverReference;
    private readonly JSEnvironment _environment;
    private readonly string _identity = Guid.CreateVersion7().ToString("N");
    private JSModule _module;
    private Task<IJSObjectReference>? _attempt;
    private int _landings;
    private IJSObjectReference? _registration;
    private volatile bool _isUnavailable;
    private volatile bool _isDisposed;

    /// <summary>Creates the registration of the user scope's culture state, applied through its integration, over the scope's JS runtime.</summary>
    /// <param name="integration">The integration that applies each accepted pair to the scope's culture state.</param>
    /// <param name="js">The scope's JS runtime.</param>
    /// <param name="logger">The logger the registration writes what it absorbs to.</param>
    /// <param name="cultureProvider">The application's culture provider, which gives the culture for each name the coordinator delivers, where the application registered one.</param>
    public CultureCoordinatorRegistration(ICultureStateIntegration integration, IJSRuntime js, ILogger<CultureCoordinatorRegistration> logger, ICultureProvider? cultureProvider = null)
    {
        ArgumentNullException.ThrowIfNull(integration);

        _integration = integration;
        _cultureProvider = cultureProvider;
        _receiverReference = DotNetObjectReference.Create(new CultureCoordinatorReceiver(this));
        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _module = new JSModule(_environment, ModulePath);
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
            services.GetService<ICultureProvider>());
    }

    /// <summary>Registers the culture state with the page's coordinator, as each connection to the browser comes up, and adopts the pair the page accepted.</summary>
    /// <param name="start">What the coordinator starts from where no server published the page's culture marker, as on a standalone app's page; a page whose server published it starts from its own.</param>
    /// <returns>A task that completes once the state has adopted the accepted pair, faulted where the registration failed.</returns>
    public Task RegisterAsync(CultureCoordinatorStart? start = null)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var attempt = RegisterCoreAsync(start);
        _attempt = attempt;
        _isUnavailable = false;

        return attempt;
    }

    /// <summary>Marks the registration unavailable, as the connection to the browser goes down, so a switch fails at once until it registers again.</summary>
    public void MarkUnavailable()
        => _isUnavailable = true;

    /// <inheritdoc />
    /// <remarks>An attempt still under way finishes before the browser's registration is released, which disposing doesn't wait for, so a runtime that never answers the attempt, as a web view being torn down answers none, can't hold disposal open.</remarks>
    public ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return ValueTask.CompletedTask;
        }

        _isDisposed = true;

        var attempt = _attempt;
        var ending = EndAsync(attempt);

        return attempt is { IsCompleted: false } ? ValueTask.CompletedTask : new ValueTask(ending);
    }

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's registration is released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
    {
        _isDisposed = true;
        _receiverReference.Dispose();
    }

    /// <summary>Requests a switch to <paramref name="culture"/> and <paramref name="uiCulture"/> from the coordinator, completing once the coordinator accepted it and delivered it to this registration's state.</summary>
    /// <remarks>Where culture addresses decide the culture, the switch is a navigation to the pair's address: a navigation canceled or replaced by another leaves it canceled, and one that didn't land within the culture routing's switch timeout, as one a navigation handler refused doesn't, leaves it timed out.</remarks>
    internal async Task RequestAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        var registration = await GetRegistrationAsync().ConfigureAwait(false);
        var direction = uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
        var outcome = await registration.InvokeAsync<string>("request", culture.Name, uiCulture.Name, direction).ConfigureAwait(false);

        if (outcome == "canceled")
        {
            throw new TaskCanceledException($"The switch to the culture '{culture.Name}' with the UI culture '{uiCulture.Name}' navigated, but the navigation didn't land.");
        }

        if (outcome == "timed-out")
        {
            throw new TimeoutException($"The switch to the culture '{culture.Name}' with the UI culture '{uiCulture.Name}' navigated, but the navigation didn't land within the culture routing's switch timeout; a navigation handler may have refused it. The page still follows the navigation should it land.");
        }
    }

    /// <summary>Has the page's document follow the pair of <paramref name="culture"/> and <paramref name="uiCulture"/> an application's coordinator accepted, on a page whose document follows one, as a web view's does.</summary>
    /// <remarks>The document takes the pair's language and direction, and under the address policy the page's address moves into the pair's route, in place of the current history entry and under the mark a navigation handler reads.</remarks>
    /// <param name="culture">The culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction.</param>
    /// <param name="cancellationToken">Gives up on a document that doesn't answer.</param>
    /// <returns>Whether the document took the pair; it can't once the registration ended, its connection to the browser went down, or the token gave up on it.</returns>
    public async Task<bool> FollowAsync(CultureInfo culture, CultureInfo uiCulture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        IJSObjectReference registration;

        try
        {
            registration = await GetRegistrationAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
        {
            return false;
        }

        try
        {
            await registration.InvokeVoidAsync("follow", cancellationToken, culture.Name, uiCulture.Name, uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr").ConfigureAwait(false);

            return true;
        }
        catch (JSDisconnectedException)
        {
            CultureBrowserInteropLog.CircuitDroppedWhileCalling(_environment.Logger, "follow", _environment.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            CultureBrowserInteropLog.CallCanceled(_environment.Logger, "follow", _environment.RuntimeId);
        }
        catch (JSException ex)
        {
            CultureBrowserInteropLog.ScriptThrewWhileCalling(_environment.Logger, ex, "follow", _environment.RuntimeId);
        }

        return false;
    }

    /// <summary>Reports the interactive landing at the culture route <paramref name="token"/> names, or at a plain address where it is <see langword="null"/>, completing once the coordinator accepted it and this registration's state applied the page's pair.</summary>
    /// <remarks>Each report carries its place among this registration's reports, so a report reaching the coordinator after a later one accepts nothing.</remarks>
    public async Task ReportLandingAsync(string? token)
    {
        var registration = await GetRegistrationAsync().ConfigureAwait(false);
        var order = Interlocked.Increment(ref _landings);

        await registration.InvokeVoidAsync("reportLanding", token, order).ConfigureAwait(false);
    }

    /// <summary>Applies the pair of the cultures <paramref name="cultureName"/> and <paramref name="uiCultureName"/> name, which the coordinator delivered under <paramref name="version"/>, completing once the state holds it.</summary>
    /// <remarks>Each culture is the application's culture provider's for its name, or the platform's where there is no provider; a provider that fails the delivery is logged, since the coordinator hands the failure to the switch that requested the pair alone. The state's subscribers are notified after the delivery completes, so what waits on the delivery, a switch or a page's landing, goes on in the new pair as the rest of the page renders again; a notification that fails is logged.</remarks>
    /// <exception cref="InvalidOperationException">The culture provider returned no culture for a name, or a culture under another name.</exception>
    internal Task ApplyAsync(string cultureName, string uiCultureName, CultureVersion version)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        AcceptedCulture accepted;

        try
        {
            accepted = new AcceptedCulture(CultureResolution.GetCulture(_cultureProvider, cultureName), CultureResolution.GetCulture(_cultureProvider, uiCultureName), version);
        }
        catch (Exception ex) when (_cultureProvider is not null)
        {
            CultureBrowserInteropLog.CultureNotProvided(_environment.Logger, ex, cultureName, uiCultureName, _environment.RuntimeId);

            throw;
        }

        _ = ObserveAsync(_integration.ApplyAsync(accepted), accepted);

        return Task.CompletedTask;
    }

    private async Task ObserveAsync(Task notification, AcceptedCulture accepted)
    {
        await notification.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (notification.Exception?.InnerException is { } exception)
        {
            CultureBrowserInteropLog.NotificationFailed(_environment.Logger, exception, accepted.UICulture.Name, _environment.RuntimeId);
        }
    }

    // The browser's registration is released once an attempt under way finished, then the receiver and the module.
    private async Task EndAsync(Task<IJSObjectReference>? attempt)
    {
        if (attempt is not null)
        {
            await ((Task)attempt).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        if (_registration is { } registration)
        {
            _registration = null;
            await TryInvokeVoidAsync(registration, "dispose").ConfigureAwait(false);
            await ReleaseAsync(registration).ConfigureAwait(false);
        }

        _receiverReference.Dispose();
        await _module.DisposeAsync().ConfigureAwait(false);
    }

    // A switch waits on a registration under way and fails with it; with none under way, or one unavailable, it fails at once.
    private async Task<IJSObjectReference> GetRegistrationAsync()
    {
        if (_attempt is not { } attempt || _isUnavailable || _isDisposed)
        {
            throw new InvalidOperationException("The culture state isn't registered with the page's culture coordinator, so the switch can't be accepted: a static render has no page to coordinate, and a circuit whose connection is down registers again as it comes back.");
        }

        var registration = await attempt.ConfigureAwait(false);

        return _isUnavailable || _isDisposed
            ? throw new InvalidOperationException("The culture state's registration with the page's culture coordinator ended before the switch could be requested.")
            : registration;
    }

    private async Task<IJSObjectReference> RegisterCoreAsync(CultureCoordinatorStart? start)
    {
        try
        {
            // A runtime in the browser is a WebAssembly one, whose registration a closing circuit leaves in place.
            var runtime = OperatingSystem.IsBrowser() ? "webassembly" : "server";
            var registration = await GetModule().InvokeAsync<IJSObjectReference>("register", _identity, _receiverReference, runtime, start).ConfigureAwait(false);

            if (_registration is { } replaced && !ReferenceEquals(replaced, registration))
            {
                await ReleaseAsync(replaced).ConfigureAwait(false);
            }

            _registration = registration;

            return registration;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException)
        {
            _isUnavailable = true;
            CultureBrowserInteropLog.RegistrationFailed(_environment.Logger, ex, _environment.RuntimeId);

            throw;
        }
    }

    // A module whose import faulted or was canceled is replaced, so the next registration imports it again.
    private JSModule GetModule()
    {
        if (_module.ImportAsync() is { IsFaulted: true } or { IsCanceled: true })
        {
            _module = new JSModule(_environment, ModulePath);
        }

        return _module;
    }

    private async Task TryInvokeVoidAsync(IJSObjectReference registration, string identifier)
    {
        try
        {
            await registration.InvokeVoidAsync(identifier).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            CultureBrowserInteropLog.CircuitDroppedWhileCalling(_environment.Logger, identifier, _environment.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            CultureBrowserInteropLog.CallCanceled(_environment.Logger, identifier, _environment.RuntimeId);
        }
        catch (JSException ex)
        {
            CultureBrowserInteropLog.ScriptThrewWhileCalling(_environment.Logger, ex, identifier, _environment.RuntimeId);
        }
    }

    private async Task ReleaseAsync(IJSObjectReference registration)
    {
        try
        {
            await registration.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            CultureBrowserInteropLog.CircuitDroppedWhileCalling(_environment.Logger, "release", _environment.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            CultureBrowserInteropLog.CallCanceled(_environment.Logger, "release", _environment.RuntimeId);
        }
        catch (JSException ex)
        {
            CultureBrowserInteropLog.ScriptThrewWhileCalling(_environment.Logger, ex, "release", _environment.RuntimeId);
        }
    }
}

