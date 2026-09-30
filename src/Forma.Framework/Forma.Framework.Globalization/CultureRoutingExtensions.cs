namespace Forma.Framework.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers culture routing and adds it to the request pipeline.</summary>
public static class CultureRoutingExtensions
{
    /// <summary>Registers the culture router over <paramref name="options"/>.</summary>
    public static IServiceCollection AddCultureRouting(this IServiceCollection services, CultureRouterOptions options)
        => services.AddSingleton(new CultureRouter(options));

    /// <summary>Adds the culture routing middleware to the request pipeline.</summary>
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app)
        => app.UseMiddleware<CultureRoutingMiddleware>();
}
