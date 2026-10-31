# Forma WebView Implementation Plan

The steps that carry out the [WebView design](webview-design.md), the culture design's
[Blazor Hybrid](culture-design.md#blazor-hybrid) section, and the render modes the
[component foundations](component-foundations-design.md#render-modes) require of every component. Each
step is built and reviewed on its own in as many commits as it takes, each coherent and buildable, and
gated as it says.

## Steps

1. **The design.** ✅ The WebView design and this plan; the culture design's Blazor Hybrid section, with one
   culture for the application, the ambient culture, what Forma updates and what the host refreshes,
   persistence, and addresses across windows; the component foundations' registrations, which no longer
   stand in for a platform's service, and their render modes; and every render mode as a gate in the
   working rules. Gate: the documents agree with one another and with the code they describe.
2. **The probe.** A minimal WPF check host with two windows, each a `BlazorWebView` rendering Forma's
   components, driven by the browser check through WebView2. It establishes, before anything is built on
   them: the startup, the web view's registration as its first component initializes, its interop and its
   static assets; the renderer's name; the ambient culture after a switch, in the other window's events,
   in binding, formatting and a resource the host looks up, in an asynchronous continuation, in a window
   opened after the switch, and in a window on a UI thread of its own; each document's language and
   direction; a window's address replaced under a mark and a navigation handler refusing it; how a
   traversal and a window's first landing are told apart; and a closed window's release. This plan records
   the hook each rests on and what the platform guarantees; where the platform contradicts the design, the
   design changes before step 4 builds on it. Gate: the probe's checks, the requirements not yet met
   recorded as known failures, and their record here.
3. **Registration in either order.** The bases resolve the JS runtime, the logger factory and the locale
   factory where they are registered and fall back to their null objects; every registrar whose services
   log registers the platform's logging; every contract an add-on or a host supplies is resolved where it
   is registered, its consumer falling back; and the READMEs drop every order they required. Gate: tests
   registering each platform's, package's and host's registration before and after Forma's, with a host's
   own registered first still winning, and the gate.
4. **The WebView halves.** `Forma.Framework.Culture.Blazor.WebView` and `Forma.Blazor.WebView`, each with
   the test project named after it: the application's state, coordinator and switch; each web view's
   registration, started by its first component, and its release; the ambient cultures applied to the
   process and each window's UI thread; the document coordinator following the application's
   coordinator, replacing a window's address under Forma's mark, and adopting on a traversal and a first
   landing; the mark's reading for a navigation handler; and the startup checks rejecting a cookie policy
   and a full-load switch. Gate: the halves' tests, the coordinator's script tests in its following mode,
   and step 2's checks, each as the design states it.
5. **A host's strings in the application.** The package's targets embed a host's locale files in a Blazor
   Hybrid application's assembly. Gate: an application built from the packed packages showing a host
   string in the switched culture.
6. **The components in a web view.** The check pages move into a Razor class library the components host's
   client and the WebView check host share, and the WPF check host runs every check page as the `webview`
   runtime, beside the Web App's four. Gate: every component check passes in the web view.
7. **Static rendering.** Every check page renders statically, as the `static` runtime, under a check that
   it shows its state, raises no error and offers no enabled control whose action needs interactivity;
   each component it finds failing is fixed. Gate: the static check over every check page.
8. **Each host's integration.** Example hosts for WPF, Windows Forms and MAUI on Windows, set up from the
   packed packages as the README sets them up, published and started, each checked for its registration in
   either order, its startup, its published assets, a switch across two windows and a closed window's
   release. Gate: the example hosts' checks.
9. **Render modes in the gate.** An inventory check that every component a host places has a check page,
   and that every check page runs in every interactive runtime and statically; the Windows run of CI
   installs what the WebView checks need; on another platform they report themselves skipped. Gate: CI on
   both platforms with the WebView checks counted on Windows.
10. **The host's setup.** The README sets out each host, its registration, its page, its persistence and its
    locale files, each block held to its example host; the repository structure, the vocabulary, the
    features status and the feature history describe the WebView half. Gate: the example hosts match their
    README blocks.

The scope chosen leaves Android, iOS and Mac Catalyst, and a culture per window, outside these steps, as
the features status records.
