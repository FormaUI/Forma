# Forma WebAssembly Implementation Plan

The plan for building what [the WebAssembly design](webassembly-design.md) states, with the switch and the
coordinator the [culture design](culture-design.md) states for every runtime on a page, and the record of
what has landed.

## Status

No step has landed.

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

Each step is built, reviewed and committed on its own.

1. **The client's references.** Every client library references the packages it compiles against in
   place of the shared framework, which stays with the circuit halves and the culture router, as the
   design's [first section](webassembly-design.md#what-a-client-loads) sets out. Gate: the gate, and every
   client library restoring for `browser-wasm`.
2. **The product's halves.** `Forma.Blazor.Server` takes `FormaPageHead` and registers the circuit halves,
   `Forma.Blazor.WebAssembly` registers the WebAssembly halves, and the neutral registrar registers the
   culture state and switch itself, as [registration](webassembly-design.md#registration) sets out; the
   Showcase and the READMEs move to the server half. Gate: the gate, the Showcase unchanged in a browser,
   and a WebAssembly client referencing the halves building.
3. **The browser check.** A Web App host in the tests, with Server, WebAssembly and Auto pages, built and
   published trimmed by the gate and driven in a browser, with runtime async off for every assembly a
   client loads. Gate: the check passes interactivity, a popover through interop and binding in each
   runtime.
4. **Trimming.** The 27 warnings resolved, each client library `IsTrimmable` with the analyzer on.
   Gate: the gate with no trim warning, and the browser check on the trimmed publish.
5. **The switch and the coordinator.** The culture design's switch, as its
   [section](culture-design.md#one-switch-for-the-page) sets out: the switch and its policies, the
   versions, the state applying accepted pairs, the coordinator taking the document culture
   synchronizer's script and ownership, the landing accepted through it, the culture route page
   requesting acceptance, routes naming a pair, the test switch and the local switch. Gate: script tests
   of the coordinator's ordering, versions, document boundary, adoption on registration, landing
   acceptance, a switch waiting out a navigation, the cookie confirmed before acceptance and the last
   registration's release; component tests through the test switch; and the culture plan's document and
   address checks again on the Showcase in a browser.
6. **The culture under WebAssembly.** The culture's WebAssembly half, its registration at startup and the
   process's defaults, and the server registrar turning capture on. Gate: every check under
   [To verify](#to-verify), in the browser check's host.
7. **A host's strings in the browser.** A host's locale files reach the client before its first lookup.
   Gate: a host string switches with the library's in a trimmed, published client.

## Open questions

Each carries the recommendation the step it belongs to starts from.

1. **How the server registrar turns capture on** (step 6). An `IStringLocalizerFactory` over Forma's
   localization is adopted only once it proves the contract: both factory overloads map to Forma's
   resources; missing resources, fallback, enumeration and `ResourceNotFound` behave consistently;
   resource selection reads the UI culture and argument formatting the formatting culture; its lifetime
   captures no one user's state; and it coexists with a host's own factory in both registration orders,
   since `TryAdd` keeps only a factory registered before it. Otherwise the setting, which a registrar
   over services alone can't reach, since the options type is internal. Recommendation: the setting,
   through a registrar on the host's builder that adds it only where the host's configuration has none,
   so an explicit `false` stays the host's.
2. **How a host's locale files reach the client** (step 7): embedded, or fetched behind a loading
   boundary before the first lookup. Recommendation: embedded in the host's client assembly by the
   package's targets, as the packs are, which keeps lookups synchronous with no boundary and works offline,
   at the cost of the files' size, small beside the runtime.
3. **The names** (steps 2 and 6): `Forma.Blazor.Server`, `Forma.Blazor.WebAssembly` and
   `Forma.Framework.Culture.Blazor.WebAssembly`, each named for its render mode as the culture design's
   rule for a platform half asks, and their registrars' names, still to choose.
4. **Who may apply a pair** (step 5). `ApplyAsync` on the state, taking an accepted culture, keeps the
   state a host can replace; the alternative reaches applying only through Forma's switches, at the cost
   of that replacement. Recommendation: `ApplyAsync` on the state, its rule that a host applies nothing
   itself stated in its summary.
5. **Culture addresses where interactive routing runs under WebAssembly** (step 6). The router is a
   server half, so a culture route page rendered under WebAssembly has no route to resolve.
   Recommendation: the routes, their tokens and their matching move into a render-mode-neutral assembly,
   so such a page resolves its route and requests its acceptance as a circuit's does, while the middleware
   stays the server's.
6. **The browser check's host** (step 3). Recommendation: a host of its own in the tests, which the gate
   builds, publishes and drives, rather than the Showcase, which stays a check made by hand.

Hybrid stays the [culture plan](culture-implementation-plan.md)'s, after its ownership is chosen.

## To verify

The probe's checks, carried into the browser check's host as acceptance checks for steps 5 and 6. Its
failure and its two gaps are what those steps fix.

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
