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
}
