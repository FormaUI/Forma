namespace Forma.Blazor;

using Forma.Framework.Culture;

using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Rejects, as the host starts, culture capture turned off on a server that renders WebAssembly components under coordinated pages.</summary>
/// <remarks>
/// A coordinated prerendered page starts each WebAssembly runtime in the pair the server rendered it in, which the framework hands the
/// client only while capture is on. The check reads the final configuration, so the order of the registrations doesn't change its result.
/// </remarks>
/// <param name="configuration">The host's configuration.</param>
/// <param name="providers">The render modes' endpoint providers, which name the render modes the server renders.</param>
internal sealed class CultureCaptureCheck(IConfiguration configuration, IEnumerable<RenderModeEndpointProvider> providers) : IHostedService
{
    /// <summary>The setting that hands the server's culture to a WebAssembly client.</summary>
    public const string Setting = "Components:UseCultureFromServer";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Capture is off and the server renders WebAssembly components.</exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var value = configuration[Setting];

        if ((string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0") && providers.Any(provider => provider.Supports(RenderMode.InteractiveWebAssembly)))
        {
            throw new InvalidOperationException(
                $"{Setting} is false, but the server renders WebAssembly components under the coordinated culture arrangement, which starts each WebAssembly runtime in the pair the server rendered it in and so needs the server to hand it over. "
                + $"Remove the false, or declare the client's culture independent with {nameof(CultureArrangement)}.{nameof(CultureArrangement.Independent)} on both AddFormaServerIntegration and AddFormaWebAssemblyIntegration.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
