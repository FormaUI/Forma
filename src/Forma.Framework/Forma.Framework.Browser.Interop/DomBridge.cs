namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The browser entry modules one JS runtime scope shares.</summary>
public sealed class DomBridge : IAsyncDisposable, IDisposable
{
    private readonly IJSEnvironment _environment;
    private readonly Dictionary<DomRoot, JSModule> _modules = [];
    private readonly HashSet<DomSession> _liveSessions = [];
    private bool _isDisposed;

    /// <summary>Creates the bridge for one JS runtime scope, with <paramref name="inputRouter"/> as its input router.</summary>
    public DomBridge(IJSRuntime js, ILogger<DomBridge> logger, InputRouter? inputRouter = null)
        : this(new JSEnvironment(js, logger, Guid.CreateVersion7()), inputRouter)
    {
    }

    internal DomBridge(IJSEnvironment environment, InputRouter? inputRouter)
    {
        ArgumentNullException.ThrowIfNull(environment);

        _environment = environment;
        InputRouter = inputRouter;
    }

    /// <summary>The JS runtime scope's input router.</summary>
    internal InputRouter? InputRouter { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var module in _modules.Values)
        {
            await module.DisposeAsync();
        }

        _modules.Clear();
    }

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's modules are released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
    {
        _isDisposed = true;
        _modules.Clear();
    }

    /// <summary>Starts refreshing every established observation of the bridge's sessions, in one call.</summary>
    /// <remarks>
    /// Each observation is named by its owner, token and generation at the start of the refresh, and the browser refreshes nothing for one
    /// released or replaced since. The reply, and the states the refresh delivers, reach a circuit only after the connection that asked for the
    /// refresh has finished coming up.
    /// </remarks>
    internal void BeginRefresh()
    {
        if (_isDisposed)
        {
            return;
        }

        var owners = _liveSessions.Select(session => session.DescribeObservations()).OfType<object>().ToList();

        if (owners.Count > 0)
        {
            _ = RefreshAsync(owners);
        }
    }

    /// <summary>Takes <paramref name="session"/> as one whose registrations have reached the browser through the bridge.</summary>
    internal void Track(DomSession session)
        => _liveSessions.Add(session);

    /// <summary>Forgets <paramref name="session"/>.</summary>
    internal void Untrack(DomSession session)
        => _liveSessions.Remove(session);

    /// <summary>Imports <paramref name="root"/>, sharing an import already made or in flight.</summary>
    /// <exception cref="ObjectDisposedException">The bridge has been disposed.</exception>
    internal Task<IJSObjectReference> ImportAsync(DomRoot root)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_modules.TryGetValue(root, out var module) || module.ImportAsync() is { IsFaulted: true } or { IsCanceled: true })
        {
            module = new JSModule(_environment, root.Path);
            _modules[root] = module;
        }

        return module.ImportAsync();
    }

    private async Task RefreshAsync(List<object> owners)
    {
        const string Operation = "refresh after reconnecting";

        try
        {
            var module = await ImportAsync(DomRoot.Watcher);
            await module.InvokeVoidAsync("refreshAll", owners);
        }
        catch (JSDisconnectedException)
        {
            DomLog.CircuitDropped(_environment.Logger, Operation, DomRoot.Watcher.Name, _environment.RuntimeId);
        }
        catch (Exception ex) when (ex is JSException or OperationCanceledException)
        {
            DomLog.CallFailed(_environment.Logger, ex, Operation, DomRoot.Watcher.Name, _environment.RuntimeId);
        }
        catch (ObjectDisposedException)
        {
            DomLog.BridgeReleased(_environment.Logger, Operation, DomRoot.Watcher.Name, _environment.RuntimeId);
        }
    }
}
