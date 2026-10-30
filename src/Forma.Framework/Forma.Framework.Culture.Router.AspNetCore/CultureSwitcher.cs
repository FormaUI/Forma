namespace Forma.Framework.Culture.Router.AspNetCore;

using Forma.Framework.Culture.Router.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>Switches a user scope to another culture by navigating to the current page's address in it.</summary>
/// <remarks>
/// The switcher only requests the address; the culture state follows the address once the navigation that request
/// becomes settles, so an address and the culture it shows never disagree.
/// </remarks>
public sealed class CultureSwitcher : IAsyncDisposable, IDisposable
{
    private readonly CultureRouter _cultureRouter;
    private readonly ICultureState _cultureState;
    private readonly ICultureSwitch _cultureSwitch;
    private readonly NavigationManager _navigationManager;
    private readonly JSEnvironment _environment;
    private CultureCookie? _cultureCookie;
    private Task _cookieWriting = Task.CompletedTask;

    /// <summary>Creates the switcher over the application's cultures and the user scope's culture state, culture switch, navigation and JS runtime.</summary>
    public CultureSwitcher(CultureRouter cultureRouter, ICultureState cultureState, ICultureSwitch cultureSwitch, NavigationManager navigationManager, IJSRuntime js, ILogger<CultureSwitcher> logger)
    {
        ArgumentNullException.ThrowIfNull(cultureRouter);
        ArgumentNullException.ThrowIfNull(cultureState);
        ArgumentNullException.ThrowIfNull(cultureSwitch);
        ArgumentNullException.ThrowIfNull(navigationManager);

        _cultureRouter = cultureRouter;
        _cultureState = cultureState;
        _cultureSwitch = cultureSwitch;
        _navigationManager = navigationManager;
        _environment = new JSEnvironment(js, logger, Guid.CreateVersion7());
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

    /// <summary>Navigates to the current page's address in the culture of <paramref name="route"/>.</summary>
    /// <remarks>
    /// Under <see cref="CultureSwitchMode.FullLoad"/> the address is loaded as a new document, whose circuit reconciles its
    /// culture state with the address as it opens; interactive routing has no page at another culture's address. Under
    /// <see cref="CultureSwitchMode.InPlace"/> the navigation stays in the circuit, and the culture route page it reaches
    /// follows its route.
    /// </remarks>
    public void SwitchTo(CultureRoute route)
        => _navigationManager.NavigateTo(GetUri(route).AbsoluteUri, forceLoad: _cultureRouter.Switch is not CultureSwitchMode.InPlace);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var cultureCookie = _cultureCookie;
        _cultureCookie = null;

        await _cookieWriting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (cultureCookie is not null)
        {
            await cultureCookie.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    /// <remarks>Releases the managed state alone; the browser's module is released by <see cref="DisposeAsync"/>.</remarks>
    public void Dispose()
        => _cultureCookie = null;

    /// <summary>Follows the culture route a page was routed with, reporting whether the page goes on to render at its address.</summary>
    /// <remarks>
    /// A segment that isn't a culture's token is not found wherever the page renders. A page an interactive router rendered
    /// within <see cref="CultureRouteScope"/> under <see cref="CultureSwitchMode.InPlace"/> follows its route: an address that isn't
    /// its culture's canonical one is replaced by the canonical one, and otherwise the route's culture is switched to, its
    /// switch observed rather than awaited, and the cookie is written where the state changed before the call returned.
    /// </remarks>
    /// <param name="token">The culture segment the page was routed with, <see langword="null"/> for its plain route.</param>
    /// <param name="isRoutedInPlace">Whether an interactive router rendered the page within <see cref="CultureRouteScope"/>.</param>
    internal bool FollowRoute(string? token, bool isRoutedInPlace)
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

        var match = _cultureRouter.Match(new PathString("/" + token));

        if (!match.IsCanonical)
        {
            _navigationManager.NavigateTo(GetUri(match.Route).AbsoluteUri, replace: true);

            return false;
        }

        var revision = _cultureState.Revision;
        var change = _cultureSwitch.SwitchAsync(match.Route.Culture);

        _ = ObserveAsync(change, match.Route);

        if (_cultureState.Revision != revision)
        {
            WriteCookie(match.Route);
        }

        return true;
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

    // A switch completes after the page it was made for has gone on to render, so a failure is logged here.
    private async Task ObserveAsync(Task change, CultureRoute route)
    {
        await change.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (change.Exception?.InnerException is { } exception)
        {
            CultureRouterLog.CultureSwitchFailed(_environment.Logger, exception, route.Culture.Name);
        }
    }

    // Each write is queued behind the last, so the browser keeps the culture of the latest change.
    private void WriteCookie(CultureRoute route)
    {
        var root = ReadRoot();
        var pathBase = PathString.FromUriComponent(root.AbsolutePath[..^1]);
        var isSecure = string.Equals(root.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        _cultureCookie ??= new CultureCookie(_environment);
        _cookieWriting = WriteCookieAsync(_cultureCookie, _cookieWriting, route.Token, pathBase, isSecure);
    }

    private static async Task WriteCookieAsync(CultureCookie cultureCookie, Task lastWriting, string token, PathString pathBase, bool isSecure)
    {
        await lastWriting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await cultureCookie.WriteAsync(
            CultureRoutingMiddleware.GetCookieName(pathBase),
            token,
            CultureRoutingMiddleware.GetCookiePath(pathBase),
            CultureRoutingMiddleware.CookieLifetime,
            isSecure).ConfigureAwait(false);
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
