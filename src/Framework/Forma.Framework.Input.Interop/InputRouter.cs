namespace Forma.Framework.Input.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Input.Drop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The circuit's input router, which carries the host's policy and routes to the script and registers the scopes components own.</summary>
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

    /// <summary>The environment the router and the scopes it registers log under.</summary>
    internal IJSEnvironment Environment
        => _environment;

    /// <summary>Registers <paramref name="profile"/> on <paramref name="element"/>'s scope, inside <paramref name="parent"/> and with the <paramref name="within"/> elements counted as inside it, for <paramref name="owner"/> to receive actions through <c>HandleInputActionAsync</c> and dismissals through <c>HandleDismissAsync</c>.</summary>
    public async ValueTask<InputScope?> RegisterAsync<TOwner>(ElementReference element, InputProfile profile, DotNetObjectReference<TOwner>? owner = null, ElementReference? parent = null, IReadOnlyList<ElementReference>? within = null)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!await ConfigureAsync())
        {
            return null;
        }

        var handle = await _module.TryInvokeAsync<IJSObjectReference>("register", element, profile, owner, new ScopePlacement { Parent = parent, Within = within ?? [] });

        return handle is null ? null : new InputScope(handle, _environment);
    }

    /// <summary>Carries the host's policy and routes to the script, returning whether the script has them.</summary>
    /// <remarks>One push is held for the circuit, so concurrent callers share it; a push that failed is dropped and made again by the next call.</remarks>
    public async Task<bool> ConfigureAsync()
    {
        var configuration = _configuration ??= PushAsync();
        var isConfigured = await configuration;

        if (!isConfigured && ReferenceEquals(_configuration, configuration))
        {
            _configuration = null;
        }

        return isConfigured;
    }

    /// <summary>Performs a host route's action through the host's handler.</summary>
    /// <remarks>An action no host route names, or one carrying an input that can't be read, is refused and logged rather than performed.</remarks>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        if ((!IsDeclared(_options.Routes, action) && !IsDeclared(_options.PreemptiveRoutes, action)) || !IsWellFormed(input))
        {
            InputInteropLog.HostActionRefused(_environment.Logger, _environment.RuntimeId);

            return Task.CompletedTask;
        }

        return _hostHandler?.HandleAsync(action, input) ?? Task.CompletedTask;
    }

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

    /// <summary>Releases the managed state alone.</summary>
    public void Dispose()
        => _selfReference?.Dispose();

    /// <summary>Whether one of <paramref name="routes"/> names <paramref name="action"/>.</summary>
    internal static bool IsDeclared(IReadOnlyList<InputRoute>? routes, string? action)
        => action is not null && routes is not null && routes.Any(route => route.Action == action);

    /// <summary>Whether <paramref name="input"/>, as the browser described it, is one an action can read: of a known family, with what its family carries, and every coordinate finite.</summary>
    internal static bool IsWellFormed(InputInfo? input)
        => input switch
        {
            null or { Type: null } => false,
            InputKeyInfo key => key.Key is not null && key.Modifiers is not null && double.IsFinite(key.Timestamp),
            InputPointerInfo pointer => pointer.Modifiers is not null && Enum.IsDefined(pointer.PointerType) && double.IsFinite(pointer.ClientX) && double.IsFinite(pointer.ClientY),
            InputWheelInfo wheel => wheel.Modifiers is not null && double.IsFinite(wheel.DeltaX) && double.IsFinite(wheel.DeltaY),
            InputDropInfo drop => drop.Data is not null && drop.Data.All(kind => kind is not null) && double.IsFinite(drop.ClientX) && double.IsFinite(drop.ClientY),
            _ => false,
        };

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
        var handle = await _module.TryInvokeAsync<IJSObjectReference>("registerHost", new HostProfile { Routes = _options.Routes, PreemptiveRoutes = _options.PreemptiveRoutes }, _selfReference);
        _hostScope = handle is null ? null : new InputScope(handle, _environment);

        return handle is not null;
    }

    /// <summary>Where a scope stands: inside its parent, with the elements counted as inside it.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record ScopePlacement
    {
        public required ElementReference? Parent { get; init; }

        public required IReadOnlyList<ElementReference> Within { get; init; }
    }

    /// <summary>The host's profile: its routes, and those that take an input before any scope does.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record HostProfile
    {
        public required IReadOnlyList<InputRoute> Routes { get; init; }

        public required IReadOnlyList<InputRoute> PreemptiveRoutes { get; init; }
    }
}
