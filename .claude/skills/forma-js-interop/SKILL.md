---
name: forma-js-interop
description: Forma's JavaScript and interop standard — when JS is allowed at all, the behaviours/operations/components/services split that decides where a script lives and how it is written, the four module contracts, how C# reaches a module through JsInterop and JsModule, and the external-library carve-out for FormaComposer. Use when adding or changing anything under wwwroot/scripts/ or in an interop assembly, naming a script or wrapper, or deciding whether a capability needs JS.
---

# JavaScript

JS-free is the default, not a prohibition.

- Do prefer HTML, CSS, or Blazor when they provide the behaviour cleanly.
- Do use interop when the platform exposes the needed behaviour only to script or when declarative techniques leave a real usability problem.
- Do keep behaviour in C# and use JS only for what the DOM cannot otherwise provide.
- Do use only native modern browser APIs.
- Do write every script in strict mode: a script is an ES module, which is strict by construction, and anything that ever stops being one opens with `'use strict'`.
- Do name JS parameters by role (`input`, `dialog`, `label`), not type (`inputEl`, `dialogElement`, `el`).
- Don't use a deprecated platform or language feature — `document.execCommand`, `queryCommandState`, `queryCommandValue`, and everything on MDN's deprecated JavaScript list; when one is found in reach of a change, replace it as part of that change rather than leaving it.
- Don't contort a component or ship a worse experience merely to remain script-free.
- Don't treat the amount of JS required as a reason to leave a rough edge unresolved.

## Behaviours, operations, components and services

Every script is one of four kinds. The kind decides its shape; the assembly that ships it decides its home.

```text
behaviour  → reusable attached behaviour
operation  → reusable stateless operation
component  → interop owned by a specific Forma component
service    → one instance per document that many callers register with
```

Scripts and their wrappers ship together in an interop assembly, never in `Forma.Blazor`.
`Forma.Framework.Core.Interop` is the floor: the interop base every wrapper builds on, `JsModule`
and `JsInterop`, and the scripts every other script builds on — the report to a .NET owner, the
selectors of the controls a list leaves alone, the frame, the reading direction, the scroller
lookup, the markup sanitizer — directly under its `wwwroot/scripts/` with no wrapper. `Forma.Framework.Common.Interop`
holds every behaviour and operation any component may attach, with the wrappers of components
that belong to no feature. A feature with scripts of its own ships them, and their wrappers, in
`Forma.Framework.{Feature}.Interop`, as `Forma.Framework.Input.Interop` ships the router and its
inspector. Every interop assembly is built on the core one, and a dependency between two others
is declared as a project reference, so the graph stays visible.

The assemblies are served side by side as `_content/forma/core`, `_content/forma/common`,
`_content/forma/input` and `_content/forma/composer`, each with its scripts under `scripts/`.
A script reaches another assembly's script by climbing to that root — `../../../core/scripts/Frame.js`
from a behaviour — so the import stays relative to the module and survives a host path base.

`Forma.Framework.Common.Interop` serves many concepts, so it sorts by kind: a script lives in
`wwwroot/scripts/behaviours/`, `operations/` or `components/`, and its wrapper in the matching
`Behaviours/`, `Operations/` or `Components/` folder. A feature interop assembly is named for its
one concept, so its scripts sit directly under `wwwroot/scripts/` and its wrappers at the assembly
root, as `Forma.Framework.Input.Interop` serves `scripts/InputRouter.js` beside `InputRouter.cs`.

| Kind | What it is | Script | C# wrapper |
|---|---|---|---|
| Behaviour | binds to an element, holds listeners or observers, needs teardown | `{Capability}.js` | `{Capability}.cs`, when C# attaches it |
| Operation | a stateless call against an element or a value | `{Concern}.js` | `{Concern}.cs`, when C# calls it |
| Component | the whole browser side of one component, both kinds together | `{Component}.js` | `{Component}.cs` |
| Service | one instance per document, holding state across every caller that registers with it, configured by the host and queryable | `{Service}.js` | `{Service}.cs`, a scoped service owning the module and the host's policy |

- Do file a script as a behaviour when it attaches to an element for a lifetime and is later torn down.
- Do file a script as an operation when it is a stateless DOM operation, or a set of related ones.
- Do file a script as a component when it owns the browser-side interop for one component and naturally holds both kinds; keep that interop together rather than splitting it by implementation shape.
- Do file a script as a service when there is one of it per document and its callers register with it rather than attach it: `InputRouter` keeps every scope's routes and the active ordering across the whole page.
- Do file what several scripts share, and nothing in C# reaches, in `Forma.Framework.Core.Interop`: `Owner.js` tells a .NET owner what happened, `Controls.js` names what native controls claim for themselves. A shared piece is never filed as an operation merely because it is stateless.
- Do name a service for what it is to the page: `InputRouter`.
- Do name a component script after the component it supports with the `Forma` prefix dropped, so `FormaDialog` is served by `Dialog.js` and `FormaRichTextEditor` by `RichTextEditor.js`.
- Do split a component script that has grown past one concern into `{Name}/*.mjs` modules behind a `{Name}.js` entry point beside them; the entry keeps its path, so no wrapper changes, and it composes the modules rather than re-exporting them.
- Do shape each such module as one concept expressed as a class — a formatter, a memory, an inspector — taking its collaborators once in the constructor, with `#`-private mechanics; not a bag of exported functions threaded the same context parameter.
- Do give a family of pure helpers a named class with static members rather than loose exports, so a caller imports the concept, not stray functions.
- Do name a behaviour for what the target can do while it is attached: `DropZone`, `DragGesture`, `Focus`, `Scroll`, `Breakpoint`, `AnchoredPopover`.
- Do settle that name by asking what `attach(target)` leaves the target able to do, and naming that.
- Do name an operation module for the browser concern it serves: `Text.js`, `Pointer.js`, `Playback.js`, `Clipboard.js`, `Asset.js`.
- Do lift a capability another component could want out into a behaviour or an operation in `Forma.Framework.Common.Interop`, however it arrived; a component script is for what only its own component wants.
- Do compose behaviours in JS, `FileDropZone` over `DropZone` and `ReorderInteraction` over `DragGesture`, so C# only ever holds the outermost one.
- Do leave a behaviour no C# wrapper when only JS attaches it, as `DragGesture` has none; where a capability sits never depends on who consumes it.
- Do read `Forma.Framework.Common.Interop`'s `wwwroot/scripts/behaviours/` before writing a new behaviour; capability names make the folder its own inventory.
- Don't name a behaviour after the browser primitive implementing it: something holding no state and needing no teardown, such as pointer capture or asset loading, is an operation.
- Don't name an operation module after the DOM type it receives; `MediaElement.js` accretes everything taking a `<video>`, where `Playback.js` and `Geometry.js` divide by what the calls are for.
- Don't name a shared behaviour or operation after a component that calls it. A consumer-shaped file in the shared assembly hides the capabilities inside it, which is how the same key guard, pointer capture, and asset loader each came to be written twice.
- Don't place a script beside the component that uses it; a component's own interop belongs in an interop assembly, not in the component's folder.
- Don't group unrelated capabilities in one file because one component happens to use both.

## Behaviour contract

```js
export function attach(target, options = {}) {
    return new Capability(target, options);
}
```

- Do export exactly one entry point, `attach(target, options)`, returning a handle with `dispose()`.
- Do keep the handle's state in private `#fields` of a module-private class.
- Do register every listener with an `AbortController` signal and abort it in `dispose()`.
- Do take a class name as an option rather than hardcoding one, because CSS class names belong to the component.
- Don't key state by element in a `WeakMap`, on an element property, or in a module-level `Map`; the caller holds the handle.
- Don't export a `detach`, `unobserve`, or `disconnect` counterpart; disposing the handle is the teardown.

## Operation contract

```js
export function capture(element, pointerId) {
    element.setPointerCapture(pointerId);
}
```

- Do export plain named functions, because an operation holds nothing and hands back no handle.
- Do name an export for the action and let the module supply the subject, so `Pointer.js` exports `capture` rather than `capturePointer`.
- Do qualify an export where its module covers more than one thing, so `Text.js` exports `take` beside `readSelection` and `writeSelection`.
- Don't reach for a class, private fields, an `AbortController`, or a `dispose`; needing any of them means a behaviour has been filed as an operation.

## Component contract

- Do export `attach(target, options)` for the component's own surface when it has one, beside whatever operations it needs, so one module covers the component's whole browser side.
- Do write each exported part to the contract above that fits it; the folder decides where the module lives, not how its parts are written.
- Don't reimplement a shared behaviour or operation inside a component module; import it.

## Service contract

```js
export function register(element, profile = {}, owner = null) {
    return scopeFor(element).add(profile, owner);
}

export function configure(options = {}) { … }
export function resolve(event) { … }
```

- Do export `register(...)` returning a registration with `dispose()`, beside `configure(options)` for the host and the queries anyone may ask; a service has no `attach`.
- Do keep state at module level, because there is one service per document and the registrations are its records; this is the one kind where a module-level `Map` is the design.
- Do let many registrations coexist, each disposed on its own, and let the service outlive any one of them.
- Do install the service's own listeners on first registration and remove them when the last registration goes.
- Don't turn a behaviour into a service because two components share it; a service exists only where the page needs one arbiter.

## Reaching a script from C#

- Do derive a behaviour wrapper from `JsInterop`, give it the module path, and expose a typed `AttachAsync`.
- Do give an operation wrapper a `JsModule` field rather than deriving from it, so only its own typed methods can be called.
- Do derive a component wrapper from `JsInterop` when its module attaches a surface, and give it a `JsModule` field when the module is operations alone.
- Do make a service's wrapper a scoped service at the root of its interop assembly with a `JsModule` field, registered in `AddFormaUI`, that pushes the host's policy once per circuit and hands out registration handles; it is not a plain wrapper, because it owns policy as well as the module.
- Do call an operation through `TryInvoke*`, so a dropped circuit leaves the call absent rather than throwing into a render.
- Do name a wrapper after the module it owns, with no `Bridge`, `Helper`, or `Interop` suffix.
- Do let `JsModule` and `JsInterop` handle importing, `JSException`, `JSDisconnectedException`, and disposal rather than repeating any of it.
- Do dispose the wrapper from the component's `DisposeAsync`.
- Do import from `./_content/forma/{core|common|input|composer}/scripts/{Name}.js`, with the kind folder in the path where the assembly sorts by kind, so the wrapper names the assembly that ships the script.
- Do suppress CA1812 on a private record that exists only to receive a JS return shape, since nothing constructs it in C#.
- Don't make a wrapper static; `IJSRuntime` is scoped per circuit and the module reference has to be disposed with it.
- Don't import a module by hand with `JS.InvokeAsync<IJSObjectReference>("import", …)`.
- Don't repeat a module path across components; the wrapper owns it.

### The silent-failure contract

`JsInterop.InvokeAsync` returns `false` when nothing is attached and when the circuit has dropped — by design, so a dead circuit cannot throw into a render. That silence cuts both ways: a call made before `AttachAsync` has answered is indistinguishable from one that ran.

- Do treat that returned `bool` as the record of whether the call happened, and drive state from it rather than assuming success.
- Do remember that `OnAfterRenderAsync` runs after *every* render, so a second pass can overtake a first one that is still awaiting its attach round-trip.
- Don't record state as applied beside an interop call whose result you discarded; the state sticks, nothing retries, and no error is raised anywhere.

## External libraries

- Don't use jQuery, npm packages, polyfills, or other external JS libraries. `FormaComposer` is the single carve-out, because an editor hosts capabilities — Markdown rendering, maths typesetting, syntax highlighting — that are solved problems nobody should reimplement.
- Do confine every such library to its own satellite package, `Forma.AddOns.Composer.{Capability}`, reached only through a plugin contract, so a host that does not want the capability never downloads it and the engine never depends on it.
- Do keep those libraries in a supporting role: a capability renders, converts, or classifies. The editing surface, the selection, the undo stack, and the toolbar stay Forma's, in C#.
- Do ship them as plain static assets under `_content/…`, loaded through the plugin's declared assets. A bundler, a build step, or an `npm install` is still out.
- Do make a capability that is absent or fails to load degrade to absent, never to a control that throws when pressed.
- Don't widen the carve-out past `FormaComposer`, and don't let a missing capability break the editor around it.
