namespace Forma.Framework.Culture.Router;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers culture routing.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture router over <paramref name="options"/> as the page's culture addressing, and the switcher each user scope switches through over the culture services the product's registrar registers.</summary>
    /// <exception cref="ArgumentException">The options register no route, a default that isn't a route's token, a token claimed twice or containing a slash, or an hreflang that isn't a language with an optional script and region.</exception>
    public static IServiceCollection AddFormaCultureRouting(this IServiceCollection services, CultureRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.TryAddSingleton(new CultureRouter(options));
        services.TryAddSingleton<ICultureAddressing>(provider => provider.GetRequiredService<CultureRouter>());
        services.TryAddScoped<CultureSwitcher>();

        return services;
    }
}
