# Forma.Framework.Globalization

Culture routing for an ASP.NET Core application: the request culture comes from the first URL path
segment, every page has one canonical address per culture, and a `CultureRouter` builds the link
that addresses any page in any culture. The package references the shared ASP.NET Core framework.

## Install and register

```pwsh
dotnet add package Forma.Framework.Globalization
```

```csharp
using Forma.Framework.Globalization;

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

`AddCultureRouting` validates the options and registers a singleton `CultureRouter`;
`UseCultureRouting` adds the middleware, which belongs before routing so that endpoints see a path
with the culture segment already removed. With the options above, `/about` serves English,
`/he/about` Hebrew and `/es/about` Latin American Spanish.

## The options

| Member | What it is |
| --- | --- |
| `CultureRouterOptions.Routes` | Every culture the application serves, in the order they are listed. |
| `CultureRouterOptions.DefaultCulture` | The `Token` of the culture serving a request that names none. |
| `CultureRouterOptions.Prefix` | `CulturePrefix.Always` (the default) addresses every culture by a token; `CulturePrefix.ExceptDefault` addresses the default culture without one. |
| `CultureRoute.Token` | The canonical first path segment selecting the culture. |
| `CultureRoute.Culture` | The `CultureInfo` applied to a request that selects the route. |
| `CultureRoute.Aliases` | Tokens that also select the route and redirect to `Token`. |
| `CultureRoute.HrefLang` | The value emitted in `hreflang` annotations; defaults to `Token`. |

`CultureRouter` throws `ArgumentException` when no route is registered, when `DefaultCulture` is not
the `Token` of a route (an alias is not enough), when a token contains `/` or is claimed by more than
one route, or when a route's `HrefLang` is not a language with an optional script and region, such
as `en`, `en-GB`, `zh-Hant` or `zh-Hant-TW`. A token that is not itself a usable `hreflang` sets
one explicitly: `new() { Token = "latam", Culture = new("es-419"), HrefLang = "es" }`.

## What the middleware does

For a document request, `CultureRoutingMiddleware` matches the first segment against every token
and alias, applies that route's culture — `CultureInfo.CurrentCulture`,
`CultureInfo.CurrentUICulture`, the `Content-Language` response header and the
`IRequestCultureFeature` Blazor reads for an interactive circuit — and forwards the request with the
culture segment removed. A request whose address is not canonical for the culture it names — an
alias, a token in the wrong case, a token on the default culture under `ExceptDefault`, or no token
under `Always` — is redirected with `302 Found` to the canonical address, query string and path base
included. A first segment that matches no token is left in place, so an unknown culture reaches
routing as an ordinary path. A path naming a culture in two consecutive segments, such as `/he/en`,
is answered with `400 Bad Request`.

A served document also receives a `forma-culture` cookie holding the route's token. A request that
is not a document — one under `/_`, such as the `/_blazor` endpoint, or one whose last segment has
an extension — takes its culture from that cookie, since its own path carries no segment, and is
neither redirected nor given a cookie. The cookie never chooses the culture of a document: the URL
alone does.

## Building links

`CultureRouter` is the registered service a component or page injects to address a culture.

| Member | What it does |
| --- | --- |
| `Routes` | Every route, in registration order. |
| `DefaultRoute` | The route serving a request that names none. |
| `CurrentRoute` | The route whose culture is `CultureInfo.CurrentUICulture`, or `DefaultRoute` when none matches. |
| `GetPath(route, path)` | The `PathString` addressing `route` for a path, stripping any culture segment the path already carries. |
| `GetUrl(route, relativeUrl)` | The relative URL addressing `route` for a base-relative URL, query string included. |
| `Match(path)` | The `CultureMatch` for a path: the route it names or the default, the path with the segment removed, the canonical path, and whether the path already is canonical. |
| `HasConsecutiveCultures(path)` | Whether a path names a culture in two consecutive segments. |

```razor
@inject CultureRouter Router
@inject NavigationManager Navigation

<a href="@Router.GetPath(Router.CurrentRoute, "/about")">About</a>

@foreach (var route in Router.Routes)
{
    <link rel="alternate" hreflang="@route.HrefLang"
          href="@Router.GetUrl(route, Navigation.ToBaseRelativePath(Navigation.Uri))" />
}
```

`GetPath` keeps a link on the reader's current culture, and `GetUrl` over the current address moves
the reader to another culture on the same page. `GetPath`, `GetUrl` and `Match` throw
`ArgumentException` for a path naming consecutive cultures.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
