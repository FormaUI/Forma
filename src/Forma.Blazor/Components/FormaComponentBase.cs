namespace Forma.Blazor.Components;

using Forma.Framework.Core.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>What every Forma component is: it reaches the browser, speaks the reader's language, can say what happened to it, and is disposed once.</summary>
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    private ILogger? _logger;
    private IStringLocale? _locale;
    private IJSEnvironment? _js;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>The component's identity for its lifetime, which the ids of its elements derive from.</summary>
    public Guid RuntimeId { get; } = Guid.CreateVersion7();

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose translation files the component reads: its own, unless it shares a feature's.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The JS environment the component's interop runs in: the runtime, under the component's own log and identity.</summary>
    protected IJSEnvironment JS
        => _js ??= new JSEnvironment(JSRuntime, Logger, RuntimeId);

    /// <summary>The component's log, under its own type as the category.</summary>
    protected ILogger Logger
        => _logger ??= LoggerFactory.CreateLogger(GetType());

    /// <summary>The component's strings, in the reader's culture.</summary>
    protected IStringLocale L
        => _locale ??= LocaleFactory.Create(LocalizationSourceType);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases what the component holds; runs once.</summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
