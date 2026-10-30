namespace Forma.Framework.Culture.Blazor.WebAssembly;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;

/// <summary>Registers a WebAssembly runtime's culture state with the page's culture coordinator as the runtime starts, before it renders.</summary>
/// <remarks>
/// The runtime's culture services are one per runtime, so it registers the state its components read. The registration carries the runtime's pair and
/// the switch's policy as a server would publish them, which the coordinator starts from on a standalone app's page alone; a page its
/// server rendered starts from the marker the server published. A registration that fails is logged by the registration, and the
/// runtime goes on in the pair it started in.
/// </remarks>
/// <param name="registration">The runtime's registration with the coordinator.</param>
/// <param name="cultureState">The runtime's culture state.</param>
/// <param name="policy">How a switch persists the pair the page accepts.</param>
/// <param name="navigation">The runtime's navigation, which names the application's root.</param>
/// <param name="addressing">The culture addresses the page's culture follows, where culture routing is registered.</param>
internal sealed class CultureCoordinatorStartup(CultureCoordinatorRegistration registration, ICultureState cultureState, CultureSwitchPolicy policy, NavigationManager navigation, ICultureAddressing? addressing) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
        => await registration.RegisterAsync(CreateStart()).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private CultureCoordinatorStart CreateStart()
        => new()
        {
            Marker = CulturePublication.GetMarkerAttributes(cultureState.Culture, cultureState.UICulture),
            Policy = CulturePublication.GetPolicyAttributes(policy, addressing?.GetMap(new Uri(navigation.BaseUri).AbsolutePath)),
            Direction = cultureState.IsRightToLeft ? "rtl" : "ltr",
        };
}
