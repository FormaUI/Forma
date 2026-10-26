namespace Forma.Framework.Globalization.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers culture routing and adds it to the request pipeline.</summary>
public static class CultureRoutingExtensions
{
    /// <summary>Registers the culture router over <paramref name="options"/> and the switcher each user scope switches through.</summary>
    public static IServiceCollection AddCultureRouting(this IServiceCollection services, CultureRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaGlobalizationServices();
        services.TryAddSingleton(new CultureRouter(options));
        services.TryAddScoped<CultureSwitcher>();

        return services;
    }

    /// <summary>Adds the culture routing middleware to the request pipeline.</summary>
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app)
        => app.UseMiddleware<CultureRoutingMiddleware>();
}
