namespace Forma.Framework.Globalization.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers culture routing and adds it to the request pipeline.</summary>
public static class CultureRoutingExtensions
{
    /// <summary>Registers the culture router over <paramref name="options"/>, the switcher each user scope switches through, and the reconciliation of a circuit's culture state with its address.</summary>
    public static IServiceCollection AddCultureRouting(this IServiceCollection services, CultureRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaGlobalizationServices();
        services.TryAddSingleton(new CultureRouter(options));
        services.TryAddScoped<CultureSwitcher>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CultureAddressCircuitHandler>(provider => new CultureAddressCircuitHandler(provider.GetRequiredService<CultureSwitcher>())));

        return services;
    }

    /// <summary>Adds the culture routing middleware to the request pipeline.</summary>
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app)
        => app.UseMiddleware<CultureRoutingMiddleware>();
}
