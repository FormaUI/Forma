# Forma.Framework.Culture.Browser.Interop

The document culture synchronizer keeps the page's `<html lang dir>` on a user scope's culture state, so a
page switched to Hebrew in place is laid out right to left and read as Hebrew by browsers and screen readers.
`lang` is the state's UI culture's name and `dir` its direction.

## Use it

The package comes with `formaui-net.Blazor`, whose `AddFormaUI()` registers it through
`AddFormaCultureBrowserSynchronization()`, and `formaui-net.Framework.Culture.Blazor.Server`'s circuit
handler starts it with each circuit:

| Service | Registered as |
| --- | --- |
| `ICultureState` | `DefaultCultureState`, scoped, through `AddFormaCultureServices()`. |
| `ICultureStateIntegration` | The same instance as the scope's `ICultureState`, through `AddFormaCultureServices()`. |
| `DocumentCultureSynchronizer` | Scoped, one per user scope. |

Each is added with `TryAdd`, so a host's own registration made first is the one used.
`DocumentCultureSynchronizer.BeginFollowing()` starts keeping the document on the state, and brings it in
step again when called as a connection to the browser comes up.

## How the document follows the state

- One culture state owns the document at a time; it sets both attributes as it starts and on each
  change.
- An enhanced navigation or a streaming update can rewrite the attributes, so the owner sets its pair
  again after each.
- Where an `ICultureRouteHandler` is registered, as `AddFormaCultureRouting` registers one, the
  owner follows addresses: from an enhanced navigation's start the document keeps the pair its response
  serves, the landing is reported to the handler with its address, and the owner sets its latest pair
  once the circuit replies that the browser holds the pair the state is left in.
- When the client closes the owning circuit, once no interactive server component remains or before a
  paused circuit resumes, the document returns to what the server last served, removing an attribute the
  server left out.

The package's JavaScript initializer, which Blazor loads on its own, tracks what the server serves and
releases the owner as its circuit closes. The script is served from `_content/forma/culture/scripts/`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
