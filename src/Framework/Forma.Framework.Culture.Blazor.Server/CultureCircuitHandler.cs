namespace Forma.Framework.Culture.Blazor.Server;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Applies a circuit's culture state before every inbound activity, each UI event and each call from script into .NET, and registers it with the page's culture coordinator from each connection.</summary>
/// <remarks>
/// The cultures are set in the activity's own flow, so the work it runs reads them, and the flow that dispatched the activity keeps its own
/// cultures once the activity ends. The first connection's registration completes before the circuit renders, so its components render in
/// the pair the page accepted. A later connection comes up within the reconnection the browser waits on, and the browser answers nothing
/// sent during it until it completes, so that connection's registration runs past it and a switch meanwhile waits for it.
/// </remarks>
internal sealed class CultureCircuitHandler(ICultureState cultureState, CultureCoordinatorRegistration registration) : CircuitHandler
{
    private bool _hasConnected;

    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var registration = RegisterAsync();

        if (_hasConnected)
        {
            return Task.CompletedTask;
        }

        _hasConnected = true;

        return registration;
    }

    /// <inheritdoc />
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        registration.MarkUnavailable();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            CultureInfo.CurrentCulture = cultureState.Culture;
            CultureInfo.CurrentUICulture = cultureState.UICulture;
            await next(context).ConfigureAwait(false);
        };

    // A registration that fails leaves the registration unavailable, which the next switch reports.
    private async Task RegisterAsync()
        => await registration.RegisterAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}
