namespace Forma.Framework.Culture.Browser.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The document's language and direction, kept on a user scope's culture state.</summary>
/// <remarks>
/// Changes reach the browser one reconciliation at a time, each queued behind the last. Each reads the state's revision before its
/// UI culture, which the state publishes the other way round, so the revision a reconciliation records is never newer than the pair
/// it sent; it sends nothing for a revision already applied, and records one only once the browser has the pair. Each connection to
/// the browser asks for a registration under the scope's one identity, which recovers a live registration and replaces one the
/// browser released, and a reconciliation answers only the requests made before it started.
/// </remarks>
public sealed class DocumentCultureSynchronizer : IAsyncDisposable, IDisposable
{
    private const string ModulePath = "./_content/forma/culture/scripts/DocumentCultureSynchronizer.js";

    private readonly ICultureState _cultureState;
    private readonly JSEnvironment _environment;
    private readonly string _identity = Guid.CreateVersion7().ToString("N");
    private readonly Lock _reconciliationLock = new();
    private Task _lastReconciliation = Task.CompletedTask;
    private JSModule _module;
    private IJSObjectReference? _registration;
    private int? _appliedRevision;
    private int _registrationRequests;
    private int _answeredRegistrationRequests;
    private bool _isFollowing;
    private volatile bool _isDisposed;

    /// <summary>Creates the document culture synchronizer for one user scope's <paramref name="cultureState"/>.</summary>
    public DocumentCultureSynchronizer(ICultureState cultureState, IJSRuntime js, ILogger<DocumentCultureSynchronizer> logger)
    {
        ArgumentNullException.ThrowIfNull(cultureState);

        _cultureState = cultureState;
        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _module = new JSModule(_environment, ModulePath);
    }

    /// <summary>Starts keeping the document on the culture state, or brings it in step again as a connection to the browser comes up.</summary>
    public void BeginFollowing()
    {
        if (_isDisposed)
        {
            return;
        }

        if (!_isFollowing)
        {
            _isFollowing = true;
            _cultureState.Subscribe(ReconcileAsync);
        }

        Interlocked.Increment(ref _registrationRequests);
        _ = ReconcileAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _cultureState.Unsubscribe(ReconcileAsync);

        Task lastReconciliation;

        lock (_reconciliationLock)
        {
            lastReconciliation = _lastReconciliation;
        }

        await lastReconciliation.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (_registration is { } registration)
        {
            _registration = null;
            await TryInvokeVoidAsync(registration, "dispose").ConfigureAwait(false);
            await ReleaseAsync(registration).ConfigureAwait(false);
        }

        await _module.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's registration and module are released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _cultureState.Unsubscribe(ReconcileAsync);
    }

    private Task ReconcileAsync()
    {
        lock (_reconciliationLock)
        {
            return _lastReconciliation = ReconcileAfterAsync(_lastReconciliation);
        }
    }

    // A reconciliation that failed leaves the queue to the next, and one queued once disposed sends nothing.
    private async Task ReconcileAfterAsync(Task previousReconciliation)
    {
        await previousReconciliation.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (_isDisposed)
        {
            return;
        }

        try
        {
            await ReconcileOnceAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            CultureBrowserInteropLog.ReconciliationCancelled(_environment.Logger, ex, _environment.RuntimeId);
        }
    }

    private async Task ReconcileOnceAsync()
    {
        var registrationRequests = Volatile.Read(ref _registrationRequests);
        var revision = _cultureState.Revision;
        var uiCulture = _cultureState.UICulture;
        var language = uiCulture.Name;
        var direction = uiCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

        if (_registration is { } registration && registrationRequests == _answeredRegistrationRequests)
        {
            if (_appliedRevision == revision)
            {
                return;
            }

            switch (await TryInvokeAsync<bool>(registration, "update", language, direction).ConfigureAwait(false))
            {
                case null:
                    return;
                case true:
                    _appliedRevision = revision;
                    return;
            }
        }

        var replacement = await GetModule().TryInvokeAsync<IJSObjectReference>("register", _identity, language, direction).ConfigureAwait(false);

        if (replacement is null)
        {
            return;
        }

        if (_registration is { } replaced)
        {
            await ReleaseAsync(replaced).ConfigureAwait(false);
        }

        _registration = replacement;
        _appliedRevision = revision;
        _answeredRegistrationRequests = registrationRequests;
    }

    // A module whose import faulted or was cancelled is replaced, so the next reconciliation imports it again.
    private JSModule GetModule()
    {
        if (_module.ImportAsync() is { IsFaulted: true } or { IsCanceled: true })
        {
            _module = new JSModule(_environment, ModulePath);
        }

        return _module;
    }

    private async Task<TValue?> TryInvokeAsync<TValue>(IJSObjectReference registration, string identifier, params object?[] args)
        where TValue : struct
    {
        try
        {
            return await registration.InvokeAsync<TValue>(identifier, args).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            CultureBrowserInteropLog.CircuitDroppedWhileCalling(_environment.Logger, identifier, _environment.RuntimeId);
        }
        catch (JSException ex)
        {
            CultureBrowserInteropLog.ScriptThrewWhileCalling(_environment.Logger, ex, identifier, _environment.RuntimeId);
        }

        return null;
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
