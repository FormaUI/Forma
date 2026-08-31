namespace Forma.Blazor.Globalization;

using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

/// <summary>
/// Selects the request culture from the first URL path segment, strips that segment before routing,
/// and redirects any URL that is not the canonical address of the culture it names.
/// </summary>
public sealed class CultureRoutingMiddleware
{
    private const string CultureCookieName = "forma-culture";

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

    /// <summary>Applies the request culture, canonicalises the URL, and forwards a culture-free path.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requested = context.Request.Path;

        if (_router.HasConsecutiveCultures(requested))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return Task.CompletedTask;
        }

        var match = _router.Match(requested);
        var isDocument = IsDocument(requested);

        // A framework endpoint such as /_blazor carries no culture segment, and the interactive
        // circuit it opens inherits this request's culture, so the stored one stands in for it.
        var route = isDocument ? match.Route : StoredRoute(context) ?? match.Route;

        ApplyCulture(context, route);
        context.Request.Path = match.Path;

        if (!isDocument)
        {
            return _next(context);
        }

        if (!match.IsCanonical)
        {
            return RedirectToCanonical(context, match.CanonicalPath);
        }

        WriteCultureCookie(context, match.Route);

        return _next(context);
    }

    private static void ApplyCulture(HttpContext context, CultureRoute route)
    {
        CultureInfo.CurrentCulture = route.Culture;
        CultureInfo.CurrentUICulture = route.Culture;

        context.Response.Headers.ContentLanguage = route.Culture.Name;

        // Blazor resolves the interactive circuit's culture from this feature, not from CultureInfo.Current*.
        context.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(new RequestCulture(route.Culture, route.Culture), provider: null));
    }

    // Framework endpoints and files carrying an extension are neither canonicalised nor given a cookie.
    private static bool IsDocument(PathString path)
    {
        var value = path.Value;

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

    private CultureRoute? StoredRoute(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CultureCookieName, out var stored) || string.IsNullOrEmpty(stored))
        {
            return null;
        }

        return _router.Routes.FirstOrDefault(route => string.Equals(route.Token, stored, StringComparison.Ordinal));
    }

    private static void WriteCultureCookie(HttpContext context, CultureRoute route)
    {
        if (context.Request.Cookies.TryGetValue(CultureCookieName, out var stored)
            && string.Equals(stored, route.Token, StringComparison.Ordinal))
        {
            return;
        }

        context.Response.Cookies.Append(CultureCookieName, route.Token, new CookieOptions
        {
            Path = "/",
            IsEssential = true,
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = CookieLifetime
        });
    }
}
