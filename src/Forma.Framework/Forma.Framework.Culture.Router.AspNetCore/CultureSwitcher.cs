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
    {
        var address = ReadAddress().Address;
        var pathEnd = address.IndexOfAny(['?', '#']);
        var path = PathString.FromUriComponent("/" + (pathEnd < 0 ? address : address[..pathEnd]));

        // An address naming cultures in two consecutive segments names neither, and the middleware refuses it.
        if (_cultureRouter.HasConsecutiveCultures(path))
        {
            return Task.CompletedTask;
        }

        return _cultureState.ChangeAsync(_cultureRouter.Match(path).Route.Culture);
    }

    // The application's root and the current address relative to it, its culture segment included. A document base
    // in a culture, /app/he/ where the application's root is /app/, ends in that culture's token.
    private (Uri Root, string Address) ReadAddress()
    {
        var baseUri = new Uri(_navigationManager.BaseUri);
        var relativeAddress = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        var basePath = baseUri.AbsolutePath.TrimEnd('/');
        var lastSegmentStart = basePath.LastIndexOf('/', StringComparison.Ordinal) + 1;
        var lastSegment = PathString.FromUriComponent("/" + basePath[lastSegmentStart..]);

        if (lastSegment.Value is "/" || _cultureRouter.Match(lastSegment).Path != PathString.FromUriComponent("/"))
        {
            return (baseUri, relativeAddress);
        }

        return (new Uri(baseUri, basePath[..lastSegmentStart]), basePath[lastSegmentStart..] + "/" + relativeAddress);
    }
}
