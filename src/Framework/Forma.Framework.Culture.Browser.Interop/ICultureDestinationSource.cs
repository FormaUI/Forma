namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>The destinations the page's coordinator navigates a switch to, where pages have addresses of their own in each culture.</summary>
public interface ICultureDestinationSource
{
    /// <summary>Whether the source names destinations.</summary>
    bool ResolvesDestinations { get; }

    /// <summary>The address, relative to the application's root and with its culture segment, the page at <paramref name="relativeAddress"/> leads to in the route <paramref name="token"/> names, or <see langword="null"/> for that page's own address in the route.</summary>
    /// <param name="relativeAddress">The page's address relative to the application's root, escaped, with its culture segment and its query.</param>
    /// <param name="token">The token of the route the switch goes to.</param>
    /// <param name="cancellationToken">Gives up on the resolution.</param>
    ValueTask<string?> ResolveAsync(string relativeAddress, string token, CancellationToken cancellationToken);
}
