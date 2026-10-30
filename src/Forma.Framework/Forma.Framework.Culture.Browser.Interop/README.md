# Forma.Framework.Culture.Browser.Interop

The page's culture coordinator: one per document, it orders every culture switch on the page, gives each
the next number in the document's sequence, and delivers the accepted pair to the culture state of every
runtime registered with it, so islands of Interactive Server and of WebAssembly converge on one pair. It
keeps the page's `<html lang dir>` on the accepted pair, so a page switched to Hebrew in place is laid out
right to left and read as Hebrew by browsers and screen readers.

## Use it

The package comes with `formaui-net.Blazor`, whose `AddFormaUI()` registers it through
`AddFormaCultureCoordination()`, and `formaui-net.Framework.Culture.Blazor.Server`'s circuit handler
registers each circuit with it:

| Service | Registered as |
| --- | --- |
| `ICultureState` | `DefaultCultureState`, scoped, through `AddFormaCultureServices()`. |
| `ICultureStateIntegration` | The same instance as the scope's `ICultureState`, through `AddFormaCultureServices()`. |
| `CultureCoordinatorRegistration` | Scoped, one per user scope. |

Each is added with `TryAdd`, so a host's own registration made first is the one used.
`CultureCoordinatorRegistration.RegisterAsync()` registers the scope's state with the coordinator, which
adopts the pair the page accepted, and `CoordinatedCultureSwitch` requests each switch through it.

The coordinator starts from the culture marker `FormaPageHead` publishes in the page's head, the cultures
the server rendered the page in; a page without it can't be coordinated, and every registration fails
with an error naming `FormaPageHead`. `CultureSwitchPolicy`, which the page publishes beside the marker,
says how a switch persists: in the states alone, by default, or also in request localization's culture
cookie, which the coordinator writes in its `c=…|uic=…` serialization and reads back before it accepts
the switch.

## How the document follows the accepted pair

- Each switch is accepted one at a time, in the order the requests reach the coordinator, and a switch
  completes once its own runtime's state has applied the pair.
- A registering state adopts the pair the page accepted last, and the coordinator keeps that pair when
  the last registration goes, for a runtime that registers later.
- From an enhanced navigation's start the document is held, and a switch requested meanwhile waits; the
  navigation's landing is accepted from the marker of its own response, unless it is the accepted pair
  already, and a navigation that ends without landing ends the hold.
- A streaming update can rewrite the attributes, so the coordinator sets the accepted pair again after
  each; it reads the marker only as the document loads and at a landing.

The package's JavaScript initializer, which Blazor loads on its own, reads the page's marker and policy,
tracks what the server serves, and releases a circuit's registrations as the client closes it. The script
is served from `_content/forma/culture/scripts/`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
