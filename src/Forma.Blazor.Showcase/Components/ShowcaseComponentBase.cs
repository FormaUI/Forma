namespace Forma.Blazor.Showcase.Components;

using Forma.Framework.Culture;

using Microsoft.AspNetCore.Components;

/// <summary>A Showcase component that renders again on each change of the culture state, so its own strings follow a switch made in place.</summary>
public abstract class ShowcaseComponentBase : ComponentBase, IDisposable
{
    [Inject] private ICultureState CultureState { get; set; } = default!;

    private bool _isSubscribedToCultureState;
    private bool _isDisposed;

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

    // A component its renderer has disposed takes no further render.
    private Task HandleCultureChangedAsync()
        => _isDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);
}
