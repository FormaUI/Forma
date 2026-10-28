namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

/// <summary>Switches a user scope to another culture by navigating to the current page's address in it.</summary>
/// <remarks>
/// The switcher only requests the address; the culture state follows the address once the navigation that request
/// becomes settles, so an address and the culture it shows never disagree.
/// </remarks>
public sealed class CultureSwitcher
{
    private readonly CultureRouter _cultureRouter;
    private readonly ICultureState _cultureState;
    private readonly NavigationManager _navigationManager;

    /// <summary>Creates the switcher over the application's cultures and the user scope's culture state and navigation.</summary>
    public CultureSwitcher(CultureRouter cultureRouter, ICultureState cultureState, NavigationManager navigationManager)
    {
        ArgumentNullException.ThrowIfNull(cultureRouter);
        ArgumentNullException.ThrowIfNull(cultureState);
        ArgumentNullException.ThrowIfNull(navigationManager);

        _cultureRouter = cultureRouter;
        _cultureState = cultureState;
        _navigationManager = navigationManager;
    }

    /// <summary>The route of the culture state's UI culture.</summary>
    public CultureRoute CurrentRoute
        => _cultureRouter.GetRoute(_cultureState.UICulture);

    /// <summary>The current page's address in the culture of <paramref name="route"/>, query included.</summary>
    public Uri GetUri(CultureRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var address = ReadAddress().Address;
        var fragmentStart = address.IndexOf('#', StringComparison.Ordinal);

        return GetUri(route, fragmentStart < 0 ? address : address[..fragmentStart]);
    }

    /// <summary>The address <paramref name="relativeAddress"/>, relative to the application's root, in the culture of <paramref name="route"/>, query included.</summary>
    public Uri GetUri(CultureRoute route, string relativeAddress)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(relativeAddress);

        return new Uri(ReadAddress().Root, _cultureRouter.GetUrl(route, relativeAddress).TrimStart('/'));
    }

    /// <summary>Loads the current page's address in the culture of <paramref name="route"/>.</summary>
    /// <remarks>
    /// The address is loaded as a new document, whose circuit reconciles its culture state with the address as it
    /// opens; interactive routing has no page at another culture's address.
    /// </remarks>
    public void SwitchTo(CultureRoute route)
        => _navigationManager.NavigateTo(GetUri(route).AbsoluteUri, forceLoad: true);

    /// <summary>Changes the culture state to the culture the current address names.</summary>
    internal Task ReconcileAsync()
        => ReconcileAsync(new Uri(_navigationManager.Uri));

    /// <summary>Changes the culture state to the culture <paramref name="address"/> names.</summary>
    internal Task ReconcileAsync(Uri address)
    {
        // An address outside the application's root belongs to another application and names none of its cultures.
        if (GetRelativeAddress(ReadRoot(), address) is not { } relativeAddress)
        {
            return Task.CompletedTask;
        }

        var pathEnd = relativeAddress.IndexOfAny(['?', '#']);
        var path = PathString.FromUriComponent("/" + (pathEnd < 0 ? relativeAddress : relativeAddress[..pathEnd]));

        // An address naming cultures in two consecutive segments names neither, and the middleware refuses it.
        if (_cultureRouter.HasConsecutiveCultures(path))
        {
            return Task.CompletedTask;
        }

        return _cultureState.ChangeAsync(_cultureRouter.Match(path).Route.Culture);
    }

    // The address relative to the application's root, its culture segment included, where it lies under the root's
    // origin and path; the root's path compares as a path base does, ignoring case.
    private static string? GetRelativeAddress(Uri root, Uri address)
    {
        if (Uri.Compare(root, address, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return null;
        }

        var rootPath = root.AbsolutePath;
        var pathQueryAndFragment = address.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped);

        if (pathQueryAndFragment.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return pathQueryAndFragment[rootPath.Length..];
        }

        // The root without its trailing slash, /app for the root /app/, is the root's own address.
        var rootAddress = rootPath[..^1];

        return pathQueryAndFragment.StartsWith(rootAddress, StringComparison.OrdinalIgnoreCase) && (pathQueryAndFragment.Length == rootAddress.Length || pathQueryAndFragment[rootAddress.Length] is '?' or '#')
            ? pathQueryAndFragment[rootAddress.Length..]
            : null;
    }

    // The application's root and the current address relative to it.
    private (Uri Root, string Address) ReadAddress()
    {
        var root = ReadRoot();

        return (root, GetRelativeAddress(root, new Uri(_navigationManager.Uri)) ?? string.Empty);
    }

    // The application's root, the document's base without the culture segment it ends in: a base in a culture, /app/he/
    // where the root is /app/, ends in that culture's token.
    private Uri ReadRoot()
    {
        var baseUri = new Uri(_navigationManager.BaseUri);
        var basePath = baseUri.AbsolutePath.TrimEnd('/');
        var lastSegmentStart = basePath.LastIndexOf('/', StringComparison.Ordinal) + 1;
        var lastSegment = PathString.FromUriComponent("/" + basePath[lastSegmentStart..]);

        if (lastSegment.Value is "/" || _cultureRouter.Match(lastSegment).Path != PathString.FromUriComponent("/"))
        {
            return baseUri;
        }

        return new Uri(baseUri, basePath[..lastSegmentStart]);
    }
}
