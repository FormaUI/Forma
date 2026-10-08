namespace Forma.Blazor.Components;

using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A component's context: its identity, its strings, its log and its JS environment.</summary>
public sealed class ComponentContext : IComponentContext, IAsyncDisposable
{
    private readonly Lazy<IStringLocale> _locale;
    private readonly Lazy<ILogger> _logger;
    private readonly Lazy<IJSEnvironment> _js;

    /// <summary>Creates a context from the functions that create its JS runtime, strings and log.</summary>
    public ComponentContext(Func<IJSRuntime> jsRuntime, Func<IStringLocale> locale, Func<ILogger> logger)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(logger);

        _locale = new(locale);
        _logger = new(logger);
        _js = new(() => new JSEnvironment(jsRuntime(), Logger, RuntimeId));
    }

    /// <inheritdoc />
    public Guid RuntimeId { get; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public IStringLocale Locale
        => _locale.Value;

    /// <inheritdoc />
    public ILogger Logger
        => _logger.Value;

    /// <inheritdoc />
    public IJSEnvironment JS
        => _js.Value;

    /// <summary>Whether the context has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;

        return ValueTask.CompletedTask;
    }
}
