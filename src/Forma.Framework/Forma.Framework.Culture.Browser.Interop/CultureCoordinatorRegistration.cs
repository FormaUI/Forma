namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

using Forma.Framework.JavaScript.Interop;

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
    private readonly DotNetObjectReference<CultureCoordinatorReceiver> _receiverReference;
    private readonly JSEnvironment _environment;
    private readonly string _identity = Guid.CreateVersion7().ToString("N");
    private JSModule _module;
    private Task<IJSObjectReference>? _attempt;
    private IJSObjectReference? _registration;
    private volatile bool _isUnavailable;
    private volatile bool _isDisposed;

    /// <summary>Creates the registration of the user scope's culture state, applied through its integration, over the scope's JS runtime.</summary>
    /// <param name="integration">The integration that applies each accepted pair to the scope's culture state.</param>
    /// <param name="js">The scope's JS runtime.</param>
    /// <param name="logger">The logger the registration writes what it absorbs to.</param>
    public CultureCoordinatorRegistration(ICultureStateIntegration integration, IJSRuntime js, ILogger<CultureCoordinatorRegistration> logger)
    {
        ArgumentNullException.ThrowIfNull(integration);

        _integration = integration;
        _receiverReference = DotNetObjectReference.Create(new CultureCoordinatorReceiver(this));
        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _module = new JSModule(_environment, ModulePath);
    }

    /// <summary>Registers the culture state with the page's coordinator, as each connection to the browser comes up, and adopts the pair the page accepted.</summary>
    /// <returns>A task that completes once the state has adopted the accepted pair, faulted where the registration failed.</returns>
    public Task RegisterAsync()
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var attempt = RegisterCoreAsync();
        _attempt = attempt;
        _isUnavailable = false;

        return attempt;
    }

    /// <summary>Marks the registration unavailable, as the connection to the browser goes down, so a switch fails at once until it registers again.</summary>
    public void MarkUnavailable()
        => _isUnavailable = true;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_attempt is { } attempt)
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

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's registration is released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
    {
        _isDisposed = true;
        _receiverReference.Dispose();
    }

    /// <summary>Requests a switch to <paramref name="culture"/> and <paramref name="uiCulture"/> from the coordinator, completing once the coordinator accepted it and delivered it to this registration's state.</summary>
    internal async Task RequestAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        var registration = await GetRegistrationAsync().ConfigureAwait(false);
        var direction = uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

        await registration.InvokeVoidAsync("request", culture.Name, uiCulture.Name, direction).ConfigureAwait(false);
    }

    /// <summary>Applies a pair the coordinator delivered.</summary>
    internal Task ApplyAsync(AcceptedCulture accepted)
        => _isDisposed ? Task.CompletedTask : _integration.ApplyAsync(accepted);

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

    private async Task<IJSObjectReference> RegisterCoreAsync()
    {
        try
        {
            var registration = await GetModule().InvokeAsync<IJSObjectReference>("register", _identity, _receiverReference).ConfigureAwait(false);

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

    // A module whose import faulted or was cancelled is replaced, so the next registration imports it again.
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
        catch (JSException ex)
        {
            CultureBrowserInteropLog.ScriptThrewWhileCalling(_environment.Logger, ex, "release", _environment.RuntimeId);
        }
    }
}

