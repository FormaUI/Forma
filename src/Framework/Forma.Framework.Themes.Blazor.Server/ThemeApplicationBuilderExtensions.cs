namespace Forma.Framework.Themes.Blazor.Server;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Adds the theme system to the request pipeline: each request's choice, and the endpoint a statically rendered picker posts to.</summary>
public static class ThemeApplicationBuilderExtensions
{
    /// <summary>The address the endpoint <see cref="MapFormaThemes"/> maps takes a switch at, under the application's root.</summary>
    public const string SwitchPath = "/_forma/themes";

    /// <summary>Resolves the theme choice each request starts in, which its page is drawn in from its first paint, and keeps the browser's record beside the host's store.</summary>
    /// <remarks>The step goes before the pages are rendered, and before output caching where a page is cached by its theme.</remarks>
    /// <param name="app">The application's request pipeline.</param>
    /// <returns>The same pipeline.</returns>
    public static IApplicationBuilder UseFormaThemes(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (context, next) =>
        {
            await RequestThemeResolution.ResolveAsync(context).ConfigureAwait(false);
            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>Maps the endpoint a statically rendered theme picker posts a switch to: it keeps the choice in the host's store, then in the browser's record, and sends the reader back to the page.</summary>
    /// <remarks>
    /// The form carries <c>theme</c>, a registered theme's name, <c>scheme</c>, <c>system</c>, <c>light</c> or <c>dark</c>, and
    /// <c>returnUrl</c>, an address of the application's own, along with the antiforgery token the application validates. The
    /// store is written first, one revision past the latest the request knows of, and the record with that revision; a store
    /// that fails leaves the record holding the choice beside the earlier revision, so the browser draws the new choice and the
    /// store keeps its earlier one.
    /// </remarks>
    /// <param name="endpoints">The application's endpoints.</param>
    /// <param name="pattern">The address the endpoint takes a switch at.</param>
    /// <returns>The endpoint's builder.</returns>
    public static RouteHandlerBuilder MapFormaThemes(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern = SwitchPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(pattern, (HttpContext context, IFormCollection form) => ThemeSwitchEndpoint.SwitchAsync(context, form));
    }

    /// <summary>The theme choice the request starts in, as <see cref="UseFormaThemes"/> resolved it.</summary>
    /// <param name="context">The request's context.</param>
    /// <returns>The request's theme, or <see langword="null"/> where the pipeline resolved none.</returns>
    public static RequestTheme? GetFormaTheme(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.Get<RequestTheme>();
    }
}
