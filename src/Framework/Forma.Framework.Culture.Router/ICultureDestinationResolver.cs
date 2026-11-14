namespace Forma.Framework.Culture.Router;

/// <summary>Where a page leads in another culture, which a host whose pages have addresses of their own in each culture decides.</summary>
public interface ICultureDestinationResolver
{
    /// <summary>The destination the page at <paramref name="relativeAddress"/> in the culture of <paramref name="from"/> leads to in the culture of <paramref name="to"/>, or <see langword="null"/> for the same address in that culture.</summary>
    /// <param name="from">The route the page is shown in.</param>
    /// <param name="to">The route the reader is going to.</param>
    /// <param name="relativeAddress">The page's address relative to the application's root, escaped, without its culture segment and with its query, as <c>news/the-final?page=2</c>.</param>
    /// <param name="cancellationToken">Gives up on the resolution.</param>
    ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken);
}
