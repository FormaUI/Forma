# Forma.Framework.Culture.Router.AspNetCore

Culture routing's request side for ASP.NET Core: the middleware that runs a request in the pair its
address names, removes the culture segment before routing, redirects to the canonical address and keeps
the cookie naming the route last chosen. The routes, the switcher and the pages that follow their route
are [`formaui-net.Framework.Culture.Router`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router/README.md)'s,
whose README sets the whole arrangement out.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Culture.Router.AspNetCore
```

```csharp
using Forma.Framework.Culture.Router;
using Forma.Framework.Culture.Router.AspNetCore;

builder.Services.AddFormaCultureRouting(new CultureRouterOptions { ... });

var app = builder.Build();

app.UseFormaCultureRouting();
app.UseRouting();
```

`UseFormaCultureRouting` goes before routing, so endpoints see the path with the culture segment removed,
and after `UsePathBase` under a path base; an endpoint chosen for the address with its culture segment, as
`UsePathBase` chooses one, is chosen again for the path without it.

## Requests

For a page request, the middleware:

- sets `CultureInfo.CurrentCulture` to the route's culture and `CultureInfo.CurrentUICulture` to its UI
  culture, the `Content-Language` header to the UI culture, and the `IRequestCultureFeature` a Blazor
  interactive circuit reads, from the route the first segment names or the default route, each culture
  the application's `ICultureProvider`'s for its name where one is registered;
- removes the culture segment from the path before routing;
- redirects with `302 Found` to the canonical address, keeping the path base and query, when the address
  used an alias, a token in another case, a token on the default route under `ExceptDefault`, or no token
  under `Always`;
- stores the route's token in a `forma-culture-route` cookie at the path base, which the browser's script
  can read and write. Under a path base the cookie's name carries it, escaped,
  `forma-culture-route%2Fapp` under `/app`, so applications sharing a host each read their own.

A request for a static file, one whose last segment has an extension, or for a framework endpoint under
`/_`, such as `/_blazor` or `/he/_blazor`, judged by its path without the culture segment, neither
redirects nor writes the cookie. It takes the route its culture segment names, the cookie's where it has
none, and the default route where neither names one, then goes straight on to routing. A first segment
that is no token stays in the path, and a path naming cultures in two consecutive segments, such as
`/he/en`, is answered with `400 Bad Request`.

An interactive circuit's culture state follows its page: as the circuit registers with the page's culture
coordinator, and again as a paused circuit resumes, it adopts the pair the page was rendered in, which its
address named, whatever culture the circuit's connection arrived with, so a tab at `/about` stays English
after another tab has loaded `/he/about`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
