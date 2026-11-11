namespace Forma.Blazor;

using Forma.Framework.Culture.Blazor.WebView;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Forma in a Blazor Hybrid application.</summary>
public static class WebViewServiceCollectionExtensions
{
    /// <summary>Registers Forma UI services and integrates them with the Blazor Hybrid host.</summary>
    /// <remarks>Whatever the application registered first stays in place. It registers the culture's WebView half.</remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Sets the WebView integration's options.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaWebViewIntegration(this IServiceCollection services, Action<WebViewIntegrationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new WebViewIntegrationOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the culture state and the switch are the application's.
        services.AddFormaCultureWebViewIntegration(options.CultureArrangement);

        return services.AddFormaUIServices(options.InputRouting);
    }
}
