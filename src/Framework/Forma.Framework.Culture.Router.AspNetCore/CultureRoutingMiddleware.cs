namespace Forma.Framework.Culture.Router.AspNetCore;

using System.Globalization;

using Forma.Framework.Culture.Router;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Selects the request culture from the first URL path segment, strips that segment before routing,
/// and redirects any URL that is not the canonical address of the culture it names.
/// </summary>
public sealed class CultureRoutingMiddleware
{
    // How long the browser keeps the culture cookie.
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(365);

    private readonly RequestDelegate _next;
    private readonly CultureRouter _router;

    /// <summary>Creates the middleware over the application's registered cultures.</summary>
    public CultureRoutingMiddleware(RequestDelegate next, CultureRouter router)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(router);

        _next = next;
        _router = router;
    }

    /// <summary>Applies the request culture, canonicalizes the URL, and forwards a culture-free path.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestedPath = context.Request.Path.Value ?? string.Empty;

        if (_router.HasConsecutiveCultures(requestedPath))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return Task.CompletedTask;
        }

        var match = _router.Match(requestedPath);
        var isDocument = IsDocument(match.Path);

        // A framework endpoint such as /_blazor, which its interactive circuit inherits a culture from, takes the culture its
        // address names, and the stored culture only where its address names none.
        var namesCulture = !string.Equals(match.Path, requestedPath, StringComparison.Ordinal);
        var route = isDocument || namesCulture ? match.Route : GetStoredRoute(context) ?? match.Route;

        ApplyCulture(context, route);

        if (namesCulture)
        {
            // An endpoint chosen for the address with its culture segment, as UsePathBase chooses one, is chosen again
            // for the path routing goes on to.
            context.Request.Path = new PathString(match.Path);
            context.SetEndpoint(null);
            context.Request.RouteValues.Clear();
        }

        if (!isDocument)
        {
            return _next(context);
        }

        if (!match.IsCanonical)
        {
            return RedirectToCanonical(context, new PathString(match.CanonicalPath));
        }

        WriteCultureCookie(context, match.Route);

        return _next(context);
    }

    // Each of the route's cultures is the application's culture provider's for its name, where it registered one.
    private static void ApplyCulture(HttpContext context, CultureRoute route)
    {
        var cultureProvider = context.RequestServices?.GetService<ICultureProvider>();
        var culture = CultureResolution.GetCulture(cultureProvider, route.Culture);
        var uiCulture = CultureResolution.GetCulture(cultureProvider, route.UICulture);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;

        context.Response.Headers.ContentLanguage = uiCulture.Name;

        // Blazor resolves the interactive circuit's culture from this feature.
        context.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(new RequestCulture(culture, uiCulture), provider: null));
    }

    private static bool IsDocument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (value.StartsWith("/_", StringComparison.Ordinal))
        {
            return false;
        }

        var lastSegment = value.AsSpan(value.AsSpan().LastIndexOf('/') + 1);

        return !lastSegment.Contains('.');
    }

    private static Task RedirectToCanonical(HttpContext context, PathString canonicalPath)
    {
        var location = context.Request.PathBase.ToUriComponent()
            + canonicalPath.ToUriComponent()
            + context.Request.QueryString.ToUriComponent();

        context.Response.Redirect(location, permanent: false);

        return Task.CompletedTask;
    }

    private CultureRoute? GetStoredRoute(HttpContext context)
    {
        var storedToken = context.Request.Cookies[CultureRouter.GetCookieName(context.Request.PathBase.Value ?? string.Empty)];

        return string.IsNullOrEmpty(storedToken) ? null : _router.Routes.FirstOrDefault(route => string.Equals(route.Token, storedToken, StringComparison.Ordinal));
    }

    private static void WriteCultureCookie(HttpContext context, CultureRoute route)
    {
        var name = CultureRouter.GetCookieName(context.Request.PathBase.Value ?? string.Empty);

        if (context.Request.Cookies.TryGetValue(name, out var storedToken) && string.Equals(storedToken, route.Token, StringComparison.Ordinal))
        {
            return;
        }

        context.Response.Cookies.Append(name, route.Token, new CookieOptions
        {
            Path = CultureRouter.GetCookiePath(context.Request.PathBase.ToUriComponent()),
            IsEssential = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = CookieLifetime
        });
    }
}
