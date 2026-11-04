namespace Forma.Framework.Culture.Blazor.Server;

using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Adds the application's cultures to the request pipeline.</summary>
public static class RequestCultureExtensions
{
    /// <summary>Runs each request in the cultures the application's <see cref="ICultureProvider"/> gives for the names of the cultures the pipeline chose for it, as request localization chooses them, and leaves a request whose application registered no provider in its own.</summary>
    /// <remarks>The step goes after the middleware that chooses the request's cultures, as <c>UseRequestLocalization</c>, and sets the request culture feature beside the ambient cultures.</remarks>
    /// <param name="app">The application's request pipeline.</param>
    /// <returns>The same pipeline.</returns>
    public static IApplicationBuilder UseFormaRequestCulture(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static (context, next) =>
        {
            ApplyCulture(context);

            return next(context);
        });
    }

    private static void ApplyCulture(HttpContext context)
    {
        if (context.RequestServices?.GetService<ICultureProvider>() is not { } cultureProvider)
        {
            return;
        }

        var culture = CultureResolution.GetCulture(cultureProvider, CultureInfo.CurrentCulture);
        var uiCulture = CultureResolution.GetCulture(cultureProvider, CultureInfo.CurrentUICulture);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;

        if (context.Features.Get<IRequestCultureFeature>() is { } feature)
        {
            context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture(culture, uiCulture), feature.Provider));
        }
    }
}
