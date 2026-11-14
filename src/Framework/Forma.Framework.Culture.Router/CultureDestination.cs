namespace Forma.Framework.Culture.Router;

/// <summary>Where a page leads in another culture: its own address there, or the address a host sends the reader to in its place.</summary>
public sealed record CultureDestination
{
    private CultureDestination(string relativeAddress, bool isFallback)
    {
        RelativeAddress = relativeAddress;
        IsFallback = isFallback;
    }

    /// <summary>The destination's address relative to the application's root, escaped, without its culture segment and with its query.</summary>
    public string RelativeAddress { get; }

    /// <summary>Whether the destination stands in for a page the other culture doesn't have.</summary>
    public bool IsFallback { get; }

    /// <summary>The page's own address in the other culture.</summary>
    /// <param name="relativeAddress">The address relative to the application's root, escaped, without its culture segment and with its query.</param>
    public static CultureDestination Translated(string relativeAddress)
    {
        ArgumentNullException.ThrowIfNull(relativeAddress);

        return new(relativeAddress.TrimStart('/'), isFallback: false);
    }

    /// <summary>The address a host sends the reader to in place of a page the other culture doesn't have.</summary>
    /// <param name="relativeAddress">The address relative to the application's root, escaped, without its culture segment and with its query.</param>
    public static CultureDestination Fallback(string relativeAddress)
    {
        ArgumentNullException.ThrowIfNull(relativeAddress);

        return new(relativeAddress.TrimStart('/'), isFallback: true);
    }
}
