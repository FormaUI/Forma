namespace Forma.Blazor.Browser.Host;

using Forma.Framework.Culture.Router;

/// <summary>The culture checks' page with an address of its own in each culture: <c>culture/localized</c> in English and <c>culture/meturgam</c> in Hebrew.</summary>
public sealed class LocalizedPages : ICultureDestinationResolver
{
    /// <inheritdoc />
    public ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(relativeAddress);

        var queryStart = relativeAddress.IndexOf('?', StringComparison.Ordinal);
        var path = queryStart < 0 ? relativeAddress : relativeAddress[..queryStart];
        var query = queryStart < 0 ? string.Empty : relativeAddress[queryStart..];

        return ValueTask.FromResult<CultureDestination?>((path, to.Token) switch
        {
            ("culture/localized", "he") => CultureDestination.Localized("culture/meturgam" + query),
            ("culture/meturgam", "en") => CultureDestination.Localized("culture/localized" + query),
            _ => null
        });
    }
}
