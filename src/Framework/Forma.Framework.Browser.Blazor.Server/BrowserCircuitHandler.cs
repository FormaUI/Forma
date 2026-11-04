namespace Forma.Framework.Browser.Blazor.Server;

using Forma.Framework.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Refreshes a circuit's established observations, and brings its text fields' edits back in step, as its connection to the browser comes up.</summary>
internal sealed class BrowserCircuitHandler(DomBridge bridge) : CircuitHandler
{
    /// <inheritdoc />
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        bridge.BeginRefresh();

        return Task.CompletedTask;
    }
}
