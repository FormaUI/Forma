namespace Forma.Framework.Browser.Interop.Server;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Refreshes a circuit's established observations as its connection to the browser comes up.</summary>
internal sealed class DomCircuitHandler(DomBridge bridge) : CircuitHandler
{
    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        bridge.BeginRefresh();

        return Task.CompletedTask;
    }
}
