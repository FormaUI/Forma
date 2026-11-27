namespace Forma.Framework.Themes.Browser.Interop;

using System.Runtime.ExceptionServices;

using Forma.Framework.Coordination.Browser.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A user scope's registration with the page's theme coordinator, which keeps its theme state on the choice the page accepted and writes the host's store when the coordinator asks.</summary>
/// <remarks>
/// Each attempt registers the state under the scope's identity, recovering a registration the browser still holds, and the state adopts the
/// choice the coordinator accepted last; the coordinator then delivers each choice it accepts, which the state applies in the order of their
/// versions. A scope with a store writes it for the switches it requested, and for another runtime's where that runtime has gone.
/// </remarks>
public sealed class ThemeCoordinatorRegistration : SwitchCoordinatorRegistration
{
    private const string ModulePath = "./_content/forma/themes/scripts/ThemeCoordinator.js";

    private readonly IThemeStateIntegration _integration;
    private readonly ThemeRegistry _registry;
    private readonly IThemePreferenceStore? _store;
    private readonly DotNetObjectReference<ThemeCoordinatorReceiver> _receiverReference;
    private readonly string _identity = Guid.CreateVersion7().ToString("N");
    private int _requestCount;
    private Exception? _storeFailure;

    /// <summary>Creates the registration of the user scope's theme state, applied through its integration, over the scope's JS runtime.</summary>
    /// <param name="integration">The integration that applies each accepted choice to the scope's theme state.</param>
    /// <param name="registry">The themes registered, which a delivered choice is checked against.</param>
    /// <param name="js">The scope's JS runtime.</param>
    /// <param name="logger">The logger the registration writes what it absorbs to.</param>
    /// <param name="store">The host's store, which the registration writes when the coordinator asks, where the host registered one.</param>
    public ThemeCoordinatorRegistration(IThemeStateIntegration integration, ThemeRegistry registry, IJSRuntime js, ILogger<ThemeCoordinatorRegistration> logger, IThemePreferenceStore? store = null)
        : base("theme", ModulePath, new JSEnvironment(js, logger, Guid.CreateVersion7()))
    {
        ArgumentNullException.ThrowIfNull(integration);
        ArgumentNullException.ThrowIfNull(registry);

        _integration = integration;
        _registry = registry;
        _store = store;
        _receiverReference = DotNetObjectReference.Create(new ThemeCoordinatorReceiver(this));
    }

    /// <summary>Creates the registration of a user scope's theme state from the scope's services, over the JS runtime its platform registered.</summary>
    /// <param name="services">The scope's services.</param>
    /// <returns>The registration.</returns>
    public static ThemeCoordinatorRegistration Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new ThemeCoordinatorRegistration(
            services.GetRequiredService<IThemeStateIntegration>(),
            services.GetRequiredService<ThemeRegistry>(),
            services.GetJSRuntime(),
            services.GetRequiredService<ILogger<ThemeCoordinatorRegistration>>(),
            services.GetService<IThemePreferenceStore>());
    }

    /// <summary>Registers the theme state with the page's coordinator, as each connection to the browser comes up, and adopts the choice the page accepted.</summary>
    /// <returns>A task that completes once the state has adopted the accepted choice, faulted where the registration failed.</returns>
    public Task RegisterAsync()
        => StartRegistrationAsync(module =>
        {
            // A runtime in the browser is a WebAssembly one, whose registration a closing circuit leaves in place.
            var runtime = OperatingSystem.IsBrowser() ? "webassembly" : "server";

            return module.InvokeAsync<IJSObjectReference>("register", _identity, _receiverReference, runtime, _store is not null);
        });

    /// <summary>Requests a switch to <paramref name="choice"/> from the coordinator, completing once it was accepted, the browser's record holds it, this registration's state applied it, and the store holds it or a newer choice.</summary>
    /// <remarks>Each request carries its place among this registration's requests, taken as the request is made, before it waits for the registration, so a request reaching the coordinator after a later one completes as canceled, changing nothing.</remarks>
    /// <exception cref="InvalidOperationException">The choice names a theme that isn't selectable, or the registration is unavailable.</exception>
    /// <exception cref="TaskCanceledException">A switch requested after this one reached the coordinator first.</exception>
    internal async Task RequestAsync(ThemeChoice choice)
    {
        var order = Interlocked.Increment(ref _requestCount);
        var registration = await GetRegistrationAsync().ConfigureAwait(false);
        string outcome;

        try
        {
            outcome = await registration.InvokeAsync<string>("request", choice.Theme, ThemeAttributes.GetSchemeValue(choice.Scheme), order).ConfigureAwait(false);
        }
        catch (JSException) when (Interlocked.Exchange(ref _storeFailure, null) is { } storeFailure)
        {
            ExceptionDispatchInfo.Throw(storeFailure);

            throw;
        }

        if (outcome == "unselectable")
        {
            throw new InvalidOperationException($"The theme '{choice.Theme}' isn't selectable: it isn't registered, or its stylesheet didn't load on the page.");
        }

        if (outcome == "canceled")
        {
            throw new TaskCanceledException($"The switch to the theme '{choice.Theme}' changed nothing: a switch requested after it reached the page's theme coordinator first.");
        }
    }

    /// <summary>Applies the choice of <paramref name="theme"/> in <paramref name="scheme"/>, which the coordinator delivered as number <paramref name="number"/> of the document <paramref name="documentKey"/> names.</summary>
    /// <remarks>A delivery the coordinator couldn't have made, naming no registered theme, no scheme preference or no place in its document's sequence, is refused and logged. The state's subscribers are notified after the delivery completes; a notification that fails is logged.</remarks>
    internal Task ApplyAsync(string? theme, string? scheme, string? documentKey, int number)
    {
        if (IsDisposed)
        {
            return Task.CompletedTask;
        }

        if (theme is null || !_registry.TryGetTheme(theme, out _) || !ThemeAttributes.TryParseScheme(scheme, out var preference) || documentKey is null || number < 0)
        {
            ThemesBrowserInteropLog.ChoiceRefused(JS.Logger, JS.RuntimeId);

            return Task.CompletedTask;
        }

        _ = ObserveAsync(_integration.ApplyAsync(new AcceptedTheme(new ThemeChoice(theme, preference), new ThemeVersion(documentKey, number))));

        return Task.CompletedTask;
    }

    /// <summary>Keeps the choice of <paramref name="theme"/> in <paramref name="scheme"/> in the host's store at <paramref name="revision"/>, as the coordinator asks once the version before it has written.</summary>
    /// <remarks>A store that fails is kept as the failure of the switch this registration requested, and the coordinator hands the failure to every switch waiting on the write.</remarks>
    internal async Task WriteStoreAsync(string? theme, string? scheme, long revision)
    {
        if (_store is null || theme is null || !_registry.TryGetTheme(theme, out _) || !ThemeAttributes.TryParseScheme(scheme, out var preference) || revision < 0)
        {
            ThemesBrowserInteropLog.WriteRefused(JS.Logger, JS.RuntimeId);

            return;
        }

        try
        {
            await _store.WriteAsync(new ThemePreference(new ThemeChoice(theme, preference), revision), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _storeFailure = exception;
            ThemesBrowserInteropLog.StoreWriteFailed(JS.Logger, exception, JS.RuntimeId);

            throw;
        }
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

    private async Task ObserveAsync(Task notification)
    {
        await notification.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (notification.Exception?.InnerException is { } exception)
        {
            ThemesBrowserInteropLog.NotificationFailed(JS.Logger, exception, JS.RuntimeId);
        }
    }
}
