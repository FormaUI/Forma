# Forma.Framework.Culture.Router

Culture routing: the first segment of the URL path selects the page's culture pair, as in `/he/about`,
every page has one canonical address per route, and `CultureRouter` builds the link to any page in any
route. It suits a localized site whose addresses are shared, bookmarked and indexed per language. The
rules here hold in every runtime; a server host adds the request side from
[`formaui-net.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router.AspNetCore/README.md).

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Culture.Router --prerelease
dotnet add package formaui-net.Framework.Culture.Router.AspNetCore --prerelease
```

```csharp
using Forma.Blazor;
using Forma.Framework.Culture.Router;
using Forma.Framework.Culture.Router.AspNetCore;

builder.AddFormaServerIntegration();
builder.Services.AddFormaCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", UICulture = new("en") },
        new() { Token = "es", UICulture = new("es-419"), Aliases = ["es-419"] },
        new() { Token = "he", UICulture = new("he") },
        new() { Token = "he-de", Culture = new("de-DE"), UICulture = new("he"), HrefLang = "he" }
    ]
});

var app = builder.Build();

app.UseFormaCultureRouting();
app.UseRouting();
```

`AddFormaCultureRouting` validates the options and registers `CultureRouter` as a singleton, the page's
culture addressing it gives, and the `CultureSwitcher` each user scope switches through, over the culture
services the product's registrar registers, so it goes before or after that registrar. Culture
addresses then decide the page's culture: the switch that
[`formaui-net.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Server/README.md)'s
`AddFormaServerIntegration()` registers navigates to the address of the pair it is asked for, and the page's
culture coordinator accepts each navigation's landing: an enhanced navigation's from the pair its
response was rendered in, and an interactive one's from the route its page was routed with. With these
options `/about` serves English, `/he/about` Hebrew, `/he-de/about` Hebrew text with German formats, and
`/es/about` Latin American Spanish.

| Option | What it is |
| --- | --- |
| `CultureRouterOptions.Routes` | Every route the application serves, in order. |
| `CultureRouterOptions.DefaultCulture` | The `Token` of the route serving a request that names none. |
| `CultureRouterOptions.Prefix` | `CulturePrefix.Always`, the default, puts a token on every route; `CulturePrefix.ExceptDefault` leaves the default route without one. |
| `CultureRouterOptions.Switch` | `CultureSwitchMode.FullLoad`, the default, loads another route's address as a new document; `CultureSwitchMode.InPlace` reaches it in the same document, as [switching in place](#switching-in-place) sets out. |
| `CultureRouterOptions.SwitchTimeout` | How long a switch in place waits for its navigation to land; 30 seconds by default. |
| `CultureRoute.Token` | The canonical first path segment selecting the route. |
| `CultureRoute.UICulture` | The culture choosing the strings and direction of a page the route selects. |
| `CultureRoute.Culture` | The culture formatting dates, numbers and collation there; the UI culture where none is named. |
| `CultureRoute.Aliases` | Further tokens that select the route and redirect to `Token`. |
| `CultureRoute.HrefLang` | The value written in `hreflang` annotations; `Token` by default. |

`CultureRouter` throws `ArgumentException` for options with no route, a `DefaultCulture` that is not a
route's `Token`, a token containing `/` or claimed by two routes, or an `HrefLang` that is not a language
with an optional script and region, such as `en`, `en-GB` or `zh-Hant-TW`. A token that is not a valid
`hreflang` value sets one, as the `he-de` route does above.

## The document's base

The document's `<base>` is in the page's route, `/he/` for a Hebrew page and `/app/he/` under the path
base `/app`, so interactive routing finds the pages of the document's route, and another route's
address, where it finds none, loads as a new document in that route. Under `CultureSwitchMode.InPlace`
it is the application's root, `/` or `/app/`.

`FormaPageHead`, from
[`formaui-net.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Server/README.md),
writes that base where culture routing is registered, beside the address policy the page's coordinator
switches by, so `App.razor` gives it no `BaseHref`, which it refuses there:

```razor
<head>
    <FormaPageHead Title="My App" />
</head>
```

The application's root is the base without its culture segment, so a path base doesn't end in a segment
that is a route's token.

## Switching

A page or component switches through `ICultureSwitch`, as every Forma host does, or through
`CultureSwitcher.SwitchToAsync` with a route. Under culture routing a switch is a navigation to the
current page's address in the route carrying the pair: by default it loads the address as a new
document, which starts in the pair its address names; under `CultureSwitchMode.InPlace` it stays in the
document, completing once its navigation's landing was accepted, as canceled where the navigation was
canceled or replaced by another, and with `TimeoutException` where it didn't land within `SwitchTimeout`, as a
navigation a handler refused doesn't. A newer switch takes the place of one still waiting, and the page follows a
navigation that lands after its switch timed out. A pair no route carries throws `InvalidOperationException` before
anything changes.

| Member | What it does |
| --- | --- |
| `CurrentRoute` | The route carrying the culture state's pair, or the default route. |
| `GetUri(route)` | The current page's address in a route, query string included. |
| `GetUri(route, relativeAddress)` | An address relative to the application's root, such as `about?tab=2`, in a route. |
| `SwitchToAsync(route)` | Switches to a route's pair, navigating to the current page's address in it. |

```razor
@using Forma.Framework.Culture.Router
@inject CultureRouter Router
@inject CultureSwitcher Switcher

@foreach (var route in Router.Routes)
{
    <a href="@Switcher.GetUri(route)" aria-current="@(route == Switcher.CurrentRoute ? "true" : null)">@route.Token</a>
}
```

## Switching in place

`CultureSwitchMode.InPlace` keeps the document and its circuits through a switch, so the user's work stays
where it was. Under an interactive router, interactive routing reaches every route's address, and the
culture state follows the page it routes to. Three things make it so:

- `Switch = CultureSwitchMode.InPlace` in the options, under which [the document's base](#the-documents-base)
  is the application's root, so every route's address lies within it.
- `CultureRouteScope` around the `Router`, inside the host's `Routes` component, so it renders in the
  router's own tree:

  ```razor
  @using Forma.Framework.Culture.Router

  <CultureRouteScope>
      <Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
          <Found Context="routeData">
              <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
          </Found>
      </Router>
  </CultureRouteScope>
  ```

- Every page carrying its culture route beside its plain one, and deriving `CultureRoutePage`:

  ```razor
  @page "/about"
  @page "/{culture}/about"
  @inherits CultureRoutePage
  ```

`CultureRoutePage` supplies the `Culture` parameter and acts on it before the page's lifecycle runs:

- a segment that isn't a route's token renders the `Router`'s `NotFoundPage`, and a full request answers
  `404 Not Found` with it;
- an alias, a token in another case, the default route's token under `ExceptDefault`, or a plain address
  under `Always` is replaced by the canonical address;
- otherwise, where an interactive router renders the page within `CultureRouteScope`, the page reports its
  landing to the page's coordinator, which accepts the route's pair and writes the `forma-culture-route`
  cookie from the browser, since no request carries it, and the page's `OnInitialized` and
  `OnParametersSet` run once its culture state holds the pair, in the state and as the ambient culture.
  The layout and other components render again as each is notified; a notification that fails is
  logged at `Error` under `CultureCoordinatorRegistration`, and a landing the coordinator couldn't take
  under `CultureSwitcher`, the page rendering in the culture state as it stands.

A page that is an island of its own never receives `CultureRouteScope`, and its circuit follows the
address an enhanced navigation lands at instead, as it does without `InPlace`. Nothing reports a missing
`CultureRouteScope`, since an island's absence of it is expected: without it no page follows its route,
and a navigation within one route looks the same either way, so check a switch.

A page that doesn't derive `CultureRoutePage` follows nothing. One that carries only its plain route has
no page at another route's address: Blazor's router loads the address as a new document for a link, which
starts in the pair its address names, and renders its not-found page for `NavigateTo`.

### Where the pages are islands

A host whose pages each carry their own interactive render mode, under a static router, needs only
`Switch = CultureSwitchMode.InPlace`, under which [the document's base](#the-documents-base) is the
application's root. A switch or a link to another route's address is then an enhanced navigation: the
server renders the page in that route, each island the page already holds keeps its state, and every
circuit's culture state follows the pair the navigation landed at. The pages keep their plain routes.

An island keeps the markup it rendered before the switch until it renders again, so a component of the
host's own that shows text in the culture follows as Forma's components do. It renders again on each
change of `ICultureState`, and reads its strings through a locale bound to the state, which
`Forma.Framework.Localization` creates, so a render that work begun before the switch asks for is in the
new culture too:

```razor
@implements IDisposable
@inject ICultureState CultureState
@inject IStringLocaleFactory LocaleFactory

<p>@(_isSaved ? L["Saved."] : L["Not saved yet."])</p>

@code {
    private IStringLocale? _locale;
    private bool _isSaved;

    private IStringLocale L
        => _locale ??= LocaleFactory.Create(GetType(), CultureState);

    protected override void OnInitialized()
        => CultureState.Subscribe(RenderAgainAsync);

    public void Dispose()
        => CultureState.Unsubscribe(RenderAgainAsync);

    private Task RenderAgainAsync()
        => InvokeAsync(StateHasChanged);
}
```

Text that presents the component's current state, as `_isSaved` does here, is looked up as the component
renders, and a label it builds once, such as an option it offers, it builds again before it renders
again, keeping the reader's choices among them. Text an event produced, an announcement or a validation
message, keeps the language it was produced in until the next such event.

## Building links

A link in markup takes its address from `CultureSwitcher.GetUri`, which knows the application's root under
any path base and document base. `CultureRouter` builds the paths beneath it, reading a path unescaped, as
a request does, and a relative URL escaped, as a link writes it:

| Member | What it does |
| --- | --- |
| `Routes` | Every route, in registration order. |
| `DefaultRoute` | The route serving a request that names none. |
| `FindRoute(culture, uiCulture)` | The route carrying a pair, or `null`. |
| `FindRoute(token)` | The route a token or alias selects, in any case, or `null`. |
| `GetPath(route, path)` | A path in a route, replacing any culture segment it carries. |
| `GetUrl(route, relativeUrl)` | A URL relative to the application's root in a route, from that root, query string included. |
| `Match(path)` | The `CultureMatch` of a path: its `Route`, its `Path` without the culture segment, its `CanonicalPath` and whether it `IsCanonical`. |
| `HasConsecutiveCultures(path)` | Whether a path names cultures in two consecutive segments. |

```razor
@using Forma.Framework.Culture.Router
@inject CultureRouter Router
@inject CultureSwitcher Switcher

<a href="@Switcher.GetUri(Switcher.CurrentRoute, "about")">About</a>

@foreach (var route in Router.Routes)
{
    <link rel="alternate" hreflang="@route.HrefLang" href="@Switcher.GetUri(route)" />
}
```

`GetUri` over the current route keeps a link in the reader's route, and over another route links the
same page there.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
