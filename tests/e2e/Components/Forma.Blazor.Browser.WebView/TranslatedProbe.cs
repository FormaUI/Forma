namespace Forma.Blazor.Browser.WebView;

using Forma.Framework.Culture.Router;

/// <summary>The probe page's address of its own in Spanish: <c>translated-probe</c> elsewhere and <c>sonda-traducida</c> in Spanish.</summary>
public sealed class TranslatedProbe : ICultureDestinationResolver
{
    /// <inheritdoc />
    public ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(to);

        return ValueTask.FromResult<CultureDestination?>((relativeAddress, to.Token) switch
        {
            ("translated-probe", "es") => CultureDestination.Translated("sonda-traducida"),
            ("sonda-traducida", not "es") => CultureDestination.Translated("translated-probe"),
            _ => null
        });
    }
}
