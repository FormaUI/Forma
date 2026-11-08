namespace Forma.Framework.Culture.Router;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using Forma.Framework.Culture.Browser.Interop;

/// <summary>The culture pairs an application serves, and the addresses carrying them.</summary>
/// <remarks>
/// A path is read as a request reads it, unescaped, and a relative URL as a link writes it, escaped, its token segments
/// compared unescaped; the page's coordinator builds and reads addresses from the mapping <see cref="GetMap"/> publishes
/// by the same rules.
/// </remarks>
public sealed partial class CultureRouter : ICultureAddressing
{
    private const string CookieName = "forma-culture-route";
    private readonly Dictionary<string, CultureRoute> _routesByToken = [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Creates a router over the routes <paramref name="options"/> registers.</summary>
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

        // A browser's timer holds a delay of up to 2^31 - 1 milliseconds, and runs a longer one at once.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.SwitchTimeout, TimeSpan.Zero, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.SwitchTimeout, TimeSpan.FromMilliseconds(int.MaxValue), nameof(options));

        Routes = [.. options.Routes];
        Prefix = options.Prefix;
        Switch = options.Switch;
        SwitchTimeout = options.SwitchTimeout;
        DefaultRoute = Routes.FirstOrDefault(route => string.Equals(route.Token, options.DefaultCulture, StringComparison.Ordinal))
            ?? throw new ArgumentException($"DefaultCulture '{options.DefaultCulture}' is not the token of any registered culture.", nameof(options));
    }

    /// <summary>The route serving a request that names none.</summary>
    public CultureRoute DefaultRoute { get; }

    /// <summary>Every route the application serves, in registration order.</summary>
    public IReadOnlyList<CultureRoute> Routes { get; }

    /// <summary>Where a culture token appears in a URL.</summary>
    public CulturePrefix Prefix { get; }

    /// <summary>How a switch to another route navigates.</summary>
    public CultureSwitchMode Switch { get; }

    /// <summary>How long a switch in place waits for its navigation to land, past which it completes as timed out.</summary>
    public TimeSpan SwitchTimeout { get; }

    // Google reads an ISO 639-1 language with an optional ISO 15924 script and ISO 3166-1 alpha-2 region.
    [GeneratedRegex("^[a-z]{2}(-[a-z]{4})?(-[a-z]{2})?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HrefLangPattern { get; }

    /// <summary>The name of the cookie naming the route last chosen by the application at <paramref name="pathBase"/>.</summary>
    /// <remarks>Under a path base the name carries the path base, escaped, so a request never carries two culture cookies of one name, however many applications share the host.</remarks>
    /// <param name="pathBase">The application's path base, unescaped, empty at the host's root.</param>
    public static string GetCookieName(string pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase);

        return pathBase.Length == 0 ? CookieName : CookieName + Uri.EscapeDataString(pathBase);
    }

    /// <summary>The path the cookie naming the route last chosen by the application at <paramref name="pathBase"/> is written at.</summary>
    /// <param name="pathBase">The application's path base, escaped, empty at the host's root.</param>
    public static string GetCookiePath(string pathBase)
    {
        ArgumentNullException.ThrowIfNull(pathBase);

        return pathBase.Length == 0 ? "/" : pathBase;
    }

    /// <summary>The route carrying the pair of <paramref name="culture"/> and <paramref name="uiCulture"/>, or <see langword="null"/> where none carries it.</summary>
    public CultureRoute? FindRoute(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        return Routes.FirstOrDefault(route => string.Equals(route.Culture.Name, culture.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(route.UICulture.Name, uiCulture.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The route <paramref name="token"/> or one of its aliases selects, in any case, or <see langword="null"/> where none does.</summary>
    public CultureRoute? FindRoute(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return _routesByToken.GetValueOrDefault(token);
    }

    /// <summary>The path <paramref name="path"/>, unescaped, in the culture of <paramref name="route"/>.</summary>
    public string GetPath(CultureRoute route, string path)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(path);

        GuardAgainstConsecutiveCultures(GetSegments(path), path, nameof(path));

        return PrefixPath(route, route.Token, StripToken(path, segment => segment));
    }

    /// <summary>The relative URL <paramref name="relativeUrl"/>, escaped, in the culture of <paramref name="route"/>, query included.</summary>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    [SuppressMessage("Design", "CA1055:URI return values should not be strings")]
    public string GetUrl(CultureRoute route, string relativeUrl)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(relativeUrl);

        var queryStart = relativeUrl.IndexOf('?', StringComparison.Ordinal);
        var path = "/" + (queryStart < 0 ? relativeUrl : relativeUrl[..queryStart]);
        var query = queryStart < 0 ? string.Empty : relativeUrl[queryStart..];

        GuardAgainstConsecutiveCultures([.. GetSegments(path).Select(Uri.UnescapeDataString)], relativeUrl, nameof(relativeUrl));

        return PrefixPath(route, Uri.EscapeDataString(route.Token), StripToken(path, Uri.UnescapeDataString)) + query;
    }

    /// <summary>Matches the unescaped <paramref name="path"/> against the registered routes.</summary>
    public CultureMatch Match(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var segments = GetSegments(path);
        GuardAgainstConsecutiveCultures(segments, path, nameof(path));

        var normalized = path.Length == 0 ? "/" : path;
        var route = segments.Length > 0 ? FindRoute(segments[0]) : null;
        var effectiveRoute = route ?? DefaultRoute;
        var remainder = route is null ? normalized : "/" + string.Join('/', segments.Skip(1));
        var canonicalPath = PrefixPath(effectiveRoute, effectiveRoute.Token, remainder);

        return new CultureMatch(effectiveRoute, remainder, canonicalPath, string.Equals(canonicalPath, normalized, StringComparison.Ordinal));
    }

    /// <summary>Whether the unescaped <paramref name="path"/> names a culture in two consecutive segments.</summary>
    public bool HasConsecutiveCultures(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return HasConsecutiveCultures(GetSegments(path));
    }

    /// <inheritdoc />
    public bool CanAddress(CultureInfo culture, CultureInfo uiCulture)
        => FindRoute(culture, uiCulture) is not null;

    /// <inheritdoc />
    /// <remarks>
    /// Interactive routing finds a page within the base, so under a base in a route's culture it finds the pages of the
    /// document's culture and none at another route's address, which loads as a new document. Under
    /// <see cref="CultureSwitchMode.InPlace"/> the base is the application's root, within which every route's address lies.
    /// </remarks>
    public string GetDocumentBase(string root, CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);

        if (Switch is CultureSwitchMode.InPlace)
        {
            return root;
        }

        var route = FindRoute(culture, uiCulture) ?? DefaultRoute;

        return IsUnprefixed(route) ? root : $"{root.TrimEnd('/')}/{Uri.EscapeDataString(route.Token)}/";
    }

    /// <inheritdoc />
    public CultureRouteMap GetMap(string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);

        var pathBase = root.TrimEnd('/');

        return new CultureRouteMap
        {
            Routes = [.. Routes.Select(route => new CultureRouteMapEntry
            {
                Token = route.Token,
                Aliases = route.Aliases,
                Culture = route.Culture.Name,
                UICulture = route.UICulture.Name,
                Direction = route.UICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr",
            })],
            DefaultToken = DefaultRoute.Token,
            PrefixesDefault = Prefix is CulturePrefix.Always,
            SwitchesInPlace = Switch is CultureSwitchMode.InPlace,
            SwitchTimeout = SwitchTimeout,
            Root = root,
            CookieName = GetCookieName(Uri.UnescapeDataString(pathBase)),
            CookiePath = GetCookiePath(pathBase),
        };
    }

    /// <summary>Whether <paramref name="segment"/> is a registered route's token or alias, in any case.</summary>
    internal bool IsToken(string segment)
        => _routesByToken.ContainsKey(segment);

    private static string[] GetSegments(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static void Validate(CultureRoute route, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.Token);
        ArgumentNullException.ThrowIfNull(route.UICulture);
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

    private bool HasConsecutiveCultures(string[] segments)
        => segments.Length > 1 && IsToken(segments[0]) && IsToken(segments[1]);

    private void GuardAgainstConsecutiveCultures(string[] segments, string path, string parameterName)
    {
        if (HasConsecutiveCultures(segments))
        {
            throw new ArgumentException($"Path '{path}' names a culture in two consecutive segments.", parameterName);
        }
    }

    private bool IsUnprefixed(CultureRoute route)
        => Prefix is CulturePrefix.ExceptDefault && string.Equals(route.Token, DefaultRoute.Token, StringComparison.Ordinal);

    // The path under the route's token, as written by `token`, or the path itself where the route carries none.
    private string PrefixPath(CultureRoute route, string token, string path)
        => IsUnprefixed(route) ? path : path == "/" ? "/" + token : "/" + token + path;

    // The path without the culture segment it opens with, each segment read through `read` to compare it with the tokens.
    private string StripToken(string path, Func<string, string> read)
    {
        var normalized = path.Length == 0 ? "/" : path;
        var segments = GetSegments(normalized);

        if (segments.Length > 0 && IsToken(read(segments[0])))
        {
            return segments.Length == 1 ? "/" : "/" + string.Join('/', segments.Skip(1));
        }

        return normalized;
    }

    private void Register(string token, CultureRoute route, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (!_routesByToken.TryAdd(token, route))
        {
            throw new ArgumentException($"Token '{token}' is registered by more than one culture.", parameterName);
        }
    }
}
