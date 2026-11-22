namespace Forma.Blazor.Components;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The base of every Forma component: its context, strings, log and JS environment, and its disposal.</summary>
public abstract class FormaComponentBase : ComponentBase, IHandleAfterRender, IAsyncDisposable
{
    [Inject] private IComponentContextFactory ContextFactory { get; set; } = default!;

    private ComponentContext? _context;
    private bool _isSubscribedToCultureState;
    private bool _hasRendered;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocaleSourceType
        => GetType();

    /// <summary>The component's context: its identity, its cultures, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => OwnContext;

    private ComponentContext OwnContext
        => _context ??= ContextFactory.Create(GetType(), () => LocaleSourceType);

    /// <summary>The component's strings, in the culture state's UI culture.</summary>
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

    /// <summary>Runs the component's after-render methods, as the renderer asks after each render in an interactive render mode.</summary>
    /// <remarks>The first also has the user scope's input router carry the host's routes to the page, so a host's route answers on every interactive page that renders a Forma component.</remarks>
    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        var firstRender = !_hasRendered;
        _hasRendered = true;
        OnAfterRender(firstRender);
        var rendered = OnAfterRenderAsync(firstRender);

        return firstRender ? Task.WhenAll(rendered, OwnContext.ConfigureInputRouterAsync()) : rendered;
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
