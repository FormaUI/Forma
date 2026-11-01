# Forma WebView Design

Blazor Hybrid hosts Blazor components in a native application, which renders them in its own process to a
web view in each of its native windows, and Forma's components run there as they do in a Web App. This design states what
Forma supports under Blazor Hybrid, what an application loads, how a host registers Forma, how long what it
registers lives, what the application's page carries, how its strings reach it, and how each host is
checked. The one culture an application holds across its web views is the
[culture design](culture-design.md#blazor-hybrid)'s.

## The contract

A component behaves under Blazor Hybrid as it does in a Web App's interactive render modes: it renders,
binds, reaches its web view through its interop, shows its strings and follows the culture. Forma supports
Blazor Hybrid on WebView2, in WPF, Windows Forms and MAUI on Windows; each of those hosts is checked for
its own integration, and every component is checked in a web view. Android, iOS and Mac Catalyst, whose web
views are Android's WebView and WebKit's `WKWebView`, are outside the scope this design chooses for the
implementation and its checks.

## What an application loads

The components run on .NET in the application's process, so an application loads the libraries a
WebAssembly client loads, which reference the packages they compile against rather than the shared
framework, as the [WebAssembly design](webassembly-design.md#what-a-client-loads) sets out. What only Blazor
Hybrid does lives in the product's WebView half, named after Microsoft's
`Microsoft.AspNetCore.Components.WebView`, which every host's `BlazorWebView` builds on, and after the
renderer's own name for itself, `WebView`; so one half serves WPF, Windows Forms and MAUI alike:

| Assembly | Holds |
| --- | --- |
| `Forma.Blazor.WebView` | The application's registrar, `AddFormaUIWebView`, which adds the Framework's WebView halves |
| `Forma.Framework.Culture.Blazor.WebView` | The culture's WebView half, as the culture design's [assemblies](culture-design.md#assemblies-and-registration) set out |

## Registration

The host calls `AddFormaUIWebView` beside its host's own registrar, `AddWpfBlazorWebView`,
`AddWindowsFormsBlazorWebView` or `AddMauiBlazorWebView`, in either order. It registers what `AddFormaUI`
does and the culture's WebView half, and takes the application-wide input routing as the other halves do.
Every registration resolves the same in either order, since no service Forma registers stands in for one the
platform supplies, as the [component foundations](component-foundations-design.md#the-services) set out; a
host's own registration made first still wins.

Culture routing registers as it does in a Web App, `AddFormaCultureRouting`, under
`CultureSwitchMode.InPlace`; `CultureSwitchMode.FullLoad`, a cookie policy and a culture cookie are rejected
at startup with `InvalidOperationException` naming the setting, since nothing reloads a web view's session
on purpose and no request reaches a server to read a cookie.

## Lifetimes

The culture is the application's, and everything that reaches a document is its web view's:

| Lifetime | Holds |
| --- | --- |
| The application | The culture state, the application's culture coordinator and the switch |
| Each web view | Its JS runtime, its navigation, its document's culture coordinator and its registration with the application's, the input router's scopes, every component's subscriptions, and every other scoped service |

A web view takes part once the first Forma component in it initializes, which registers the web view with
the application's coordinator. A web view releases its scope as it is disposed, which releases its
registration and every subscription its components made, and leaves the culture state and the coordinator
to the application; a web view opened later starts from the state as it then stands. Windows Forms disposes a
web view with its form, while WPF disposes no control on its own, so a WPF host disposes a web view as its
native window closes. A web view that went without being disposed is dropped from the coordinator at the
first delivery to it that fails.

## The page

An application's page is its `wwwroot/index.html`, which the web view serves itself: the host links Forma's
stylesheets, its own scoped styles and `_framework/blazor.webview.js`, as a standalone WebAssembly app's
`index.html` does. `FormaPageHead` renders a server's document head and has no place there; each web view's
document coordinator starts from its registration, as on a standalone app's page.

## Strings in the application

A string is looked up synchronously, and an application packaged for MAUI has no folder beside it to read
locale files from, so a host's own locale files are embedded in its application assembly by the package's
targets, as a WebAssembly client's are, and the library's packs are embedded in their assemblies as
everywhere.

## The checks

- **The components.** The browser check's component suite runs under Blazor Hybrid as a runtime of its own,
  `webview`, beside the Web App's, in a WPF check host whose web views the check drives through WebView2: every
  check page, every interaction, and every call to a script and back.
- **Each host's integration.** An example host for WPF, Windows Forms and MAUI on Windows, each set up from
  Forma's packages as the README sets it up, is published and started, and checked for its registration in
  either order, its startup, its published assets, a switch across two web views, and the release of a web
  view whose native window closed.
- **The culture.** Two web views check what the culture design states: a switch in one and an event in the
  other; binding, formatting and a resource looked up in the host's own code; an asynchronous continuation; a
  web view opened after a switch; each document's language and direction; each web view's address following
  a switch; Back and Forward; a web view's handler refusing to follow; and native windows on UI threads of
  their own.
- **Where they run.** WebView2 runs on Windows, so the WebView checks run in the Windows gate and report
  themselves skipped on another platform.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| V1 | Which hosts does Forma support under Blazor Hybrid? | WPF, Windows Forms and MAUI on Windows, on WebView2, each checked for its integration; Android, iOS and Mac Catalyst are outside the chosen scope |
| V2 | What is the half called? | `Forma.Blazor.WebView` with `AddFormaUIWebView`, and the culture's `Forma.Framework.Culture.Blazor.WebView`, after Microsoft's `Microsoft.AspNetCore.Components.WebView` and the renderer's name, as each half takes its render mode's name |
| V3 | What lives for the application, and what for a web view? | The culture state, the application's coordinator and the switch for the application; the JS runtime, navigation, the document's coordinator, the input router's scopes and every subscription for its web view, released as the web view is disposed |
| V4 | When does a web view take part? | As the first Forma component in it initializes |
| V5 | In which order does a host register? | Either; no Forma registration stands in for a platform's service |
| V6 | What does a Blazor Hybrid page carry? | The host's `index.html` with Forma's stylesheets; no `FormaPageHead`, each web view's coordinator starting from its registration |
| V7 | How are the hosts checked? | The component suite in a WPF check host as the `webview` runtime, and an example host per supported host, set up from the packages as the README says, on the Windows gate |
