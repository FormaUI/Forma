# Forma WebView Implementation Plan

The steps that carry out the [WebView design](webview-design.md), the culture design's
[Blazor Hybrid](culture-design.md#blazor-hybrid) section, and the render modes the
[component foundations](component-foundations-design.md#render-modes) require of every component. Each
step is built and reviewed on its own in as many commits as it takes, each coherent and buildable, and
gated as it says.

## Steps

1. **The design.** ✅ The WebView design and this plan; the culture design's Blazor Hybrid section, with one
   culture for the application, the ambient culture, what Forma updates and what the host refreshes,
   persistence, and addresses across web views; the component foundations' registrations, which no longer
   stand in for a platform's service, and their render modes; and every render mode as a gate in the
   working rules. Gate: the documents agree with one another and with the code they describe.
2. **The probe.** ✅ `tests/Forma.Blazor.Browser.WebView`, a WPF check host whose native windows each render Forma's
   components to a `BlazorWebView`, which the browser check starts per check and drives through WebView2's
   DevTools. What it established, each a check of its own:
   - **The renderer** is `WebView`, interactive, and a WPF web view's components run under WPF's
     `DispatcherSynchronizationContext`. A component's script and Forma's stylesheets load from the web view's
     static assets.
   - **A web view's registration.** The coordinator's script initializer is a Web App's `beforeWebStart`, which
     a web view never runs, so a web view's document coordinator starts from the registration its first
     component makes as it initializes, interop being available there, as a standalone app's page starts;
     the document takes the registration's language and direction. That start path refuses the address
     policy, which step 4 opens for a web view's in-place routing.
   - **The ambient culture.** The process's default cultures alone never reach a UI thread the platform
     assigned a culture to, as MAUI assigns the device's; cultures set in the dispatcher's captured flow are
     gone once that flow ends; cultures posted to each web view's UI thread through its synchronization
     context, with the flow suppressed, reach every later event: its formatting, its binding and the
     platform's resources. Work begun before such a switch finishes in the culture it captured, and a web
     view opened after it starts in it. Step 4 applies an accepted pair this way.
   - **Threads.** WPF's web view dispatches its components on the application's dispatcher whatever thread
     its native window runs on, so WPF's web views share the application's UI thread, and one in a native
     window of another thread never renders; Windows Forms dispatches each on its own control's thread, which step 8 checks.
     The culture design now says so.
   - **A web view's address replaced.** Replacing it through the web view's navigation, with
     `ReplaceHistoryEntry` and the mark as `HistoryEntryState`, runs the web view's location-changing handlers,
     which read the mark and can prevent it; otherwise the router keeps the page's instance and the user's
     work, and the history gains no entry. A handler receives a target as it was asked for, relative where
     .NET asked relatively.
   - **A traversal and a first landing.** Blazor's location-changed event reports a traversal as
     intercepted, though its location-changing event doesn't, so a traversal is told apart by the document's
     own history events, `popstate` and the Navigation API's `traverse`; a web view's first landing raises no
     location change at all.
   - **A closed native window.** WPF's web view releases its scope only when its host disposes it, never as its
     native window closes, since WPF disposes no control on its own; Windows Forms disposes it with its form. The
     WebView design now says a WPF host disposes the web view, and that a web view never disposed is dropped
     at its first failed delivery.
   - **Known failures,** which step 4 meets: a switch through the application's switch reaching every
     web view's state, and every web view's document following it.
3. **Registration in either order.** ✅ The bases resolve the JS runtime and the locale factory where they
   are registered and fall back to their null objects, as `DomBridge`, `InputRouter`, the culture's
   registration and the sprite loader resolve the runtime, through `GetJSRuntime`; every registrar whose
   services log registers the platform's logging through `AddLogging`, `AddJsonLocalization` among them,
   whose null logger factory had kept a host's logging out where it ran first; the emoji and icon
   components fall back to the reader's font and the bundled glyphs, so `AddFormaUI` registers no artwork;
   and the READMEs drop every order they required. Each is checked in both orders against the registrar
   that supplies the real service, and with a host's own registered first. Departure: the rule covers what
   another registrar supplies with `TryAdd`, the platform's or a package's; a contract only a host
   replaces, such as the sanitizer or the time provider, keeps its default, since a host's own
   registration replaces it in either order with `Add` and, made first, with `TryAdd`.
4. **The WebView halves.** ✅ `Forma.Framework.Culture.Blazor.WebView` and `Forma.Blazor.WebView`, each with
   the test project named after it: the application's state, coordinator and switch; each web view's
   registration, started by its first component, and its release; the ambient cultures applied to the
   process and each web view's UI thread; the document coordinator following the application's
   coordinator, replacing a web view's address under Forma's mark, and adopting on a traversal and a first
   landing; the mark's reading for a navigation handler, `IsCultureFollowing`; and the startup checks
   rejecting a cookie policy and a full-load switch. Gate: the halves' tests, the coordinator's script tests
   in its following mode, and step 2's checks, each as the design states it. The check host registers
   through `AddFormaUIWebView` with culture routes, before the platform's registrar and after it, and the
   browser check splits what the platform guarantees from what Forma does across web views: a switch from a
   component and from native code reaching every web view's state, events, binding and document; every
   address following under the mark, a guard on the user's work letting it pass and holding a navigation
   away; a refusing handler keeping its address; a navigation to another culture's address switching the
   application; a traversal and a web view opened at a stale address taking the application's culture; and
   switching on past a closed native window. Step 2's two known failures are plain checks. Departures:
   - Culture routing registered the culture coordination itself, so a render mode's half registered after
     it found the state scoped, under WebAssembly as under a web view; it now registers only what it owns,
     so it goes before or after any half.
   - The state's notifier takes subscriptions from any thread and notifies every subscriber where one
     fails, faulting with all of them, since the application's state is subscribed from every web view's
     thread and one web view's failure mustn't leave the others showing the old pair.
   - A culture route page starts its web view's part before it reports its landing, since a page lands
     before any Forma component in it initializes; the design now says a web view takes part at whichever
     comes first.
   - A web view whose follow goes a minute unanswered is dropped, the time Blazor Server gives a call into
     script by default: the check host showed a WPF web view whose native window closed without being
     disposed answering nothing, so its first delivery never fails. The design now says so.
   - The integration a web view resolves is the application's coordinator, so a pair applied through it
     enters the application's sequence; a pair a document accepts names its cultures alone, and the
     coordinator takes it only where the names differ from the state's, keeping formats a host pinned. The
     design now says so.
   - The startup checks run as the application first resolves its coordinator, at its first web view or its
     first switch, since a native application starts no host to check from, and `AddFormaCultureWebView`
     takes no policy, the state alone being the one a web view accepts.
5. **A host's strings in the application.** ✅ The package's targets embed a host's locale files in a Blazor
   Hybrid application's assembly. Gate: an application built from the packed packages showing a host
   string in the switched culture. The build knows a Blazor Hybrid application as an executable that
   references its platform's web view package, WPF's, Windows Forms' or MAUI's, since none of them sets a
   property a build can read. Departure: the gate is met in the check host, whose publish carries no folder
   of locale files and whose web views show its own string in the switched culture, through the same
   targets file a package imports; an application built from the packed packages shows it with step 8's
   example hosts, which are built from them already.
6. **The components in a web view.** ✅ The check pages move into a Razor class library the components host's
   client and the WebView check host share, and the WPF check host runs every check page as the `webview`
   runtime, beside the Web App's four. Gate: every component check passes in the web view. A check in the web
   view opens a native window of its own at its page, through the application's first web view, at the size
   a check's page has in a browser, and closes it as it ends, one check at a time, since every WPF web view
   dispatches on the application's UI thread; the web views share one browser context, so a check's end
   takes back what it routed and granted. Departures and findings:
   - A web view hands a link opening another site in a new window to the system's browser, and WPF's never
     raises `UrlLoading` for one, so the check host's page stands in for the system's browser, recording the
     address once the page handled the click, and the share menu's check reads it there.
   - The video player's embedded page and its service's script came from the components host's server, which
     a web view's page has none of; they're files of the check pages' library, which every host serves alike.
   - Disposing the module wrapper, the input registration and the culture registration waited for a call
     into script they had started, which a web view whose document is gone never answers; each now leaves
     the call to finish and releases what it produced then, and the scripting standard says so.
   - The WPF WebView2 control's own disposal throws, a COM `E_NOINTERFACE` on its controller, when a web view
     is disposed moments after a keyboard shortcut, on the UI thread, after every component and scoped
     service disposed cleanly. The check host reports it and closes the native window regardless; step 8's
     WPF example host and step 10's README close it the same way.
   - The check host's page linked no scoped stylesheet of its own, which a host's page does; it does now.
7. **Static rendering.** Every check page renders statically, as the `static` runtime, under a check that
   it shows its state, raises no error and offers no enabled control whose action needs interactivity;
   each component it finds failing is fixed. Gate: the static check over every check page.
8. **Each host's integration.** Example hosts for WPF, Windows Forms and MAUI on Windows, set up from the
   packed packages as the README sets them up, published and started, each checked for its registration in
   either order, its startup, its published assets, a host string from its embedded locale files in the
   switched culture, a switch across two web views and the web view of a closed native window's
   release. Gate: the example hosts' checks.
9. **Render modes in the gate.** An inventory check that every component a host places has a check page,
   and that every check page runs in every interactive runtime and statically; the Windows run of CI
   installs what the WebView checks need, the WebView2 runtime where its image lacks one, and the check
   reports what the check host wrote where a web view never opens ✅; on another platform they report
   themselves skipped. Gate: CI on both platforms with the WebView checks counted on Windows.
10. **The host's setup.** The README sets out each host, its registration, its page, its persistence and its
    locale files, each block held to its example host; the repository structure, the vocabulary, the
    features status and the feature history describe the WebView half. Gate: the example hosts match their
    README blocks.

The scope chosen leaves Android, iOS and Mac Catalyst, and a culture per web view, outside these steps, as
the features status records.
