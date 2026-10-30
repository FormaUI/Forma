namespace Forma.Framework.Culture.Router;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>Switches a user scope to another route, and builds the current page's address in each.</summary>
/// <remarks>
/// A switch requests the route's pair through the culture switch, which, where culture addresses decide the culture,
/// navigates to the current page's address in the route; the culture state follows the address once the navigation
/// lands, so an address and the culture it shows never disagree.
/// </remarks>
public sealed class CultureSwitcher
{
    private readonly CultureRouter _cultureRouter;
    private readonly ICultureState _cultureState;
    private readonly ICultureSwitch _cultureSwitch;
    private readonly NavigationManager _navigationManager;
    private readonly CultureCoordinatorRegistration _registration;
    private readonly ILogger<CultureSwitcher> _logger;

    /// <summary>Creates the switcher over the application's routes and the user scope's culture state, culture switch, navigation and registration with the page's culture coordinator.</summary>
    public CultureSwitcher(CultureRouter cultureRouter, ICultureState cultureState, ICultureSwitch cultureSwitch, NavigationManager navigationManager, CultureCoordinatorRegistration registration, ILogger<CultureSwitcher> logger)
    {
        ArgumentNullException.ThrowIfNull(cultureRouter);
        ArgumentNullException.ThrowIfNull(cultureState);
        ArgumentNullException.ThrowIfNull(cultureSwitch);
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(logger);

        _cultureRouter = cultureRouter;
        _cultureState = cultureState;
        _cultureSwitch = cultureSwitch;
        _navigationManager = navigationManager;
        _registration = registration;
        _logger = logger;
    }

    /// <summary>The route of the culture state's pair, or the default route where none carries it.</summary>
    public CultureRoute CurrentRoute
        => _cultureRouter.FindRoute(_cultureState.Culture, _cultureState.UICulture) ?? _cultureRouter.DefaultRoute;

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

    /// <summary>Switches to the pair of <paramref name="route"/>, which navigates to the current page's address in it.</summary>
    /// <remarks>
    /// Under <see cref="CultureSwitchMode.FullLoad"/> the address loads as a new document, whose coordinator starts from the
    /// pair the address names, so the switch never completes in this document. Under <see cref="CultureSwitchMode.InPlace"/>
    /// the switch completes once its navigation's landing was accepted, and as canceled where the navigation didn't land.
    /// </remarks>
    public Task SwitchToAsync(CultureRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        return _cultureSwitch.SwitchAsync(route.Culture, route.UICulture);
    }

    /// <summary>Follows the culture route a page was routed with, reporting whether the page goes on to render at its address.</summary>
    /// <remarks>
    /// A segment that isn't a culture's token is not found wherever the page renders. A page an interactive router rendered
    /// within <see cref="CultureRouteScope"/> under <see cref="CultureSwitchMode.InPlace"/> follows its route: an address that isn't
    /// its route's canonical one is replaced by the canonical one, and otherwise the page reports its landing to the page's
    /// coordinator and goes on once its culture state has applied the route's pair.
    /// </remarks>
    /// <param name="token">The culture segment the page was routed with, <see langword="null"/> for its plain route.</param>
    /// <param name="isRoutedInPlace">Whether an interactive router rendered the page within <see cref="CultureRouteScope"/>.</param>
    internal async Task<bool> FollowRouteAsync(string? token, bool isRoutedInPlace)
    {
        if (token is not null && !_cultureRouter.IsToken(token))
        {
            _navigationManager.NotFound();

            return false;
        }

        if (!isRoutedInPlace || _cultureRouter.Switch is not CultureSwitchMode.InPlace)
        {
            return true;
        }

        var match = _cultureRouter.Match("/" + token);

        if (!match.IsCanonical)
        {
            _navigationManager.NavigateTo(GetUri(match.Route).AbsoluteUri, replace: true);

            return false;
        }

        try
        {
            await _registration.ReportLandingAsync(token);
        }
        catch (Exception exception) when (exception is InvalidOperationException or JSException or JSDisconnectedException or TaskCanceledException)
        {
            CultureRouterLog.LandingFailed(_logger, exception, token ?? "/");
        }

        return true;
    }

    /// <summary>Puts the culture state's cultures on the calling flow, where a page's lifecycle runs.</summary>
    /// <remarks>A pair the coordinator delivers is applied on the delivery's own flow, so a page that followed its route takes the state's cultures onto its own before its lifecycle.</remarks>
    internal void HoldStateCultures()
    {
        CultureInfo.CurrentCulture = _cultureState.Culture;
        CultureInfo.CurrentUICulture = _cultureState.UICulture;
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
        var lastSegment = Uri.UnescapeDataString(basePath[lastSegmentStart..]);

        return lastSegment.Length == 0 || !_cultureRouter.IsToken(lastSegment) ? baseUri : new Uri(baseUri, basePath[..lastSegmentStart]);
    }
}
