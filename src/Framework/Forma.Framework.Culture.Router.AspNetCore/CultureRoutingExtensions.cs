namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Builder;

/// <summary>Adds culture routing to the request pipeline.</summary>
public static class CultureRoutingExtensions
{
    /// <summary>Adds the culture routing middleware to the request pipeline, over the router <c>AddFormaCultureRouting</c> registered.</summary>
    public static IApplicationBuilder UseFormaCultureRouting(this IApplicationBuilder app)
        => app.UseMiddleware<CultureRoutingMiddleware>();
}
