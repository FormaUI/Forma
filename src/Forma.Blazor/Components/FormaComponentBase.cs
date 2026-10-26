namespace Forma.Blazor.Components;

using System.Globalization;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Globalization;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The base of every Forma component: its context, strings, log and JS environment, and its disposal.</summary>
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>The cultures the component writes its strings, formats and direction in.</summary>
    [Inject] protected ICultureState CultureState { get; set; } = default!;

    private ComponentContext? _context;

    /// <summary>The culture a host cascades where it changes the culture without a new request.</summary>
    /// <remarks>
    /// Each render of the cascading value renders the component again, even one whose parameters are unchanged, so its
    /// strings and formats follow a new culture; a host therefore cascades it from where it renders only when the culture
    /// changes, such as its root. The component writes in the culture in force, which the host sets; this only tells it
    /// when that changed, and is <see langword="null"/> where the host cascades none.
    /// </remarks>
    [CascadingParameter] protected CultureInfo? CascadedCulture { get; set; }

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The component's context: its identity, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => _context ??= new ComponentContext(
            () => JSRuntime,
            () => LocaleFactory.Create(LocalizationSourceType, CultureState),
            () => LoggerFactory.CreateLogger(GetType()),
            () => Services.GetService<DomBridge>());

    /// <summary>The component's strings, in the culture state's UI culture.</summary>
    protected IStringLocale L
        => Context.Locale;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        try
        {
            await DisposeAsyncCore();
        }
        finally
        {
            // The component's own cleanup can still use its context, and a component that never read one has none to release.
            if (_context is not null)
            {
                await _context.DisposeAsync();
            }
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Releases what the component holds.</summary>
    /// <remarks>The base runs it once.</remarks>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
