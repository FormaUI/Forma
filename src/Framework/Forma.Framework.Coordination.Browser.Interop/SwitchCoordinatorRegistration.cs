namespace Forma.Framework.Coordination.Browser.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>A user scope's registration with one of the page's switch coordinators, which keeps the scope's state of a setting on the value the page accepted.</summary>
/// <remarks>
/// Each attempt registers the state under the scope's identity, recovering a registration the browser still holds, and the state adopts the
/// value the coordinator accepted last; the coordinator then delivers each value it accepts. The registration is registering while an attempt
/// is under way, registered once it completed, and unavailable after it failed or the connection to the browser went down, until the next
/// attempt. A module whose import faulted or was canceled is imported again by the next attempt.
/// </remarks>
public abstract class SwitchCoordinatorRegistration : IAsyncDisposable, IDisposable
{
    private readonly string _setting;
    private readonly string _modulePath;
    private JSModule _module;
    private Task<IJSObjectReference>? _attempt;
    private IJSObjectReference? _registration;
    private volatile bool _isUnavailable;
    private volatile bool _isDisposed;

    /// <summary>Initializes the registration of the setting <paramref name="setting"/> names with the coordinator the module at <paramref name="modulePath"/> holds.</summary>
    /// <param name="setting">The setting's name in the messages the registration writes and throws, as <c>culture</c> or <c>theme</c>.</param>
    /// <param name="modulePath">The coordinator's module.</param>
    /// <param name="js">The scope's JS environment, which carries the log and the runtime id the registration writes under.</param>
    protected SwitchCoordinatorRegistration(string setting, string modulePath, JSEnvironment js)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(setting);
        ArgumentException.ThrowIfNullOrWhiteSpace(modulePath);
        ArgumentNullException.ThrowIfNull(js);

        _setting = setting;
        _modulePath = modulePath;
        JS = js;
        _module = new JSModule(js, modulePath);
    }

    /// <summary>The scope's JS environment.</summary>
    protected JSEnvironment JS { get; }

    /// <summary>Whether the registration has been disposed.</summary>
    protected bool IsDisposed
        => _isDisposed;

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
        GC.SuppressFinalize(this);

        var attempt = _attempt;
        var ending = EndAsync(attempt);

        return attempt is { IsCompleted: false } ? ValueTask.CompletedTask : new ValueTask(ending);
    }

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's registration is released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the registration's managed state, the receiver a derived registration owns among it, where <paramref name="disposing"/>.</summary>
    /// <param name="disposing">Whether the call comes from disposing the registration.</param>
    protected virtual void Dispose(bool disposing)
        => _isDisposed = true;

    /// <summary>Starts an attempt to register the state, which <paramref name="register"/> makes through the coordinator's module.</summary>
    /// <param name="register">Calls the module's registration, returning the browser's registration of the state.</param>
    /// <returns>A task that completes once the state has adopted the accepted value, faulted where the registration failed.</returns>
    protected Task StartRegistrationAsync(Func<JSModule, ValueTask<IJSObjectReference>> register)
    {
        ArgumentNullException.ThrowIfNull(register);

        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        var attempt = RegisterCoreAsync(register);
        _attempt = attempt;
        _isUnavailable = false;

        return attempt;
    }

    /// <summary>The browser's registration of the state, once an attempt under way completed.</summary>
    /// <returns>The registration.</returns>
    /// <exception cref="InvalidOperationException">No attempt is under way, the registration is unavailable or disposed, or it ended before the attempt completed.</exception>
    protected async Task<IJSObjectReference> GetRegistrationAsync()
    {
        if (_attempt is not { } attempt || _isUnavailable || _isDisposed)
        {
            throw new InvalidOperationException($"The {_setting} state isn't registered with the page's {_setting} coordinator, so the switch can't be accepted: a static render has no page to coordinate, and a circuit whose connection is down registers again as it comes back.");
        }

        var registration = await attempt.ConfigureAwait(false);

        return _isUnavailable || _isDisposed
            ? throw new InvalidOperationException($"The {_setting} state's registration with the page's {_setting} coordinator ended before the switch could be requested.")
            : registration;
    }

    /// <summary>Calls <paramref name="identifier"/> on the browser's <paramref name="registration"/>, logging and absorbing a dropped connection, a canceled call and a script that threw.</summary>
    /// <param name="registration">The browser's registration.</param>
    /// <param name="identifier">The function called.</param>
    /// <param name="cancellationToken">Gives up on a document that doesn't answer.</param>
    /// <param name="arguments">The function's arguments.</param>
    /// <returns>Whether the call completed.</returns>
    protected async Task<bool> TryInvokeVoidAsync(IJSObjectReference registration, string identifier, CancellationToken cancellationToken, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(registration);

        try
        {
            await registration.InvokeVoidAsync(identifier, cancellationToken, arguments).ConfigureAwait(false);

            return true;
        }
        catch (JSDisconnectedException)
        {
            CoordinationBrowserInteropLog.CircuitDroppedWhileCalling(JS.Logger, identifier, _setting, JS.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            CoordinationBrowserInteropLog.CallCanceled(JS.Logger, identifier, _setting, JS.RuntimeId);
        }
        catch (JSException ex)
        {
            CoordinationBrowserInteropLog.ScriptThrewWhileCalling(JS.Logger, ex, identifier, _setting, JS.RuntimeId);
        }

        return false;
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
            await TryInvokeVoidAsync(registration, "dispose", CancellationToken.None).ConfigureAwait(false);
            await ReleaseAsync(registration).ConfigureAwait(false);
        }

        ReleaseManagedState();
        await _module.DisposeAsync().ConfigureAwait(false);
    }

    private void ReleaseManagedState()
        => Dispose(disposing: true);

    private async Task<IJSObjectReference> RegisterCoreAsync(Func<JSModule, ValueTask<IJSObjectReference>> register)
    {
        try
        {
            var registration = await register(GetModule()).ConfigureAwait(false);

            if (_registration is { } replacedRegistration && !ReferenceEquals(replacedRegistration, registration))
            {
                await ReleaseAsync(replacedRegistration).ConfigureAwait(false);
            }

            _registration = registration;

            return registration;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException)
        {
            _isUnavailable = true;
            CoordinationBrowserInteropLog.RegistrationFailed(JS.Logger, ex, _setting, JS.RuntimeId);

            throw;
        }
    }

    private JSModule GetModule()
    {
        if (_module.ImportAsync() is { IsFaulted: true } or { IsCanceled: true })
        {
            _module = new JSModule(JS, _modulePath);
        }

        return _module;
    }

    private async Task ReleaseAsync(IJSObjectReference registration)
    {
        try
        {
            await registration.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            CoordinationBrowserInteropLog.CircuitDroppedWhileCalling(JS.Logger, "release", _setting, JS.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            CoordinationBrowserInteropLog.CallCanceled(JS.Logger, "release", _setting, JS.RuntimeId);
        }
        catch (JSException ex)
        {
            CoordinationBrowserInteropLog.ScriptThrewWhileCalling(JS.Logger, ex, "release", _setting, JS.RuntimeId);
        }
    }
}
