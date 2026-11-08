# Forma.Framework.Culture.Blazor.WebView

What the culture state needs from a Blazor Hybrid application: by default, one culture for the whole
application, across every web view in every native window, or, declared independent, one for each web view.
Under the default, a switch made in any web view, or by the application's own native code, is applied to
the one culture state, to the process's default cultures and to each web view's UI thread, so every event
any web view dispatches after it runs in the new pair; each web view's document takes the pair's language
and direction, and under culture routing its address moves into the pair's route, keeping the page and the
user's work.

## Use it

The package comes with `formaui-net.Blazor.WebView`, and `AddFormaUIWebView()` registers it through
`AddFormaCultureWebView()`:

| Service | Registered as |
| --- | --- |
| `ICultureState` | Forma's own state, a singleton: one for the application. |
| `ICultureSwitch` | The application's switch, a singleton, which a component and the host's native code both switch through. |
| `ICultureStateIntegration` | The application's culture coordinator, which takes any pair applied through it as the application's next switch. |
| `ICultureScopeStart` | Each web view's registration with the application's coordinator, scoped, which the web view's first Forma component starts. |
| `CultureCoordinatorRegistration` | Each web view's registration with its document's culture coordinator, scoped. |

Each is added with `TryAdd`, so a host's own registration made first is the one used; a host that
registers its own culture state registers it as a singleton, and one each web view resolves for itself is
rejected with `InvalidOperationException` as its first component starts.

A web view takes part as its first Forma component initializes, starting in the application's pair, and
leaves as it is disposed, leaving the culture to the application. Windows Forms disposes a web view with its
form; a WPF host disposes its `BlazorWebView` as the native window closes, since WPF disposes no control on
its own.

## A culture per web view

`AddFormaCultureWebView(CultureArrangement.Independent)`, which
`AddFormaUIWebView(options => options.CultureArrangement = CultureArrangement.Independent)` makes, gives each web
view a culture of its own:

| Service | Registered as |
| --- | --- |
| `ICultureState` | Forma's own state, scoped: one for each web view. |
| `ICultureSwitch` | The web view's switch, scoped, which requests each change through the web view's own document. |
| `ICultureScopeStart` | Each web view's registration with its own document's culture coordinator, scoped, which the web view's first Forma component starts from the web view's pair. |
| `CultureCoordinatorRegistration` | Each web view's registration with its document's culture coordinator, scoped. |

A switch in one web view, or a landing at another culture's address, changes that web view's state, its
components, its document and its address alone. Forma applies no web view's pair to the process's or the UI
threads' cultures, which every web view shares, so a component of the host's own reads its web view's
`ICultureState`, and native code switches a web view through that web view's services. A host's own state is
registered scoped; one two web views share is rejected with `InvalidOperationException` as the second starts.

## Persistence

The switch persists the pair in the state alone. A native application keeps the user's choice in its own
settings: it saves the pair as the state notifies it, and switches to the saved pair at startup, before its
first web view opens. A cookie policy is rejected with `InvalidOperationException` as the coordinator, or
under a culture per web view a web view's start, is first resolved, since no request carries a cookie to a
server.

## Culture routing

Pages a Web App shares with the application, culture route pages under `CultureRouteScope`, route as they
do in the Web App, registered with `AddFormaCultureRouting` before or after `AddFormaUIWebView()` under
`CultureSwitchMode.InPlace`; a full-load switch is rejected with `InvalidOperationException` as the
coordinator, or under a culture per web view a web view's start, is first resolved, since a web view reloads
only by restarting its session. By default the application's culture decides each web view's address: a
navigation to an address in another culture switches the application, and every web view then replaces its
address with the same page in the new route; Back, Forward and a web view's first landing take the
application's culture, never switching it.

A web view replaces its address through its navigation, so its location-changing handlers run for the
replacement. A handler guarding the user's work lets it go on, since it keeps the page and the work:

```csharp
using Forma.Framework.Culture.Blazor.WebView;

private ValueTask OnLocationChangingAsync(LocationChangingContext context)
{
    if (HasUnsavedWork && !context.IsCultureFollowing())
    {
        context.PreventNavigation();
    }

    return ValueTask.CompletedTask;
}
```

A handler that prevents the replacement keeps the web view's address, while its components and its document
show the application's pair.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
