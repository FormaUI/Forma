namespace Forma.UI.Blazor.Showcase.Components.Metadata;

using Forma.UI.Blazor.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

/// <summary>Emits the canonical link and the hreflang set addressing the current page in every culture.</summary>
public sealed partial class PageLocalization : ComponentBase
{
    private CultureRoute? _current;
    private string _origin = string.Empty;
    private string _relative = string.Empty;

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private CultureRouter Router { get; set; } = default!;

    private IEnumerable<(string HrefLang, string Url)> Alternates
        => Router.Routes.Select(route => (route.HrefLang, UrlFor(route)));

    private string Canonical
        => UrlFor(_current ?? Router.CurrentRoute);

    private string XDefault
        => UrlFor(Router.DefaultRoute);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _relative = Navigation.ToBaseRelativePath(Navigation.Uri);
        _origin = Navigation.BaseUri.TrimEnd('/');

        var queryStart = _relative.IndexOf('?', StringComparison.Ordinal);
        var path = PathString.FromUriComponent("/" + (queryStart < 0 ? _relative : _relative[..queryStart]));

        // Static rendering sees a path the middleware already stripped, so the culture comes from the
        // ambient one; a circuit sees the browser's path and the culture is still in it.
        var match = Router.Match(path);

        _current = string.Equals(match.Path.Value, path.Value, StringComparison.Ordinal) ? null : match.Route;
    }

    private string UrlFor(CultureRoute route)
        => _origin + Router.GetUrl(route, _relative);
}
