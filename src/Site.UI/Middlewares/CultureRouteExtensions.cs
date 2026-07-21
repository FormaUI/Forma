using Microsoft.AspNetCore.Builder;

namespace Site.UI.Middlewares;

public static class CultureRouteExtensions
{
    public static IApplicationBuilder UseCultureRouting(this IApplicationBuilder app, CultureRouteOptions options)
    {
        return app.UseMiddleware<CultureRouteMiddleware>(options);
    }
}
