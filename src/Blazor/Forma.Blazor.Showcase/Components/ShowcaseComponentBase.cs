namespace Forma.Blazor.Showcase.Components;

using Forma.Framework.Culture;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>A Showcase component whose strings follow the culture state, rendered again on each change of the state.</summary>
public abstract class ShowcaseComponentBase : ComponentBase, IDisposable
{
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>The cultures the component writes its strings and formats in.</summary>
    [Inject] protected ICultureState CultureState { get; set; } = default!;

    private IStringLocale? _locale;
    private bool _isSubscribedToCultureState;
    private bool _isDisposed;

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The component's strings, in the culture state's UI culture.</summary>
    protected IStringLocale L
        => _locale ??= LocaleFactory.Create(LocalizationSourceType, CultureState);

    /// <inheritdoc />
    /// <remarks>The first call subscribes the component to its culture state.</remarks>
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
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases what the component holds.</summary>
    /// <param name="disposing">Whether the call comes from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (disposing && _isSubscribedToCultureState)
        {
            CultureState.Unsubscribe(HandleCultureChangedAsync);
        }
    }

    /// <summary>Brings what the component holds of its strings into the culture state's cultures, before the component renders again.</summary>
    /// <returns>The work of bringing them into the state's cultures.</returns>
    protected virtual Task OnCultureChangedAsync()
        => Task.CompletedTask;

    // A component its renderer has disposed takes no further render.
    private Task HandleCultureChangedAsync()
        => _isDisposed ? Task.CompletedTask : InvokeAsync(async () =>
        {
            await OnCultureChangedAsync();
            StateHasChanged();
        });
}
