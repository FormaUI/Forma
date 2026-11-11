namespace Forma.Blazor;

using Forma.Framework.Culture.Blazor.WebAssembly;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Forma on a WebAssembly client.</summary>
public static class WebAssemblyServiceCollectionExtensions
{
    /// <summary>Registers Forma UI services and integrates them with the WebAssembly host.</summary>
    /// <remarks>Whatever the client registered first stays in place. It registers the culture's WebAssembly half.</remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Sets the WebAssembly integration's options.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaWebAssemblyIntegration(this IServiceCollection services, Action<WebAssemblyIntegrationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new WebAssemblyIntegrationOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the runtime's switch is the arrangement's.
        services.AddFormaCultureWebAssemblyIntegration(options.CultureArrangement, options.CultureSwitchPolicy);

        return services.AddFormaUIServices(options.InputRouting);
    }
}
