# Forma.Framework.Culture.Browser.Interop

The page's culture coordinator: one per document, it orders every culture switch on the page, gives each
the next number in the document's sequence, and delivers the accepted pair to the culture state of every
runtime registered with it, each Interactive Server circuit and WebAssembly runtime on the page, so they
converge on one pair. It
keeps the page's `<html lang dir>` on the accepted pair, so a page switched to Hebrew in place is laid out
right to left and read as Hebrew by browsers and screen readers.

## Use it

The package comes with `formaui-net.Blazor`, whose `AddFormaUI()` registers it through
`AddFormaCultureCoordination()`, and `formaui-net.Framework.Culture.Blazor.Server`'s circuit handler
registers each circuit with it, as `formaui-net.Framework.Culture.Blazor.WebAssembly`'s startup registers
each WebAssembly runtime:

| Service | Registered as |
| --- | --- |
| `ICultureState` | Forma's own state, scoped, through `AddFormaCultureServices()`. |
| `ICultureStateIntegration` | The same instance as the scope's `ICultureState`, through `AddFormaCultureServices()`. |
| `CultureCoordinatorRegistration` | Scoped, one per user scope. |

Each is added with `TryAdd`, so a host's own registration made first is the one used.
`CultureCoordinatorRegistration.RegisterAsync()` registers the scope's state with the coordinator, which
adopts the pair the page accepted, and `CoordinatedCultureSwitch` requests each switch through it. The
coordinator delivers each pair by its names, and the registration hands the state the application's
`ICultureProvider`'s culture for each name, or the platform's where none is registered; a delivery the
provider fails is logged as an error under `CultureCoordinatorRegistration`.

The coordinator starts from the culture marker `FormaPageHead` publishes in the page's head, the cultures
the server rendered the page in; a page without it can't be coordinated, and every registration fails
with an error naming `FormaPageHead`. `CultureSwitchPolicy`, which the page publishes beside the marker,
says how a switch persists: in the states alone, by default, or also in request localization's culture
cookie, which the coordinator writes in its `c=…|uic=…` serialization and reads back before it accepts
the switch. `AddFormaCultureSwitchPolicy(policy)` registers it, refusing a cookie a browser can't keep, and
`CulturePublication` gives the marker's and the policy's attributes as the page publishes them. A
standalone app's page has no server, so its runtime registers with a `CultureCoordinatorStart` carrying
both, which the coordinator starts from on a page whose load initialized nothing. A web view's page starts
the same way, under a policy whose attributes `GetPolicyAttributes` gives:

- A coordinated web view's document, given `follows: true, isWebView: true`, follows the pair an
  application's coordinator accepts, which `CultureCoordinatorRegistration.FollowAsync` hands it, rather than
  accepting one of its own.
- The document of a web view holding a culture of its own, given `isWebView: true` alone, accepts its own
  switches.

A switch completes once its own runtime's state has applied the pair it accepted. An enhanced
navigation's landing changes the page's pair to the one its response was rendered in, and a switch
requested while a navigation is in flight waits for it to land. The package's JavaScript initializer,
which Blazor loads on its own, starts the coordinator and releases a circuit's registrations as the client
closes the circuit.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
