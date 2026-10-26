# Forma.Framework.Globalization.AspNetCore

Culture routing for ASP.NET Core: the first segment of the URL path selects the request culture,
as in `/he/about`, every page has one canonical address per culture, and `CultureRouter` builds
the link to any page in any culture. It suits a localized site whose addresses are shared,
bookmarked and indexed per language.
On an Interactive Server circuit it keeps the ambient culture and the page's `<html lang dir>` in step
with the culture state, so every event runs in the state's cultures and the page reads in its language.

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

`AddCultureRouting` validates the options and registers `CultureRouter` as a singleton.
`UseCultureRouting` goes before routing, so endpoints see the path with the culture segment
removed. With these options `/about` serves English, `/he/about` Hebrew and `/es/about` Latin
American Spanish.

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
- stores the route's token in a `forma-culture` cookie.

A request for a static file, one whose last segment has an extension, or for a framework endpoint
under `/_`, such as `/_blazor`, takes its culture from that cookie, or from its path when there
is none, and goes straight on to routing. A first segment that is no token stays in the path, and a path naming cultures in two consecutive segments, such as
`/he/en`, is answered with `400 Bad Request`.

## Building links

A page or component injects `CultureRouter` to address a culture:

| Member | What it does |
| --- | --- |
| `Routes` | Every route, in registration order. |
| `DefaultRoute` | The route serving a request that names none. |
| `CurrentRoute` | The route of `CultureInfo.CurrentUICulture`, or `DefaultRoute`. |
| `GetPath(route, path)` | The `PathString` of a path in a route's culture, replacing any culture segment it carries. |
| `GetUrl(route, relativeUrl)` | A base-relative URL in a route's culture, query string included. |
| `Match(path)` | The `CultureMatch` of a path: its `Route`, its `Path` without the culture segment, its `CanonicalPath` and whether it `IsCanonical`. |
| `HasConsecutiveCultures(path)` | Whether a path names cultures in two consecutive segments. |

```razor
@using Forma.Framework.Globalization.AspNetCore
@inject CultureRouter Router
@inject NavigationManager Navigation

<a href="@Router.GetPath(Router.CurrentRoute, "/about")">About</a>

@foreach (var route in Router.Routes)
{
    <link rel="alternate" hreflang="@route.HrefLang"
          href="@Router.GetUrl(route, Navigation.ToBaseRelativePath(Navigation.Uri))" />
}
```

`GetPath` keeps a link in the reader's culture, and `GetUrl` over the current address links the
same page in another culture.

## The circuit's culture

`AddFormaGlobalizationServices()`, which `AddFormaUI()` calls, registers the culture state of
`Forma.Framework.Globalization` and a circuit handler that sets `CultureInfo.CurrentCulture` and
`CultureInfo.CurrentUICulture` from the state before every inbound activity: each UI event and each call
from script into .NET. Code that reads the ambient culture in an event after a switch reads the new
cultures, and a switch in one circuit leaves every other circuit's in place.

The same call registers the document language of
[`Forma.Framework.Globalization.Interop`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Globalization.Interop/README.md),
which the circuit handler starts as each connection comes up, so the page's `lang` and `dir` follow the
state's UI culture until the circuit closes.

```csharp
builder.Services.AddFormaGlobalizationServices();
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
