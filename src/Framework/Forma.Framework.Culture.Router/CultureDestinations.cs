namespace Forma.Framework.Culture.Router;

/// <summary>The destination a page leads to in another culture, by the host's resolver where it registered one.</summary>
internal static class CultureDestinations
{
    /// <summary>The destination the page at <paramref name="relativeAddress"/>, its culture segment included, leads to in the culture of <paramref name="to"/>: the one <paramref name="resolver"/> names, or the page's own address there.</summary>
    public static async ValueTask<CultureDestination> ResolveAsync(CultureRouter router, ICultureDestinationResolver? resolver, string relativeAddress, CultureRoute to, CancellationToken cancellationToken)
    {
        var (from, address) = router.ReadAddress(relativeAddress);
        var destination = resolver is null ? null : await resolver.ResolveAsync(from, to, address, cancellationToken).ConfigureAwait(false);

        return destination ?? CultureDestination.Translated(address);
    }
}
