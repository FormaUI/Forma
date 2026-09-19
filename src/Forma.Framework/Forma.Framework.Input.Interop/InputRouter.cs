namespace Forma.Framework.Input.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The circuit's input router: carries the host's policy and routes to the script once, and registers the scopes components own.</summary>
public sealed class InputRouter : IAsyncDisposable, IDisposable
{
    private readonly JSEnvironment _environment;
    private readonly JSModule _module;
    private readonly InputRouterOptions _options;
    private readonly IInputActionHandler? _hostHandler;
    private DotNetObjectReference<InputRouter>? _selfReference;
    private Task<bool>? _configuration;
    private InputScope? _hostScope;

    /// <summary>Creates the router for one circuit, carrying <paramref name="options"/> and performing host routes through <paramref name="hostHandler"/>.</summary>
    public InputRouter(IJSRuntime js, ILogger<InputRouter> logger, InputRouterOptions options, IInputActionHandler? hostHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
        _module = new JSModule(_environment, "./_content/forma/input/scripts/InputRouter.js");
        _options = options;
        _hostHandler = hostHandler;
    }

    /// <summary>Registers <paramref name="element"/> as a scope, or adds to the scope it already is, yielding <see langword="null"/> when the circuit cannot register it; the <paramref name="owner"/> receives actions through <c>HandleInputActionAsync</c> and dismissals through <c>HandleDismissAsync</c>, the scope stands inside <paramref name="parent"/> for active ordering, and the <paramref name="within"/> elements count as inside it.</summary>
    public async ValueTask<InputScope?> RegisterAsync<TOwner>(ElementReference element, InputProfile profile, DotNetObjectReference<TOwner>? owner = null, ElementReference? parent = null, IReadOnlyList<ElementReference>? within = null)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!await ConfigureAsync())
        {
            return null;
        }

        var handle = await _module.TryInvokeAsync<IJSObjectReference>("register", element, profile, owner, new { parent, within = within ?? [] });

        return handle is null ? null : new InputScope(handle, _environment);
    }

    /// <summary>Performs a host route's action through the host's handler.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
        => _hostHandler?.HandleAsync(action, input) ?? Task.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_hostScope is { } hostScope)
            {
                await hostScope.DisposeAsync();
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
        if (!await _module.TryInvokeVoidAsync("configure", _options))
        {
            return false;
        }

        if (_options.Routes.Count == 0 && _options.PreemptiveRoutes.Count == 0)
        {
            return true;
        }

        if (_hostHandler is null)
        {
            throw new InvalidOperationException($"{nameof(InputRouterOptions)} declares host routes, but no {nameof(IInputActionHandler)} is registered to perform them.");
        }

        _selfReference ??= DotNetObjectReference.Create(this);
        var handle = await _module.TryInvokeAsync<IJSObjectReference>("registerHost", new { _options.Routes, _options.PreemptiveRoutes }, _selfReference);
        _hostScope = handle is null ? null : new InputScope(handle, _environment);

        return handle is not null;
    }
}
