# Forma WebView Design

Blazor Hybrid hosts Blazor components in a native application, which renders them in its own process to a
web view in each of its native windows, and Forma's components run there as they do in a Web App. This design states what
Forma supports under Blazor Hybrid, what an application loads, how a host registers Forma, how long what it
registers lives, what the application's page carries, how its strings reach it, and how each host is
checked. The culture an application holds across its web views, one by default or one for each web view, is
the [culture design](culture-design.md#blazor-hybrid)'s.

## The contract

A component behaves under Blazor Hybrid as it does in a Web App's interactive render modes: it renders,
binds, reaches its web view through its interop, shows its strings and follows the culture. Forma supports
Blazor Hybrid on WebView2, in WPF, Windows Forms and MAUI on Windows, and in MAUI on Android, iOS and Mac
Catalyst, whose web views are Android's WebView and WebKit's `WKWebView`. Those two lack what WebView2 does by
itself, saving a file, printing and sharing, which the application does natively in their place, as
[native operations](#native-operations) set out.

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

An application whose web views each hold a culture of their own declares the independent arrangement,
`AddFormaUIWebView(options => options.CultureArrangement = CultureArrangement.Independent)`, which registers the
culture's state and switch for each web view's scope in place of the application's, as the culture design's
[culture per web view](culture-design.md#a-culture-per-web-view) sets out.

Culture routing registers as it does in a Web App, `AddFormaCultureRouting`, before or after
`AddFormaUIWebView`, since it registers only its own services, under `CultureSwitchMode.InPlace`;
`CultureSwitchMode.FullLoad` and a cookie policy are rejected with `InvalidOperationException` naming the
setting as the application first resolves its culture coordinator, at its first web view or its first
switch, or under a culture per web view as a web view first resolves its start, since nothing reloads a web
view's session on purpose and no request reaches a server to read a cookie, and the culture-route cookie is
never written.

## Lifetimes

By default the culture is the application's, and everything that reaches a document is its web view's:

| Lifetime | Holds |
| --- | --- |
| The application | The culture state, the application's culture coordinator and the switch |
| Each web view | Its JS runtime, its navigation, its document's culture coordinator and its registration with the application's, the input router's scopes, every component's subscriptions, and every other scoped service |

Under a culture per web view, each web view holds its culture state and its switch as well, and its
document's coordinator orders its switches in place of the application's.

A web view takes part once the first Forma component in it initializes, or a culture route page in it
lands, whichever comes first, which registers the web view with the application's coordinator. A web view releases its scope as it is disposed, which releases its
registration and every subscription its components made, and leaves the culture state and the coordinator
to the application; a web view opened later starts from the state as it then stands. Windows Forms disposes a
web view with its form, while WPF disposes no control on its own, so a WPF host disposes a web view as its
native window closes. A web view that went without being disposed is dropped from the coordinator at the
first delivery to it that fails or goes a minute unanswered, the time Blazor Server gives a call into script
by default, since a WPF web view whose native window closed answers nothing.

## The page

An application's page is its `wwwroot/index.html`, which the web view serves itself: the host links Forma's
stylesheets, its own scoped styles and `_framework/blazor.webview.js`, as a standalone WebAssembly app's
`index.html` does. Under MAUI the script loads with `autostart="false"`, since MAUI starts Blazor itself once
the web view's channel to the application is in place, and on Android a page that starts it first loses that
channel and never renders. `FormaPageHead` renders a server's document head and has no place there; each web
view's document coordinator starts from its registration, as on a standalone app's page.

## Native operations

A component saves a file, prints a document and shares an address through the browser's own paths: a link
naming a file to download, the browser's printing and its share surface. Android's WebView and `WKWebView`
take none of them without native help, so the host does each natively where its web view can't:

- **One contract, `INativeOperations`,** in the browser interop assembly beside the operations it stands in
  for, saves a file and shares an address, answering whether the host took each; a component that saves,
  prints or shares takes it where the host registers one and goes to the browser's path where none is
  registered or the host declines.
- **Printing hands the reader the document as a file.** No mobile web view prints a document from script, so
  where the host takes files, the document to print, or the page as it stands, is written as a document of
  its own carrying the page's styles and handed to the host as an HTML file, from which the reader prints it
  through the platform's own surface.
- **MAUI's implementation is an add-on,** `Forma.AddOns.Operations.Maui`, an application's opt-in, which hands
  each file and address to the platform's share sheet through MAUI's own share and file system services on
  Android, iOS and Mac Catalyst, and declines on Windows, whose web view does all three itself. It builds
  against MAUI's platform-neutral services, which each platform's build of the application replaces with its
  own, so the add-on carries no code of a platform's own.

## Strings in the application

A string is looked up synchronously, and an application packaged for MAUI has no folder beside it to read
locale files from, so a host's own locale files are embedded in its application assembly by the package's
targets, as a WebAssembly client's are, and the library's packs are embedded in their assemblies as
everywhere. The application's assembly is the one its runtime started it from, or, where the runtime names
none, as .NET for Android and for iOS don't, each loaded assembly whose build recorded a pack or embedded the
host's files; and Android, whose build compiles an application's code as a library, embeds them in the project
it marks as an application.

## The checks

- **The components.** The browser check's component suite runs under Blazor Hybrid as a runtime of its own,
  `webview`, beside the Web App's, in a WPF check host whose web views the check drives through WebView2, and as
  `android` in a MAUI check host on an Android emulator whose screen holds a check's page whole, whose one web
  view the check drives through its DevTools socket, loading each check page afresh: every check page, every
  interaction, and every call to a script and back.
- **Each host's integration.** An example host for WPF, Windows Forms and MAUI on Windows, each set up from
  Forma's packages as the README sets it up, is published and started, and checked for its registration in
  either order, its startup, its published assets, a switch across two web views and the native windows'
  titles the host refreshes, the release of a web view whose native window closed, and the culture the
  application keeps from one run to the next.
- **Android, iOS and Mac Catalyst.** The MAUI example host builds from Forma's packages for each, in Release,
  trimmed as the platform's own build trims it, on the platform's own machine, Android's in CI, and the native operations are
  checked against MAUI's share and file system contracts. On Android the build is installed on an emulator and
  driven through its web view's DevTools socket, which adb forwards: its strings switching with the culture, the
  host's own and a pack's, its stylesheets loading from the application's package, and its share menu's Share and
  Print reaching the platform's share sheet.
- **The culture.** Two web views check what the culture design states: a switch in one and an event in the
  other; binding, formatting and a resource looked up in the host's own code; an asynchronous continuation; a
  web view opened after a switch; each document's language and direction; each web view's address following
  a switch; Back and Forward; a web view's handler refusing to follow; and native windows on UI threads of
  their own.
- **Where they run.** WebView2 runs on Windows, so the WebView checks run in the Windows gate and report
  themselves skipped on another platform. The Android checks, the example host's and the component suite's, run
  on an emulator in the full gate, nightly on `dev`, on every push to `candidate` and on demand, and locally
  where a device is attached, the component suite's after the rest of the browser check and in a run of its own;
  they report themselves skipped where a device isn't attached.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| V1 | Which hosts does Forma support under Blazor Hybrid? | WPF, Windows Forms and MAUI on Windows, on WebView2, and MAUI on Android, iOS and Mac Catalyst, on Android's WebView and `WKWebView`, each checked for its integration |
| V2 | What is the half called? | `Forma.Blazor.WebView` with `AddFormaUIWebView`, and the culture's `Forma.Framework.Culture.Blazor.WebView`, after Microsoft's `Microsoft.AspNetCore.Components.WebView` and the renderer's name, as each half takes its render mode's name |
| V3 | What lives for the application, and what for a web view? | The culture state, the application's coordinator and the switch for the application by default, or the state and the switch for each web view under a culture per web view; the JS runtime, navigation, the document's coordinator, the input router's scopes and every subscription for its web view, released as the web view is disposed |
| V4 | When does a web view take part? | As the first Forma component in it initializes, or a culture route page in it lands, whichever comes first |
| V5 | In which order does a host register? | Either; no Forma registration stands in for a platform's service |
| V6 | What does a Blazor Hybrid page carry? | The host's `index.html` with Forma's stylesheets; no `FormaPageHead`, each web view's coordinator starting from its registration |
| V7 | How are the hosts checked? | The component suite in a WPF check host as the `webview` runtime and in a MAUI check host on Android as the `android` runtime, and an example host per supported host, set up from the packages as the README says, on the Windows gate and, for Android, on an emulator |
