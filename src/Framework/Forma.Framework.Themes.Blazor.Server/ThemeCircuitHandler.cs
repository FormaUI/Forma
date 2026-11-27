namespace Forma.Framework.Themes.Blazor.Server;

using Forma.Framework.Themes.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Registers a circuit's theme state with the page's theme coordinator from each connection.</summary>
/// <remarks>
/// The first connection's registration completes before the circuit renders, so its components render in the choice the page accepted. A
/// later connection comes up within the reconnection the browser waits on, so its registration runs past it and a switch meanwhile waits for
/// it.
/// </remarks>
internal sealed class ThemeCircuitHandler(ThemeCoordinatorRegistration registration) : CircuitHandler
{
    private bool _hasConnected;

    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var registering = RegisterAsync();

        if (_hasConnected)
        {
            return Task.CompletedTask;
        }

        _hasConnected = true;

        return registering;
    }

    /// <inheritdoc />
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        registration.MarkUnavailable();

        return Task.CompletedTask;
    }

    // A registration that fails leaves the registration unavailable, which the next switch reports.
    private async Task RegisterAsync()
        => await registration.RegisterAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}
