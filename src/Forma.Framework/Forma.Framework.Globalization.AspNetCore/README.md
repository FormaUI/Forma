# Forma.Framework.Globalization.AspNetCore

Culture routing for ASP.NET Core: the first segment of the URL path selects the request culture,
as in `/he/about`, every page has one canonical address per culture, and `CultureRouter` builds
the link to any page in any culture. It suits a localized site whose addresses are shared,
bookmarked and indexed per language.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Globalization.AspNetCore
```

```csharp
using Forma.Framework.Globalization.AspNetCore;

builder.Services.AddCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", Culture = new("en") },
        new() { Token = "es", Culture = new("es-419"), Aliases = ["es-419"] },
        new() { Token = "he", Culture = new("he") }
    ]
});

var app = builder.Build();

app.UseCultureRouting();
app.UseRouting();
```

`AddCultureRouting` validates the options and registers `CultureRouter` as a singleton, the
`CultureSwitcher` each user scope switches through, and the culture state those scopes hold.
`UseCultureRouting` goes before routing, so endpoints see the path with the culture segment
removed, and after `UsePathBase` under a path base; an endpoint chosen for the address with its
culture segment, as `UsePathBase` chooses one, is chosen again for the path without it. With
these options `/about` serves English, `/he/about` Hebrew and `/es/about` Latin American
Spanish.

| Option | What it is |
| --- | --- |
| `CultureRouterOptions.Routes` | Every culture the application serves, in order. |
| `CultureRouterOptions.DefaultCulture` | The `Token` of the culture serving a request that names none. |
| `CultureRouterOptions.Prefix` | `CulturePrefix.Always`, the default, puts a token on every culture; `CulturePrefix.ExceptDefault` leaves the default culture without one. |
| `CultureRoute.Token` | The canonical first path segment selecting the culture. |
| `CultureRoute.Culture` | The `CultureInfo` a request that selects the route runs in. |
| `CultureRoute.Aliases` | Further tokens that select the route and redirect to `Token`. |
| `CultureRoute.HrefLang` | The value written in `hreflang` annotations; `Token` by default. |

`CultureRouter` throws `ArgumentException` for options with no route, a `DefaultCulture` that is
not a route's `Token`, a token containing `/` or claimed by two routes, or an `HrefLang` that is
not a language with an optional script and region, such as `en`, `en-GB` or `zh-Hant-TW`. A
token that is not a valid `hreflang` value sets one:

```csharp
new CultureRoute { Token = "latam", Culture = new("es-419"), HrefLang = "es" }
```

## Requests

For a page request, the middleware:

- sets `CultureInfo.CurrentCulture`, `CultureInfo.CurrentUICulture`, the `Content-Language`
  header and the `IRequestCultureFeature` a Blazor interactive circuit reads, from the route the
  first segment names or the default route;
- removes the culture segment from the path before routing;
- redirects with `302 Found` to the canonical address, keeping the path base and query, when the
  address used an alias, a token in another case, a token on the default culture under
  `ExceptDefault`, or no token under `Always`;
- stores the route's token in a `forma-culture-route` cookie at the path base, which the
  browser's script can read and write, and expires the `HttpOnly` `forma-culture` cookie an
  earlier version wrote at `/`. Under a path base the cookie's name carries it, escaped,
  `forma-culture-route%2Fapp` under `/app`, so applications sharing a host each read their own.

A request for a static file, one whose last segment has an extension, or for a framework endpoint
under `/_`, such as `/_blazor` or `/he/_blazor`, judged by its path without the culture segment,
neither redirects nor writes the cookie. It takes the culture its culture segment names, the cookie's
where it has none, the earlier `forma-culture` cookie's where the request carries only that one,
and the default culture where neither names a route, then goes straight on to routing. A first
segment that is no token stays in the path, and a path naming cultures in two consecutive
segments, such as `/he/en`, is answered with `400 Bad Request`.

An interactive circuit's culture state follows its address too: as the circuit opens, and again
as a paused circuit resumes, the state takes the culture the address names, whatever culture the
circuit's connection arrived with, so a tab at `/about` stays English after another tab has
loaded `/he/about`.

## Switching culture

A page or component injects `CultureSwitcher` to switch to another culture. The switch loads the
current page's address in that culture as a new document, and the culture state follows the
address as its circuit opens.

| Member | What it does |
| --- | --- |
| `CurrentRoute` | The route of the culture state's UI culture. |
| `GetUri(route)` | The current page's address in a route's culture, query string included. |
| `GetUri(route, relativeAddress)` | An address relative to the application's root, such as `about?tab=2`, in a route's culture. |
| `SwitchTo(route)` | Loads the current page's address in a route's culture. |

```razor
@using Forma.Framework.Globalization.AspNetCore
@inject CultureRouter Router
@inject CultureSwitcher Switcher

@foreach (var route in Router.Routes)
{
    <a href="@Switcher.GetUri(route)" aria-current="@(route == Switcher.CurrentRoute ? "true" : null)">@route.Token</a>
}
```

## Building links

A link in markup takes its address from `CultureSwitcher.GetUri`, which knows the application's
root under any path base and document base. `CultureRouter` builds the paths beneath it:

| Member | What it does |
| --- | --- |
| `Routes` | Every route, in registration order. |
| `DefaultRoute` | The route serving a request that names none. |
| `GetRoute(culture)` | The route serving a culture, or `DefaultRoute`. |
| `GetPath(route, path)` | The `PathString` of a path in a route's culture, replacing any culture segment it carries. |
| `GetUrl(route, relativeUrl)` | A URL relative to the application's root in a route's culture, from that root, query string included. |
| `Match(path)` | The `CultureMatch` of a path: its `Route`, its `Path` without the culture segment, its `CanonicalPath` and whether it `IsCanonical`. |
| `HasConsecutiveCultures(path)` | Whether a path names cultures in two consecutive segments. |

```razor
@using Forma.Framework.Globalization.AspNetCore
@inject CultureRouter Router
@inject CultureSwitcher Switcher

<a href="@Switcher.GetUri(Switcher.CurrentRoute, "about")">About</a>

@foreach (var route in Router.Routes)
{
    <link rel="alternate" hreflang="@route.HrefLang" href="@Switcher.GetUri(route)" />
}
```

`GetUri` over the current route keeps a link in the reader's culture, and over another route
links the same page in that culture.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
