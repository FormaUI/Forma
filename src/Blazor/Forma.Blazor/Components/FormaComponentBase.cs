namespace Forma.Blazor.Components;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The base of every Forma component: its context, strings, log and JS environment, and its disposal.</summary>
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IComponentContextFactory ContextFactory { get; set; } = default!;

    private ComponentContext? _context;
    private bool _isSubscribedToCultureState;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocaleSourceType
        => GetType();

    /// <summary>The component's context: its identity, its cultures, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => _context ??= ContextFactory.Create(GetType(), () => LocaleSourceType);

    /// <summary>The component's strings, in the culture state's UI culture.</summary>
    [LocaleSource(nameof(LocaleSourceType))]
    protected IStringLocale L
        => Context.Locale;

    /// <inheritdoc />
    /// <remarks>The first call makes the component's context, which initializes the user scope's culture support where its render mode does not, and subscribes the component to the context's culture state, so each change of the state renders it again.</remarks>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_isSubscribedToCultureState)
        {
            _isSubscribedToCultureState = true;
            Context.CultureState.Subscribe(HandleCultureChangedAsync);
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
            Context.CultureState.Unsubscribe(HandleCultureChangedAsync);
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
