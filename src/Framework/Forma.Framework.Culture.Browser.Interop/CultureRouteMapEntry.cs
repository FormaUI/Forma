namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>A route as the page publishes it: its tokens, the pair it carries and the pair's direction.</summary>
public sealed record CultureRouteMapEntry
{
    /// <summary>The route's canonical token.</summary>
    public required string Token { get; init; }

    /// <summary>The further tokens that select the route.</summary>
    public required IReadOnlyList<string> Aliases { get; init; }

    /// <summary>The name of the culture the route formats in.</summary>
    public required string Culture { get; init; }

    /// <summary>The name of the culture the route's strings are in.</summary>
    public required string UICulture { get; init; }

    /// <summary>The direction the route's UI culture writes in, <c>rtl</c> or <c>ltr</c>.</summary>
    public required string Direction { get; init; }
}
