namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>An interaction the browser runs on an element, kept in step with the target and options its owner last gave it.</summary>
/// <typeparam name="TOptions">The options the interaction takes.</typeparam>
public sealed class DomInteraction<TOptions> : IAsyncDisposable, IDomRegistrationHost<TOptions>
    where TOptions : notnull
{
    private readonly DomSession _session;
    private readonly DomRegistration<TOptions> _registration;

    internal DomInteraction(DomSession session, DomCapability<TOptions> capability, bool isEngineDisposed)
    {
        _session = session;
        _registration = new DomRegistration<TOptions>(session, capability, this, isReleased: isEngineDisposed);
    }

    /// <summary>Whether the browser holds the interaction as last requested.</summary>
    public bool IsActive
        => _registration.IsActive;

    /// <summary>Whether the interaction has been disposed.</summary>
    public bool IsDisposed
        => _registration.IsDisposed;

    /// <summary>Brings the interaction in step with what its owner wants: <paramref name="element"/> interacting with <paramref name="options"/> while <paramref name="shouldEnable"/>.</summary>
    /// <remarks>Concurrent calls join one settle, which completes once the latest request has been applied; a request equal to the one attached makes no call.</remarks>
    public Task SyncAsync(ElementReference element, TOptions options, bool shouldEnable = true)
        => _registration.SyncAsync(element, options, shouldEnable);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _registration.DisposeAsync();

    /// <inheritdoc />
    async Task<bool> IDomRegistrationHost<TOptions>.EstablishAsync(ElementReference element, TOptions options, int generation)
    {
        var capability = _registration.Capability;

        // The host's input policy must reach the browser before the attach declares the routes.
        if (capability.DeclaresRoutes(options) && _session.Bridge.InputRouter is { } inputRouter)
        {
            await inputRouter.ConfigureAsync();
        }

        return await _registration.TryInvokeAsync<bool>("attach", createsState: true, _session.RuntimeKey, new DomAttachRequest
        {
            Token = _registration.Token,
            Generation = generation,
            Capability = capability.Name,
            Target = element,
            Options = capability.DescribeOptions(options),
        });
    }

    /// <inheritdoc />
    void IDomRegistrationHost<TOptions>.OnEstablished(int generation)
    {
    }

    /// <inheritdoc />
    void IDomRegistrationHost<TOptions>.OnInvalidated()
    {
    }
}
