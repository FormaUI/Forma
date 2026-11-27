namespace Forma.Framework.Themes.Blazor.WebAssembly;

using Forma.Framework.Themes.Browser.Interop;

using Microsoft.Extensions.Hosting;

/// <summary>Registers a WebAssembly runtime's theme state with the page's theme coordinator as the runtime starts, before it renders.</summary>
/// <remarks>The runtime's theme services are one per runtime, so it registers the state its components read. A registration that fails is logged by the registration, and the runtime goes on in the choice it started in.</remarks>
/// <param name="registration">The runtime's registration with the coordinator.</param>
internal sealed class ThemeCoordinatorStartup(ThemeCoordinatorRegistration registration) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
        => await registration.RegisterAsync().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
