namespace Forma.Blazor.Components;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
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
    [Inject] private ICultureState CultureState { get; set; } = default!;

    private ComponentContext? _context;
    private bool _isSubscribedToCultureState;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The component's context: its identity, its cultures, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => _context ??= new ComponentContext(
            CultureState,
            () => JSRuntime,
            () => LocaleFactory.Create(LocalizationSourceType, CultureState),
            () => LoggerFactory.CreateLogger(GetType()),
            () => Services.GetService<DomBridge>());

    /// <summary>The component's strings, in the culture state's UI culture.</summary>
    protected IStringLocale L
        => Context.Locale;

    /// <inheritdoc />
    /// <remarks>The first call subscribes the component to its culture state, so each change of the state renders it again.</remarks>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_isSubscribedToCultureState)
        {
            _isSubscribedToCultureState = true;
            CultureState.Subscribe(HandleCultureChangedAsync);
        }

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        if (_isSubscribedToCultureState)
        {
            CultureState.Unsubscribe(HandleCultureChangedAsync);
        }

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

    // A component its renderer has disposed takes no further render.
    private Task HandleCultureChangedAsync()
        => IsDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);

    /// <summary>Releases what the component holds.</summary>
    /// <remarks>The base runs it once.</remarks>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
