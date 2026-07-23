using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Forma.Blazor.Middlewares;

/// <summary>
/// Extracts the culture from the first URL path segment, sets <see cref="CultureInfo.CurrentCulture"/>
/// and <see cref="CultureInfo.CurrentUICulture"/>, then rewrites the request path to strip the culture segment.
/// Redirects to the default culture if the first segment is not a supported culture.
/// </summary>
public sealed class CultureRouteMiddleware
{
    private const string CultureCookieName = "forma-culture";
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _supportedCultures;
    private readonly string _defaultCulture;

    public CultureRouteMiddleware(RequestDelegate next, CultureRouteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _next = next;
        _supportedCultures = new HashSet<string>(options.SupportedCultures, StringComparer.OrdinalIgnoreCase);
        _defaultCulture = options.DefaultCulture;
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var path = context.Request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var pathCulture = TryGetPathCulture(segments);
        var cookieCulture = TryGetCookieCulture(context);
        var effectiveCulture = pathCulture ?? cookieCulture;

        if (effectiveCulture is not null)
        {
            ApplyCulture(context, effectiveCulture);
        }

        if (pathCulture is not null)
        {
            context.Response.Cookies.Append(CultureCookieName, pathCulture, new CookieOptions
            {
                Path = "/",
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            // Rewrite path to strip the culture segment: "/he/modal" -> "/modal"
            var remaining = string.Join('/', segments.Skip(1));
            context.Request.Path = "/" + remaining;
        }

        // Skip framework paths and static files
        if (path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.', StringComparison.Ordinal))
        {
            return _next(context);
        }

        if (pathCulture is null)
        {
            var targetCulture = cookieCulture ?? _defaultCulture;
            context.Response.Redirect($"/{targetCulture}{path}", permanent: false);
            return Task.CompletedTask;
        }

        return _next(context);
    }

    private string? TryGetPathCulture(string[] segments)
    {
        if (segments.Length == 0)
            return null;

        return _supportedCultures.Contains(segments[0]) ? segments[0] : null;
    }

    private string? TryGetCookieCulture(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CultureCookieName, out var culture))
            return null;

        return !string.IsNullOrWhiteSpace(culture) && _supportedCultures.Contains(culture)
            ? culture
            : null;
    }

    private static void ApplyCulture(HttpContext context, string cultureName)
    {
        var culture = CultureInfo.CreateSpecificCulture(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        context.Response.Headers.ContentLanguage = culture.Name;

        // Set IRequestCultureFeature so Blazor Server reads the culture when
        // establishing the interactive circuit (it resolves the circuit culture
        // from this feature, not from CultureInfo.Current*).
        context.Features.Set<IRequestCultureFeature>(
            new RequestCultureFeature(new RequestCulture(culture, culture), provider: null));
    }
}
