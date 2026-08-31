namespace Forma.Blazor.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers culture routing and adds it to the request pipeline.</summary>
public static class CultureRoutingExtensions
{
    /// <summary>Validates <paramref name="options"/> and registers the culture registry.</summary>
    public static IServiceCollection AddCultureRouting(this IServiceCollection services, CultureRouterOptions options)
        => services.AddSingleton(new CultureRouter(options));

    /// <summary>Adds culture routing to the pipeline, before routing.</summary>
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app)
        => app.UseMiddleware<CultureRoutingMiddleware>();
}
