# Forma WebAssembly Implementation Plan

The plan for building what [the WebAssembly design](webassembly-design.md) states, with the switch and the
coordinator the [culture design](culture-design.md) states for every runtime on a page, and the record of
what has landed.

## Status

Steps 1 to 5 have landed; step 6 is next.

## Where Forma stood

A probe on 2026-10-29, against `7dcc4d15` on SDK 11.0.100-rc.3, built a standalone WebAssembly app and a
Web App with prerendering, InteractiveWebAssembly and InteractiveAuto against every client-side library,
in a scratch copy of the repository patched past each blocker, and drove both in Chrome.

- **Restore.** `NETSDK1082`: the shared framework has no `browser-wasm` runtime pack, and fourteen
  projects reference it, eleven of them client libraries. `JavaScript.Interop` and
  `Culture.Browser.Interop` use only `Microsoft.JSInterop`; `Emojis.Twemoji` and `Culture.Router.Interop`
  use nothing of it; `Browser.Interop`, `Input.Interop` and `Composer.Plugins.Explorer` need the
  components package; `Forma.Blazor`, `Composer` and `Explorer` need the web and authorization packages,
  their `@using Microsoft.AspNetCore.Http` unused; and `DataView` needs the web package and
  `WebUtilities`, a package of its own at RC 3. With those packages in its place every one restored and
  compiled.
- **Build.** `Forma.Blazor` references both circuit halves and `AddFormaUI` registers them;
  `FormaPageHead` uses the server's endpoints (`RZ10012`); and only the circuit integration registers
  `ICultureState`, so without it no component renders.
- **Trimming.** 27 warnings, the same from the library analyzer and from a publish rooting every Forma
  assembly: generic values passed to interop and binding without the target's annotation (13, in the
  JavaScript interop bases, the DOM session, the document culture synchronizer, the share targets'
  registrar and `FormaNumberInput`); reflection-based JSON (6, in the DOM observation and the share
  targets); `OpenComponent(Type)` in `FormaPropertyColumn` (2); `PluginActivator`'s construction (2); and
  `DynamicComponent.Type` in the toolbar and the composer (4). No browser-compatibility warning.
- **Runtime async.** Every async method compiled with the feature threw `InvalidProgramException` under
  WebAssembly, trimmed or not; with it off nothing threw.
- **With those fixed,** the app started in about 1.3 s locally; typing bound, the select's popover opened
  through interop, a culture switch reformatted the date picker, `4,321` committed as 4321, and the
  library's packs loaded once the host's build recorded them. The document's `lang` and `dir` didn't
  follow the switch, and a host's own locale files, copied as the README says, stayed on a disk the
  browser doesn't have.
- **The handoff.** Without `Components:UseCultureFromServer` a prerendered WebAssembly page flipped from
  the cookie's Hebrew to `en-US` at hydration. RC 3 captures only with the setting, or, without it, where
  `IStringLocalizerFactory` is registered, which Forma's localization doesn't do. Its options type,
  `WebAssemblyComponentsServiceOptions`, is `internal sealed`. Across six hosts, Forma and no Forma, the
  setting `true`, absent and `false`, with and without Microsoft's factory, under `he/he` and `de-DE/he`:
  every capturing host started WebAssembly in the pair, formatting `1.234,50` under the Hebrew placeholder;
  every other host started elsewhere, an explicit `false` beating a registered factory.
- **Download.** A normally trimmed publish is 11.24 MB brotli: 4.9 MB the Office add-on's dependencies,
  4.0 MB the runtime with all globalization data, 0.9 MB Forma's assemblies, 0.4 MB AngleSharp, 0.15 MB
  Markdig and 16 KB both locale packs.

## Steps

Each step is a stage, built and reviewed on its own in as many commits as it takes, each coherent and
buildable. From the third step on, the browser check's host gates every step beside the step's own
checks; the Showcase is a smoke check beside it, never the gate.

1. **The client's references.** ✅ Every client library references the packages it compiles against in place
   of the shared framework, which stays with the circuit halves and the culture router, and from step 9 with
   the router's middleware alone, as the design's [first section](webassembly-design.md#what-a-client-loads)
   sets out. Gate: the gate, and every client library restoring for `browser-wasm`.

   Nine of the eleven client libraries reference the packages they compile against, and two need none. The
   three products' imports dropped their authorization and `Microsoft.AspNetCore.Http` usings, which no
   code used, so none of them references an authorization package. Until step 2, `Forma.Blazor` still
   reaches the shared framework through the circuit halves it references, so a library in its graph
   restores for `browser-wasm` but doesn't yet build for it.
2. **The product's halves.** ✅ `Forma.Blazor.Server` takes `FormaPageHead` and `AddFormaUIServer`, on the
   host's builder, which registers what `AddFormaUI` does and the circuit halves; `Forma.Blazor.WebAssembly`
   takes `AddFormaUIWebAssembly`, which registers what `AddFormaUI` does until step 10 gives it the
   culture's WebAssembly half; and `AddFormaUI` registers the culture state itself, as
   [registration](webassembly-design.md#registration) sets out. The culture's circuit registrar becomes
   `AddFormaCultureServer`, the Showcase and the READMEs move to the server half, and each new assembly gets
   the test project named after it. Gate: the gate, a WebAssembly client referencing the halves building,
   and the Showcase as a smoke check.

   Both halves borrow the product's namespace, so `FormaPageHead` stays in `Forma.Blazor.Components.Page`
   and every registrar is reached through `using Forma.Blazor;`; the WebAssembly half's registrar class is
   `WebAssemblyServiceCollectionExtensions`, since the product's own `ServiceCollectionExtensions` holds
   that full name. Each registrar takes its options through a delegate, `UIServerOptions` and
   `UIWebAssemblyOptions`, whose `InputRouting` passes the input policy to `AddFormaUI`. The browser's
   circuit registrar became `AddFormaBrowserServer` with the culture's, and `AddFormaUI` registers the
   culture's and the browser's neutral services. On the Showcase, a page turned interactive and switched to
   Hebrew in place, `he/rtl`, keeping the text typed.
3. **The browser check.** ✅ A Web App host of its own in the tests, with Server, WebAssembly and Auto pages,
   which the gate builds, publishes and drives in a browser, with runtime async off for every assembly a
   client loads. The publish's build configuration is recorded, its trim mode and globalization setting
   among them, with evidence of what it actually trims: each assembly's size before and after the linker, so
   which of Forma's libraries the publish trims is measured rather than inferred from which are marked
   trimmable. It accounts for the download: a minimal Forma client and each optional add-on are built apart,
   under that recorded configuration, so two measurements compare, and each reports its compressed transfer
   by dependency group, the runtime, its globalization data, Forma's own assemblies and each dependency such
   as AngleSharp. Budgets are set from those reproducible builds rather than from the probe's publish of
   every add-on, and an intentional increase changes its budget in a reviewed change of its own. Gate: the
   check passes interactivity, a popover through interop and binding in each runtime, within the budgets.

   The recorded configuration is Release, `TrimMode` `partial`, which trims only the assemblies that declare
   themselves trimmable, and `BlazorWebAssemblyLoadAllGlobalizationData`, with runtime async off across the
   solution. The linker trimmed all 49 framework assemblies and none of Forma's 25. The minimal client
   downloads 3.6 MB compressed: the runtime 2.64 MB, the globalization data 0.35 MB, Forma 0.63 MB; the
   full client 10.7 MB, OfficeIMO 3.95 MB, DocumentFormat 1.07 MB, Forma 1.05 MB, AngleSharp 0.46 MB and
   Markdig 0.15 MB beside the runtime's 3.63 MB. Each budget is its measurement and 2%, rounded up to the
   next 25 KB. The check counts what the runtime fetches, the assemblies, the runtime's own files and the
   ICU data; the full client is published, not driven. Interactivity, binding and the select's list
   through interop passed in the Server, WebAssembly and Auto pages, and Auto ran on WebAssembly after
   its download, every request succeeding and none starting late.
4. **The culture's checks, automated.** ✅ The culture plan's document and address checks, verified by hand
   in Chrome, run in the browser check's host against today's synchronizer: a switch and the document
   following it, the hold through an enhanced navigation and a streaming update, rapid switches, a switch
   landing while offline, a pause and a resume, a closed circuit's release, and culture addresses under
   islands and under interactive routes. They establish today's Server behavior first, as regression checks
   that pass; the new requirements under [To verify](#to-verify) that today's pages can already exercise,
   the probe's defects among them, join them as known failures, so the suite never takes a defect for
   behavior to keep, and each of the rest joins with the step that makes it expressible. Steps 7 to 9
   rewrite exactly that code and are measured against these checks, each kept or changed where the culture
   design now says otherwise. The suite reports three kinds apart, regression checks, known failures and
   acceptance requirements a later step has met, and a known failure that starts to pass moves to the third
   only in the change that meets it, so a gate met under its declared expectations is never read as a defect
   fixed. Gate: every regression check passes on today's code, and every known failure fails as recorded.

   The browser check's host runs three ways: islands under a static router and an interactive router, both
   switching through culture addresses, and islands switching the culture state alone, since under culture
   addresses a closed circuit's release can't be told from the landing that sets the state from the
   address. The regression checks, each passing: a switch in place under both routers, keeping typed text;
   the document held through an enhanced navigation within the culture and through a streaming update;
   rapid switches ending on the last, under both routers; a switch landing while the browser is offline,
   which the document follows once it is back; a pause and a resume, after which the next switch is
   followed; and the served pair shown once the client closes the circuit. The known failures, each still
   failing: an event after a switch made under WebAssembly, the document following that switch,
   WebAssembly starting in the pair the server rendered, and a switch from a Server island reaching a
   WebAssembly island on the same page. The rest of To verify joins with the steps that make it
   expressible. The host routes after the culture middleware, as the Showcase does, since the routing a
   Web App adds at the pipeline's start would choose a page before the culture segment is removed.
5. **The navigation boundary.** ✅ Before anything is built on it, a focused check in the browser check's
   host establishes, under the Server's router and WebAssembly's alike, how a runtime observes: the
   destination its router accepted, with the navigation's identity; a navigation cancelled or failed without
   landing; one superseded by another, and a late report from it; Back and Forward; and a navigation that
   replaces the document. The plan records the hook each rests on and what it guarantees. Where the platform
   can't give the contract the culture design's [landings](culture-design.md#landings) assume, the design
   changes before step 8 builds the coordinator around it. Gate: the check, and its record here.

   The browser check's host renders a recording router in each runtime, and both behave alike. A router
   renders a destination only once its `OnNavigateAsync` completes with its cancellation token unsignalled,
   which is the accepted destination; the runtime numbers its navigations itself, since the router hands out
   no identity. A newer navigation signals the older one's token, and the cancelled destination never
   renders, so no report can come late from it within one runtime. A location-changing handler prevents a
   link's navigation, and no navigation starts. Back and Forward report a location change with
   `IsNavigationIntercepted` true and then navigate through the router as any navigation does. An address no
   route names is accepted and renders the router's not-found content, where no culture route page reports.
   A navigation that replaces the document starts a new runtime with an identity of its own. Two hooks don't
   mark a landing: `LocationChanged` arrives before the router starts and for a superseded address as well,
   and the router hands its `Found` content new route data on every refresh, a parent's render among them.
   The culture design's interactive landing changed accordingly: the culture route page reports the route it
   received, and the identity is the order its runtime reports in. Under WebAssembly in RC 3, `NavigateTo`
   bypasses a location-changing handler that prevents a link's navigation; the design doesn't rest on
   preventing a navigation, since one that lands reports as any landing does.
6. **Trimming.** The 27 warnings resolved, each client library `IsTrimmable` with the analyzer on. The four
   `DynamicComponent.Type` warnings come first: each is reduced to its cause, to find whether the fix
   belongs in Forma's use of the component, in the code Razor generates or in the toolchain, and the remedy
   is chosen from that, within the rule that no warning is suppressed. The publish records the final
   trimming behavior with the same evidence, and its size is checked against step 3's configuration; where
   trimming moves the baseline materially, the plan records why, rather than comparing the two measurements
   as if they were alike. Gate: the gate with no trim warning, and the browser check on that publish, within
   the budgets.

   The 27 warnings are resolved and every client library builds with the trim analyzer on, so a trim
   warning fails it; the libraries aren't yet `IsTrimmable`, which stays open. The four `DynamicComponent`
   warnings had one cause, in neither Forma's use nor the generated code alone: a component opened by
   its type keeps all its members, `DynamicComponent`'s annotated `Type` setter among them, so every use of
   it reports `IL2111`, and a member of Forma's own with an annotated parameter on a component does the
   same. The toolbar, the composer and its surface render the type through a render fragment instead,
   from properties annotated where the type enters. Marking the libraries `IsTrimmable` broke the select's
   list under WebAssembly: the linker kept the anonymous objects Forma passes to its scripts but none of
   their getters, so each reached the script empty, and the analyzer reports nothing, since the
   framework's interop takes its arguments as `object`. The linker keeps a type's members only one level
   deep and a source-generated context can't write the `DotNetObjectReference` and `ElementReference`
   values those payloads carry, so trimming needs the interop's payloads reshaped first, which changes the
   behaviors' script contract. The publish trims the 49 framework assemblies and keeps Forma's whole, as
   step 3 measured: the minimal client downloads Forma's 0.64 MB beside the runtime's 2.64 MB, and the full
   client, which now renders the composer, a grid and cards and registers the explorer plugin, Forma's
   1.07 MB beside the runtime's 3.62 MB, all within the budgets. The check passes in every runtime.
7. **The switch's contract and its application.** ✅ `ICultureState` reads the pair, its revision and its
   version; `ICultureStateIntegration`, resolved from the state's own instance, applies an accepted pair,
   and a replacement state without it is rejected as the state is first resolved; `ICultureSwitch` requests
   a change; and the versions, the test switch and the local switch, as the culture design's
   [switch](culture-design.md#one-switch-for-the-page) sets out. The public contract changes here, and
   `AddFormaUI` registers a transitional switch that keeps today's observable behavior while every caller
   moves to the switch: under the Server it applies through today's synchronizer, and under WebAssembly it
   applies locally, as a WebAssembly switch does today. Gate: the gate, component tests through the test
   switch, a replacement state without the integration rejected as it is first resolved, and step 4's
   regression checks.

   The transitional switch is the local switch, which `AddFormaUI` registers where the host registered no
   switch: it accepts at once in a sequence of its own and applies to the runtime's state, which today's
   synchronizer follows into the document, so the Server and WebAssembly keep the behavior they had. The
   culture router's landings request through the switch, so the address policy keeps working through it
   until step 9. The registrar wraps whichever state registration resolution picks, so a replacement
   without the integration fails as a scope first creates it, and an instance registration fails as it is
   registered. The test switch lives in the shared test support, and the bUnit context registers it. Every
   test and step 4's regression checks pass.
8. **Coordination.** ✅ The coordinator takes the document culture synchronizer's script and ownership, and the
   switch that requests through it replaces step 7's transitional switch for circuits: the culture marker
   `FormaPageHead` publishes; the switch's policy, chosen where the host registers the culture and published
   apart from the marker; each circuit's registration, its lifecycle and its three states; versions and the
   document's sequence; adoption on registration; the accepted pair outliving its registrations; the cookie
   read back before acceptance; and the distribution of every accepted change. A WebAssembly runtime keeps
   the transitional switch until step 10 registers it with the coordinator. The landing report and its reply
   carry over, so culture addresses keep working until step 9, with one authority: the pair the reply
   resolves enters the coordinator's acceptance as a landing record, and nothing beside the coordinator
   updates a state or the document from it; step 9 replaces where the landing's pair comes from, never who
   accepts it. Gate: script tests of the coordinator's ordering, versions, document boundary, adoption, a
   switch waiting on a registration under way and failing with it, the cookie read back, a landing reply
   accepted only through the coordinator, the last registration's release, a head update alone changing
   nothing, and a missing or invalid marker failing initialization; and step 4's checks, each as the design
   now states it.

   The coordinator replaces the synchronizer, its script and its C# both, and the circuit's switch
   requests through it under the policy of the state alone or of the cookie, which `FormaPageHead`
   publishes beside the marker. Every accepted pair reaches a state through its registration's receiver,
   the requester's and a registering state's included, and the coordinator answers a request once the
   requester's state has applied it. A circuit registers before it renders, so its components start in
   the page's pair. One departure: the server-rendered landing takes its pair from its own response's
   marker already, rather than through the route handler's reply. The reply needed a live registration,
   and the marker path is the one step 9 was to build. So `ICultureRouteHandler` and the reconciliation of
   a circuit with its address retire here, adoption on registration replacing the second. Step 4's offline
   check changed: under Chrome's offline emulation the circuit keeps its connection, so the switch is
   accepted once the browser is back, and the check asserts that the document and the state settle on
   the pair the switch's outcome gives. The closed circuit's check still passes, now through the landing
   that leaves the page. The script tests, the C# tests and the browser check pass.
9. **Routing in coordination.** The routes, their tokens, their mapping, the address policy's switch, the
   page base and the route scope move into the new `Forma.Framework.Culture.Router`, with its test project;
   routes name a pair; the mapping is published to the page; destinations are built from it, and a pair no
   route names is rejected before anything changes; and the interactive landing path, on step 5's hook, is
   wired into coordination beside the server-rendered one step 8 wired; and the document is
   held through every enhanced navigation, as the culture design's
   [document](culture-design.md#the-document-follows-the-state) section sets out. Under WebAssembly the
   interactive landing runs end to end from step 10, once the runtime registers with the coordinator. Gate:
   script tests of both landing paths, an obsolete landing, a switch waiting out a navigation and an
   unrouted pair; tests that the browser's mapping and the server's agree on every route, pairs included;
   and step 4's checks.
10. **The culture under WebAssembly.** The culture's WebAssembly half,
    `Forma.Framework.Culture.Blazor.WebAssembly` with its test project and `AddFormaCultureWebAssembly`,
    which `AddFormaUIWebAssembly` now calls; its registration at startup, which replaces step 7's
    transitional switch under coordinated pages as the local switch does under an independent client
    culture, and the runtime's current and the process's default cultures on every application, the local
    switch's included; `AddFormaUIServer` supplying capture on where the configuration names none; the
    culture arrangement on both product registrars; and the startup check that rejects capture off under
    coordinated pages. An `IStringLocalizerFactory` over Forma's localization stays optional, adopted only
    once it proves its contract: both factory overloads map to Forma's resources; missing resources,
    fallback, enumeration and `ResourceNotFound` behave consistently; resource selection reads the UI
    culture and argument formatting the formatting culture; its lifetime captures no one user's state; and
    it coexists with a host's own factory in both registration orders, since `TryAdd` keeps only a factory
    registered before it. Gate: every check under [To verify](#to-verify), in the browser check's host.
11. **A host's strings in the browser.** A host's locale files embedded in its client assembly by the
    package's targets, under resource names and discovery metadata the browser's loader finds. Gate: a
    consumer of the packed packages, published trimmed, whose host string switches with the library's.
12. **The host's setup.** The READMEs set out both arrangements, each complete on both sides: the client's
    and the server's registration, `FormaPageHead` and the marker it publishes, the capture setting, and the
    host's locale files. Each arrangement is also an example host in the tests, built and exercised by the
    gate from the README's own setup, so the README can't drift from what is supported. Gate: both example
    hosts start and switch as their README says.

Hybrid stays the [culture plan](culture-implementation-plan.md)'s, after its ownership is chosen.

## To verify

The probe's checks, carried into the browser check's host as acceptance checks for steps 7 to 10, where
step 4 records the ones not yet met as known failures. The probe's failure and its two gaps are what
those steps fix.

- An event after a switch made under WebAssembly runs in the new pair. The probe failed it: the state held
  `en-US/en-US` and the event's ambient culture `de-DE/he`.
- The document follows a switch made under WebAssembly. The probe's document stayed `he/rtl`.
- With the real switcher, under the cookie policy and the address policy, the server's next response and
  the next interactive render agree on both cultures. The probe's host persisted nothing, and its server
  prerendered the next page in `de-DE/he` under a client in `en-US`.
- Hydration across the six hosts and both cookies, as the probe checked it.
- Auto's reload runs on WebAssembly in the pair once the resources downloaded, each request tracked as
  itself, successes and failures apart and late starts recorded. The probe downloaded 77, failed none and
  saw none start late.
- With Server and WebAssembly islands on one page, a switch from either reaches both and the document.
- An older switch delivered late is ignored, a reconnecting circuit adopts the accepted pair, and a new
  document starts a new sequence.
- Work Forma schedules after a switch under WebAssembly runs in the new pair.
- Releasing every registration leaves the accepted pair, and a runtime registering after the gap adopts
  it.
- A switch requested during a navigation waits: a cancelled or failed navigation releases it without
  accepting the destination's pair, an obsolete landing record accepts nothing, the waiting switch is
  accepted against the address that landed, and a replaced document fails what waited in it.
- Under the cookie policy, the next server request resolves the accepted pair.
- The marker names `de-DE/he` apart, and the coordinator starts from it whichever runtime registers first.
- A server-rendered landing is accepted from its own response's marker and an interactive one through the
  published mapping, each with no registration live too; a head update alone, or an obsolete landing,
  changes nothing.
- A coordinated prerendered page without `FormaPageHead`, or with an invalid marker, fails the
  coordinator's initialization, naming `FormaPageHead` and the arrangement.
- The published cookie policy writes a cookie the server's next request reads back as the accepted pair,
  and adopting a pair never changes the policy.
- A switch made during the first interactive render waits for its runtime's registration, and fails when
  that registration fails or disconnects.
- Under the address policy, a switch to a pair no route names throws, naming the pair, and changes
  nothing.
- Under the independent arrangement, a WebAssembly client's local switch sets the process's defaults, so
  an event after it runs in the new pair.
- An explicit capture `false` under coordinated pages fails at startup in either registration order,
  naming the setting, the arrangement and the ways out; the same `false` with the client's culture
  declared independent starts, its client switching alone without touching the document, the
  coordinator, the address or the cookie; and a server rendering no WebAssembly checks nothing.
