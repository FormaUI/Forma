namespace Forma.Blazor.Components;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A component's context: its identity, its cultures, its strings, its log, its JS environment, and the watcher and engine its browser work goes through.</summary>
/// <remarks>The context owns the watcher and the engine it makes, and its disposal releases them together, in one call to the browser.</remarks>
public sealed class ComponentContext : IComponentContext, IAsyncDisposable
{
    private readonly Lazy<IStringLocale> _locale;
    private readonly Lazy<ILogger> _logger;
    private readonly Lazy<IJSEnvironment> _js;
    private readonly Func<DomBridge?> _domBridge;
    private DomWatcher? _domWatcher;
    private DomEngine? _domEngine;

    /// <summary>Creates a context over the user scope's culture state, from the functions that create its JS runtime, strings and log, and that find the bridge its watcher shares imports through.</summary>
    public ComponentContext(ICultureState cultureState, Func<IJSRuntime> jsRuntime, Func<IStringLocale> locale, Func<ILogger> logger, Func<DomBridge?>? domBridge = null)
    {
        ArgumentNullException.ThrowIfNull(cultureState);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(logger);

        CultureState = cultureState;
        _locale = new(locale);
        _logger = new(logger);
        _js = new(() => new JSEnvironment(jsRuntime(), Logger, RuntimeId));
        _domBridge = domBridge ?? (static () => null);
    }

    /// <inheritdoc />
    public Guid RuntimeId { get; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public ICultureState CultureState { get; }

    /// <inheritdoc />
    public IStringLocale Locale
        => _locale.Value;

    /// <inheritdoc />
    public ILogger Logger
        => _logger.Value;

    /// <inheritdoc />
    public IJSEnvironment JS
        => _js.Value;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
    public DomWatcher DomWatcher
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return _domWatcher ??= new DomWatcher(JS, _domBridge());
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
    public DomEngine DomEngine
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return _domEngine ??= new DomEngine(DomWatcher);
        }
    }

    /// <summary>Whether the context has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        // The watcher's release reaches the engine borrowing it too.
        if (_domWatcher is not null)
        {
            await _domWatcher.DisposeAsync();
        }
    }
}
