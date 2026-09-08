namespace Forma.Blazor.Showcase.Components.Metadata;

using Forma.Framework.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

/// <summary>Emits the canonical link and the hreflang set addressing the current page in every culture.</summary>
public sealed partial class PageLocalization : ComponentBase
{
    private CultureRoute? _currentRoute;
    private string _origin = string.Empty;
    private string _relativePath = string.Empty;

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private CultureRouter Router { get; set; } = default!;

    private IEnumerable<(string HrefLang, string Url)> Alternates
        => Router.Routes.Select(route => (route.HrefLang, GetUrl(route)));

    private string Canonical
        => GetUrl(_currentRoute ?? Router.CurrentRoute);

    private string XDefault
        => GetUrl(Router.DefaultRoute);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _relativePath = Navigation.ToBaseRelativePath(Navigation.Uri);
        _origin = Navigation.BaseUri.TrimEnd('/');

        var queryStart = _relativePath.IndexOf('?', StringComparison.Ordinal);
        var path = PathString.FromUriComponent("/" + (queryStart < 0 ? _relativePath : _relativePath[..queryStart]));

        // Static rendering sees a path the middleware already stripped, so the culture comes from the
        // ambient one; a circuit sees the browser's path and the culture is still in it.
        var match = Router.Match(path);

        _currentRoute = string.Equals(match.Path.Value, path.Value, StringComparison.Ordinal) ? null : match.Route;
    }

    private string GetUrl(CultureRoute route)
        => _origin + Router.GetUrl(route, _relativePath);
}
