namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The browser entry modules of one JS runtime scope, imported once each and shared by every watcher and engine made through it.</summary>
/// <remarks>Concurrent owners share an import in flight. An import that failed is dropped, so the next owner to need the entry imports it again. Disposal refuses later imports and releases each entry's reference, one arriving late included.</remarks>
public sealed class DomBridge : IAsyncDisposable, IDisposable
{
    private readonly IJSEnvironment _environment;
    private readonly Dictionary<DomRoot, JSModule> _modules = [];
    private readonly HashSet<DomSession> _liveSessions = [];
    private bool _isDisposed;

    /// <summary>Creates the bridge for one JS runtime scope, configuring routed interactions through <paramref name="inputRouter"/>.</summary>
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

    /// <summary>The router whose policy reaches the browser before an interaction declares a route.</summary>
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

    /// <summary>Releases the managed state alone, refusing later imports.</summary>
    public void Dispose()
    {
        _isDisposed = true;
        _modules.Clear();
    }

    /// <summary>Starts asking the browser, in one call, to measure afresh and deliver anew every established observation of the sessions that reached it through the bridge, as after its connection to the browser was restored.</summary>
    /// <remarks>
    /// Each observation is named by its owner, token and current generation, taken when the refresh starts, so one released or replaced since
    /// makes nothing in the browser, and nothing is called while no session holds an established observation. The call is started rather than
    /// awaited: a circuit handles one invocation from the browser at a time, so the reply, and the states the refresh delivers, wait for the
    /// connection that asked for the refresh to finish coming up. A failed call is logged under the bridge.
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

    /// <summary>Forgets a session once it has been released.</summary>
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
