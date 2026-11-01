namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;
using System.Text.Json;

/// <summary>The culture marker and the switch's policy a page publishes for its culture coordinator, as the attributes of the meta elements that carry them.</summary>
public static class CulturePublication
{
    /// <summary>The culture marker's attributes, naming the pair of <paramref name="culture"/> and <paramref name="uiCulture"/>, each culture on its own.</summary>
    /// <param name="culture">The culture the page formats in.</param>
    /// <param name="uiCulture">The culture the page's strings and direction are in.</param>
    /// <returns>The marker's attributes by name.</returns>
    public static IReadOnlyDictionary<string, string> GetMarkerAttributes(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        return new Dictionary<string, string>
        {
            ["data-culture"] = culture.Name,
            ["data-ui-culture"] = uiCulture.Name,
        };
    }

    /// <summary>The switch policy's attributes: the address policy publishing <paramref name="map"/> where there is one, otherwise <paramref name="policy"/>, and whether the page's document follows an application's coordinator.</summary>
    /// <param name="policy">How a switch persists the pair the page accepts.</param>
    /// <param name="map">The mapping between culture addresses and pairs the page publishes.</param>
    /// <param name="follows">Whether the page's document follows the pair an application's coordinator accepts, as a web view's does, rather than accepting one of its own.</param>
    /// <returns>The policy's attributes by name, a present attribute that has no value holding the empty string.</returns>
    public static IReadOnlyDictionary<string, string> GetPolicyAttributes(CultureSwitchPolicy policy, CultureRouteMap? map, bool follows = false)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var attributes = CreatePolicyAttributes(policy, map);

        if (follows)
        {
            attributes["data-follows"] = string.Empty;
        }

        return attributes;
    }

    private static Dictionary<string, string> CreatePolicyAttributes(CultureSwitchPolicy policy, CultureRouteMap? map)
    {
        if (map is not null)
        {
            return new Dictionary<string, string>
            {
                ["content"] = "address",
                ["data-routes"] = JsonSerializer.Serialize(map.Routes, CultureRouteMapJsonContext.Default.IReadOnlyListCultureRouteMapEntry),
                ["data-default"] = map.DefaultToken,
                ["data-prefix"] = map.PrefixesDefault ? "always" : "except-default",
                ["data-switch"] = map.SwitchesInPlace ? "in-place" : "full-load",
                ["data-root"] = map.Root,
                ["data-cookie-name"] = map.CookieName,
                ["data-cookie-path"] = map.CookiePath,
            };
        }

        if (policy.Persistence is not CultureSwitchPersistence.Cookie)
        {
            return new Dictionary<string, string> { ["content"] = "state" };
        }

        var attributes = new Dictionary<string, string>
        {
            ["content"] = "cookie",
            ["data-name"] = policy.CookieName,
            ["data-path"] = policy.CookiePath,
            ["data-same-site"] = policy.CookieSameSite switch
            {
                CultureCookieSameSite.Strict => "strict",
                CultureCookieSameSite.None => "none",
                _ => "lax",
            },
        };

        if (policy.CookieDomain is { } domain)
        {
            attributes["data-domain"] = domain;
        }

        // The cookie's lifetime in whole seconds, as a cookie's max-age reads it.
        if (policy.CookieLifetime is { } lifetime)
        {
            attributes["data-max-age"] = ((long)lifetime.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        if (policy.IsCookieSecure)
        {
            attributes["data-secure"] = string.Empty;
        }

        return attributes;
    }
}
