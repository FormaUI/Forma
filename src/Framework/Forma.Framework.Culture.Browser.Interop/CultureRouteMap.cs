namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>The mapping between culture addresses and pairs a page publishes, which its coordinator builds a destination and resolves an interactive landing from.</summary>
public sealed record CultureRouteMap
{
    /// <summary>Every route, in registration order.</summary>
    public required IReadOnlyList<CultureRouteMapEntry> Routes { get; init; }

    /// <summary>The token of the route an address naming none carries.</summary>
    public required string DefaultToken { get; init; }

    /// <summary>Whether the default route's addresses carry its token too.</summary>
    public required bool PrefixesDefault { get; init; }

    /// <summary>Whether a switch reaches another route's address in the same document, rather than loading it as a new one.</summary>
    public required bool SwitchesInPlace { get; init; }

    /// <summary>How long a switch in place waits for its navigation to land, past which it completes as timed out.</summary>
    public TimeSpan SwitchTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>The application's root, an escaped absolute path ending in a slash, such as <c>/app/</c>.</summary>
    public required string Root { get; init; }

    /// <summary>The name of the cookie naming the route last chosen.</summary>
    public required string CookieName { get; init; }

    /// <summary>The path the cookie is written at.</summary>
    public required string CookiePath { get; init; }
}
