namespace Forma.Framework.Globalization.AspNetCore;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Reconciles a circuit's culture state with the address it opens at, a resumed circuit's included.</summary>
/// <remarks>
/// A circuit's state starts from its connection's culture, which the culture cookie names for an address that names
/// none, and another tab may have rewritten the cookie; every handler hears the circuit open before any hears its
/// connection come up, so the document culture synchronizer starts on the address's culture.
/// </remarks>
internal sealed class CultureAddressCircuitHandler(CultureSwitcher cultureSwitcher) : CircuitHandler
{
    /// <inheritdoc />
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        => cultureSwitcher.ReconcileAsync();
}
