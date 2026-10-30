namespace Forma.Framework.Culture.Blazor.Server;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>Applies a circuit's culture state before every inbound activity, each UI event and each call from script into .NET, and registers it with the page's culture coordinator from each connection.</summary>
/// <remarks>
/// The cultures are set in the activity's own flow, so the work it runs reads them, and the flow that dispatched the activity keeps its own
/// cultures once the activity ends. A connection's registration completes before the circuit renders, so its components render in the pair
/// the page accepted.
/// </remarks>
internal sealed class CultureCircuitHandler(ICultureState cultureState, CultureCoordinatorRegistration registration) : CircuitHandler
{
    /// <inheritdoc />
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        => await registration.RegisterAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

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
}
