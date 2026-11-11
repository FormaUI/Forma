namespace Forma.Blazor;

using Forma.Framework.Browser.Blazor.Server;
using Forma.Framework.Culture;
using Forma.Framework.Culture.Blazor.Server;

using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>Registers Forma on a server host.</summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>Registers Forma UI services and integrates them with the server host.</summary>
    /// <remarks>
    /// Whatever the host registered first stays in place. It registers the circuit halves an interactive server needs. Under coordinated pages it supplies culture capture on, <c>Components:UseCultureFromServer</c>, as the configuration's lowest-priority
    /// source, so a value the host's configuration names decides; and the host fails to start where that value is false and the server
    /// renders WebAssembly components.
    /// </remarks>
    /// <param name="builder">The host's builder.</param>
    /// <param name="configure">Sets the server integration's options.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHostApplicationBuilder AddFormaServerIntegration(this IHostApplicationBuilder builder, Action<ServerIntegrationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new ServerIntegrationOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the circuit's switch requests through the page's coordinator.
        builder.Services.AddFormaCultureServerIntegration(options.CultureSwitchPolicy);
        builder.Services.AddFormaUIServices(options.InputRouting);
        builder.Services.AddFormaBrowserServerIntegration();

        if (options.CultureArrangement is CultureArrangement.Coordinated)
        {
            builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource { InitialData = [new(CultureCaptureCheck.Setting, "true")] });
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CultureCaptureCheck>(provider => new CultureCaptureCheck(
                provider.GetRequiredService<IConfiguration>(),
                provider.GetServices<RenderModeEndpointProvider>())));
        }

        return builder;
    }
}
