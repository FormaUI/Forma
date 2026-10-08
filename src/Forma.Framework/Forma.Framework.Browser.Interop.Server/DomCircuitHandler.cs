namespace Forma.Framework.Browser.Interop.Server;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Refreshes the observations a circuit's bridge holds whenever the circuit's connection to the browser comes up.</summary>
/// <remarks>
/// The connection comes up once as the circuit starts, when nothing has been observed yet and the refresh makes no call, and again after each
/// reconnection. The refresh is started, not awaited, since the circuit answers the browser only once the connection has finished coming up.
/// </remarks>
internal sealed class DomCircuitHandler(DomBridge bridge) : CircuitHandler
{
    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        bridge.BeginRefresh();

        return Task.CompletedTask;
    }
}
