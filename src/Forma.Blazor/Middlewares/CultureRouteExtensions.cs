using Microsoft.AspNetCore.Builder;

namespace Forma.Blazor.Middlewares;

public static class CultureRouteExtensions
{
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app, CultureRouteOptions options)
    {
        return app.UseMiddleware<CultureRouteMiddleware>(options);
    }
}
