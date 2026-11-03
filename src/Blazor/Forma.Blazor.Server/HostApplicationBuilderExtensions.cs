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
    /// <summary>Registers every default the components rely on and the circuit halves an interactive server needs, leaving in place whatever the host registered first.</summary>
    /// <remarks>
    /// Under coordinated pages it supplies culture capture on, <c>Components:UseCultureFromServer</c>, as the configuration's lowest-priority
    /// source, so a value the host's configuration names decides; and the host fails to start where that value is false and the server
    /// renders WebAssembly components.
    /// </remarks>
    /// <param name="builder">The host's builder.</param>
    /// <param name="configure">Sets the server registration's options.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHostApplicationBuilder AddFormaUIServer(this IHostApplicationBuilder builder, Action<UIServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new UIServerOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the circuit's switch requests through the page's coordinator.
        builder.Services.AddFormaCultureServer(options.CultureSwitchPolicy);
        builder.Services.AddFormaUI(options.InputRouting);
        builder.Services.AddFormaBrowserServer();

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
