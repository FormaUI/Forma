namespace Forma.Framework.Globalization.AspNetCore;

using System.Globalization;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Applies a circuit's culture state before every inbound activity, each UI event and each call from script into .NET.</summary>
/// <remarks>
/// The cultures are set in the activity's own flow, so the work it runs reads them, and the flow that dispatched the
/// activity keeps its own cultures once the activity ends.
/// </remarks>
internal sealed class CultureCircuitHandler(ICultureState cultureState) : CircuitHandler
{
    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            CultureInfo.CurrentCulture = cultureState.Culture;
            CultureInfo.CurrentUICulture = cultureState.UICulture;

            await next(context).ConfigureAwait(false);
        };
}
