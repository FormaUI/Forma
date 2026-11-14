namespace Forma.Framework.Culture.Router;

using Forma.Framework.Culture.Browser.Interop;

/// <summary>The destinations the page's coordinator navigates a switch to, by the host's resolver where it registered one.</summary>
/// <param name="router">The application's routes.</param>
/// <param name="resolver">The host's resolver.</param>
internal sealed class CultureDestinationSource(CultureRouter router, ICultureDestinationResolver? resolver) : ICultureDestinationSource
{
    /// <inheritdoc />
    public bool ResolvesDestinations
        => resolver is not null;

    /// <inheritdoc />
    public async ValueTask<string?> ResolveAsync(string relativeAddress, string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(relativeAddress);
        ArgumentNullException.ThrowIfNull(token);

        if (resolver is null || router.FindRoute(token) is not { } route)
        {
            return null;
        }

        var destination = await CultureDestinations.ResolveAsync(router, resolver, relativeAddress, route, cancellationToken).ConfigureAwait(false);

        return router.GetUrl(route, destination.RelativeAddress);
    }
}
