namespace Forma.Framework.Culture.Router.AspNetCore;

using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

/// <summary>
/// Selects the request culture from the first URL path segment, strips that segment before routing,
/// and redirects any URL that is not the canonical address of the culture it names.
/// </summary>
public sealed class CultureRoutingMiddleware
{
    private const string CultureCookieName = "forma-culture-route";

    // The HttpOnly cookie at the path / that an earlier version wrote, which the culture cookie replaces.
    private const string LegacyCultureCookieName = "forma-culture";

    /// <summary>How long the browser keeps the culture cookie.</summary>
    internal static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(365);

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

        var requested = context.Request.Path;

        if (_router.HasConsecutiveCultures(requested))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return Task.CompletedTask;
        }

        var match = _router.Match(requested);
        var isDocument = IsDocument(match.Path);

        // A framework endpoint such as /_blazor, which its interactive circuit inherits a culture from, takes the culture its
        // address names, and the stored culture only where its address names none.
        var namesCulture = match.Path != requested;
        var route = isDocument || namesCulture ? match.Route : GetStoredRoute(context) ?? match.Route;

        ApplyCulture(context, route);

        if (namesCulture)
        {
            // An endpoint chosen for the address with its culture segment, as UsePathBase chooses one, is chosen again
            // for the path routing goes on to.
            context.Request.Path = match.Path;
            context.SetEndpoint(null);
            context.Request.RouteValues.Clear();
        }

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

        // Blazor resolves the interactive circuit's culture from this feature.
        context.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(new RequestCulture(route.Culture, route.Culture), provider: null));
    }

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

    private CultureRoute? GetStoredRoute(HttpContext context)
    {
        var cookies = context.Request.Cookies;
        var stored = cookies.TryGetValue(GetCookieName(context.Request.PathBase), out var current) ? current : cookies[LegacyCultureCookieName];

        if (string.IsNullOrEmpty(stored))
        {
            return null;
        }

        return _router.Routes.FirstOrDefault(route => string.Equals(route.Token, stored, StringComparison.Ordinal));
    }

    private static void WriteCultureCookie(HttpContext context, CultureRoute route)
    {
        var cookies = context.Request.Cookies;

        if (cookies.ContainsKey(LegacyCultureCookieName))
        {
            context.Response.Cookies.Delete(LegacyCultureCookieName, new CookieOptions { Path = "/" });
        }

        var name = GetCookieName(context.Request.PathBase);

        if (cookies.TryGetValue(name, out var stored) && string.Equals(stored, route.Token, StringComparison.Ordinal))
        {
            return;
        }

        context.Response.Cookies.Append(name, route.Token, new CookieOptions
        {
            Path = GetCookiePath(context.Request.PathBase),
            IsEssential = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = CookieLifetime
        });
    }

    /// <summary>The name of the culture cookie of the application at <paramref name="pathBase"/>.</summary>
    /// <remarks>
    /// Under a path base the name carries the path base, escaped, so a request never carries two culture cookies of one
    /// name, however many applications share the host.
    /// </remarks>
    internal static string GetCookieName(PathString pathBase)
        => pathBase.HasValue
            ? CultureCookieName + Uri.EscapeDataString(pathBase.Value)
            : CultureCookieName;

    /// <summary>The path the culture cookie of the application at <paramref name="pathBase"/> is written at.</summary>
    internal static string GetCookiePath(PathString pathBase)
        => pathBase.HasValue ? pathBase.ToUriComponent() : "/";
}
