namespace Forma.Blazor;

using Forma.Framework.Culture.Blazor.WebView;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Forma in a Blazor Hybrid application.</summary>
public static class WebViewServiceCollectionExtensions
{
    /// <summary>Registers every default the components rely on in a Blazor Hybrid application and the culture's WebView half, leaving in place whatever the application registered first.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Sets the WebView registration's options.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaUIWebView(this IServiceCollection services, Action<UIWebViewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new UIWebViewOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the culture state and the switch are the application's.
        services.AddFormaCultureWebView();

        return services.AddFormaUI(options.InputRouting);
    }
}
