namespace Forma.Framework.Globalization.AspNetCore;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;

/// <summary>The cultures an application serves, and the URLs addressing them.</summary>
public sealed partial class CultureRouter
{
    private readonly Dictionary<string, CultureRoute> _routesByToken = [with(StringComparer.OrdinalIgnoreCase)];
    private readonly CulturePrefix _prefix;

    /// <summary>Creates a router over the cultures <paramref name="options"/> registers.</summary>
    public CultureRouter(CultureRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Routes);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DefaultCulture);

        if (options.Routes.Count == 0)
        {
            throw new ArgumentException("At least one culture must be registered.", nameof(options));
        }

        foreach (var route in options.Routes)
        {
            Validate(route, nameof(options));
            Register(route.Token, route, nameof(options));

            foreach (var alias in route.Aliases)
            {
                Register(alias, route, nameof(options));
            }
        }

        Routes = [.. options.Routes];
        _prefix = options.Prefix;
        DefaultRoute = Routes.FirstOrDefault(route => string.Equals(route.Token, options.DefaultCulture, StringComparison.Ordinal))
            ?? throw new ArgumentException($"DefaultCulture '{options.DefaultCulture}' is not the token of any registered culture.", nameof(options));
    }

    /// <summary>The route whose culture is currently in effect.</summary>
    public CultureRoute CurrentRoute
    {
        get
        {
            var current = CultureInfo.CurrentUICulture.Name;

            return Routes.FirstOrDefault(route => string.Equals(route.Culture.Name, current, StringComparison.OrdinalIgnoreCase)) ?? DefaultRoute;
        }
    }

    /// <summary>The culture serving a request that names none.</summary>
    public CultureRoute DefaultRoute { get; }

    /// <summary>Every culture the application serves, in registration order.</summary>
    public IReadOnlyList<CultureRoute> Routes { get; }

    // Google reads an ISO 639-1 language with an optional ISO 15924 script and ISO 3166-1 alpha-2 region.
    [GeneratedRegex("^[a-z]{2}(-[a-z]{4})?(-[a-z]{2})?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HrefLangPattern { get; }

    /// <summary>The path <paramref name="path"/> in the culture of <paramref name="route"/>.</summary>
    public PathString GetPath(CultureRoute route, PathString path)
    {
        ArgumentNullException.ThrowIfNull(route);
        GuardAgainstConsecutiveCultures(path, nameof(path));

        return Prefix(route, StripToken(path));
    }

    /// <summary>The relative URL <paramref name="relativeUrl"/> in the culture of <paramref name="route"/>, query included.</summary>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    [SuppressMessage("Design", "CA1055:URI return values should not be strings")]
    public string GetUrl(CultureRoute route, string relativeUrl)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(relativeUrl);

        var queryStart = relativeUrl.IndexOf('?', StringComparison.Ordinal);
        var path = PathString.FromUriComponent("/" + (queryStart < 0 ? relativeUrl : relativeUrl[..queryStart]));
        var query = queryStart < 0 ? QueryString.Empty : QueryString.FromUriComponent(relativeUrl[queryStart..]);

        GuardAgainstConsecutiveCultures(path, nameof(relativeUrl));

        return Prefix(route, StripToken(path)).ToUriComponent() + query.ToUriComponent();
    }

    /// <summary>Matches <paramref name="path"/> against the registered cultures.</summary>
    public CultureMatch Match(PathString path)
    {
        GuardAgainstConsecutiveCultures(path, nameof(path));

        var normalized = path.HasValue ? path : new PathString("/");
        var segments = GetSegments(normalized);
        CultureRoute? route = segments.Length > 0 && _routesByToken.TryGetValue(segments[0], out var found) ? found : null;
        var effectiveRoute = route ?? DefaultRoute;

        var remainder = route is null
            ? normalized
            : new PathString(segments.Length == 1 ? "/" : "/" + string.Join('/', segments.Skip(1)));

        var canonicalPath = Prefix(effectiveRoute, remainder);
        var isCanonical = string.Equals(canonicalPath.Value, normalized.Value, StringComparison.Ordinal);

        return new CultureMatch(effectiveRoute, remainder, canonicalPath, isCanonical);
    }

    /// <summary>Whether <paramref name="path"/> names a culture in two consecutive segments.</summary>
    public bool HasConsecutiveCultures(PathString path)
    {
        var segments = GetSegments(path);

        return segments.Length > 1 && _routesByToken.ContainsKey(segments[0]) && _routesByToken.ContainsKey(segments[1]);
    }

    private static string[] GetSegments(PathString path)
        => (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static void Validate(CultureRoute route, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.Token);
        ArgumentNullException.ThrowIfNull(route.Culture);
        ArgumentNullException.ThrowIfNull(route.Aliases);

        if (route.Token.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException($"Culture token '{route.Token}' must be a single path segment.", parameterName);
        }

        if (!HrefLangPattern.IsMatch(route.HrefLang))
        {
            throw new ArgumentException(
                $"Culture '{route.Token}' has hreflang '{route.HrefLang}', which is not a language with an optional script and region. Set {nameof(CultureRoute.HrefLang)} to a supported value.",
                parameterName);
        }
    }

    private void GuardAgainstConsecutiveCultures(PathString path, string parameterName)
    {
        if (HasConsecutiveCultures(path))
        {
            throw new ArgumentException($"Path '{path}' names a culture in two consecutive segments.", parameterName);
        }
    }

    private bool IsDefaultRoute(CultureRoute route)
        => string.Equals(route.Token, DefaultRoute.Token, StringComparison.Ordinal);

    private PathString Prefix(CultureRoute route, PathString path)
    {
        if (_prefix == CulturePrefix.ExceptDefault && IsDefaultRoute(route))
        {
            return path;
        }

        var prefix = new PathString("/" + route.Token);

        return path == "/" ? prefix : prefix.Add(path);
    }

    private void Register(string token, CultureRoute route, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!_routesByToken.TryAdd(token, route))
        {
            throw new ArgumentException($"Token '{token}' is registered by more than one culture.", parameterName);
        }
    }

    private PathString StripToken(PathString path)
    {
        var normalized = path.HasValue ? path : new PathString("/");
        var segments = GetSegments(normalized);

        if (segments.Length > 0 && _routesByToken.ContainsKey(segments[0]))
        {
            return new PathString(segments.Length == 1 ? "/" : "/" + string.Join('/', segments.Skip(1)));
        }

        return normalized;
    }
}
