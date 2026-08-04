namespace Forma.Blazor.Middlewares;

using Microsoft.AspNetCore.Builder;

public static class CultureRouteExtensions
{
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app, CultureRouteOptions options)
    {
        return app.UseMiddleware<CultureRouteMiddleware>(options);
    }
}
