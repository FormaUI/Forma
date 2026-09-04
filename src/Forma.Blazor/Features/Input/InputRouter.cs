namespace Forma.Blazor.Features.Input;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The circuit's input router: carries the host's policy and routes to the script once, and registers the scopes components own.</summary>
[SuppressMessage("Performance", "CA1812", Justification = "Constructed by dependency injection.")]
internal sealed class InputRouter(IJSRuntime js, InputRouterOptions options, IInputActionHandler? hostHandler = null) : IAsyncDisposable, IDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Blazor/scripts/services/InputRouter.js");
    private DotNetObjectReference<InputRouter>? _selfReference;
    private Task<bool>? _configuration;
    private InputRegistration? _hostRegistration;

    /// <summary>Registers <paramref name="element"/> as a scope, or adds to the scope it already is, yielding <see langword="null"/> when the circuit cannot register it; the <paramref name="owner"/> receives actions through <c>HandleInputActionAsync</c> and dismissals through <c>HandleDismissAsync</c>.</summary>
    public async ValueTask<InputRegistration?> RegisterAsync<TOwner>(ElementReference element, InputProfile profile, DotNetObjectReference<TOwner>? owner = null)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!await ConfigureAsync())
        {
            return null;
        }

        var handle = await _module.TryInvokeAsync<IJSObjectReference>("register", element, profile, owner);

        return handle is null ? null : new InputRegistration(handle);
    }

    /// <summary>Performs a host route's action through the host's handler.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
        => hostHandler?.HandleAsync(action, input) ?? Task.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_hostRegistration is { } hostRegistration)
            {
                await hostRegistration.DisposeAsync();
            }

            await _module.DisposeAsync();
        }
        finally
        {
            _selfReference?.Dispose();
        }
    }

    /// <summary>Releases the managed state alone; a scope disposing synchronously has no JS runtime left to talk to.</summary>
    public void Dispose()
        => _selfReference?.Dispose();

    // A push that failed is retried by the next registration.
    private async Task<bool> ConfigureAsync()
    {
        var configuration = _configuration ??= PushAsync();
        var configured = await configuration;

        if (!configured && ReferenceEquals(_configuration, configuration))
        {
            _configuration = null;
        }

        return configured;
    }

    private async Task<bool> PushAsync()
    {
        if (!await _module.TryInvokeVoidAsync("configure", options))
        {
            return false;
        }

        if (options.Routes.Count == 0 && options.PreemptiveRoutes.Count == 0)
        {
            return true;
        }

        if (hostHandler is null)
        {
            throw new InvalidOperationException($"{nameof(InputRouterOptions)} declares host routes, but no {nameof(IInputActionHandler)} is registered to perform them.");
        }

        _selfReference ??= DotNetObjectReference.Create(this);
        var handle = await _module.TryInvokeAsync<IJSObjectReference>("registerHost", new { options.Routes, options.PreemptiveRoutes }, _selfReference);
        _hostRegistration = handle is null ? null : new InputRegistration(handle);

        return handle is not null;
    }
}
