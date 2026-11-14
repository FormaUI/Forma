namespace Forma.Blazor.Browser.WebView;

using Forma.Framework.Culture.Router;

/// <summary>The probe page's address of its own in Spanish: <c>localized-probe</c> elsewhere and <c>sonda-traducida</c> in Spanish.</summary>
public sealed class LocalizedProbe : ICultureDestinationResolver
{
    /// <inheritdoc />
    public ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(to);

        return ValueTask.FromResult<CultureDestination?>((relativeAddress, to.Token) switch
        {
            ("localized-probe", "es") => CultureDestination.Localized("sonda-traducida"),
            ("sonda-traducida", not "es") => CultureDestination.Localized("localized-probe"),
            _ => null
        });
    }
}
