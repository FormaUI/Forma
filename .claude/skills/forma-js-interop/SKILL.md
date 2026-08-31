---
name: forma-js-interop
description: Forma's JavaScript and interop standard — when JS is allowed at all, the behaviours/operations/components split that decides where a script lives and how it is written, the three module contracts, how C# reaches a module through JsInterop and JsModule, and the external-library carve-out for FormaComposer. Use when adding or changing anything under wwwroot/scripts/ or Interop/, naming a script or wrapper, or deciding whether a capability needs JS.
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

## Behaviours, operations and components

Every script is one of three kinds, and the kind decides its shape and its home.

```text
behaviours/  → reusable attached behaviour
operations/  → reusable stateless operation
components/  → interop owned by a specific Forma component
```

| Kind | What it is | Script | C# wrapper |
|---|---|---|---|
| Behaviour | binds to an element, holds listeners or observers, needs teardown | `wwwroot/scripts/behaviours/{Capability}.js` | `Interop/Behaviours/{Capability}.cs`, when C# attaches it |
| Operation | a stateless call against an element or a value | `wwwroot/scripts/operations/{Concern}.js` | `Interop/Operations/{Concern}.cs`, when C# calls it |
| Component | the whole browser side of one component, both kinds together | `wwwroot/scripts/components/{Component}.js` | `Interop/Components/{Component}.cs` |

- Do put a script in `behaviours/` when it attaches to an element for a lifetime and is later torn down.
- Do put a script in `operations/` when it is a stateless DOM operation, or a set of related ones.
- Do put a script in `components/` when it owns the browser-side interop for one component and naturally holds both kinds; keep that interop together rather than splitting it by implementation shape.
- Do name a component script after the component it supports with the `Forma` prefix dropped, so `FormaModal` is served by `components/Modal.js` and `FormaRichTextEditor` by `components/RichTextEditor.js`.
- Do split a component script that has grown past one concern into `components/{Name}/*.mjs` modules behind a `components/{Name}.js` entry point; the entry keeps its path, so no wrapper changes, and it composes the modules rather than re-exporting them.
- Do shape each such module as one concept expressed as a class — a formatter, a memory, an inspector — taking its collaborators once in the constructor, with `#`-private mechanics; not a bag of exported functions threaded the same context parameter.
- Do give a family of pure helpers a named class with static members rather than loose exports, so a caller imports the concept, not stray functions.
- Do name a behaviour for what the target can do while it is attached: `DropZone`, `NavigationScrollGuard`, `ScrollBoundaryWatcher`, `DragGesture`, `FocusTrap`, `InfiniteScroll`.
- Do settle that name by asking what `attach(target)` leaves the target able to do, and naming that.
- Do name an operation module for the browser concern it serves: `Text.js`, `Pointer.js`, `Playback.js`, `Clipboard.js`, `Asset.js`.
- Do lift a capability another component could want out into `behaviours/` or `operations/`, however it arrived; `components/` is for what only its own component wants.
- Do compose behaviours in JS, `FileDropZone` over `DropZone` and `ReorderInteraction` over `DragGesture`, so C# only ever holds the outermost one.
- Do leave a behaviour no C# wrapper when only JS attaches it, as `DragGesture` has none; where a capability sits never depends on who consumes it.
- Do read `wwwroot/scripts/behaviours/` before writing a new behaviour; capability names make the folder its own inventory.
- Don't name a behaviour after the browser primitive implementing it: something holding no state and needing no teardown, such as pointer capture or asset loading, is an operation.
- Don't name an operation module after the DOM type it receives; `MediaElement.js` accretes everything taking a `<video>`, where `Playback.js` and `Geometry.js` divide by what the calls are for.
- Don't name a shared behaviour or operation after a component that calls it. A consumer-shaped file in those folders hides the capabilities inside it, which is how the same key guard, pointer capture, and asset loader each came to be written twice.
- Don't place a script beside the component that uses it; a component's own interop belongs in `components/`, not in its folder.
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

## Reaching a script from C#

- Do derive a behaviour wrapper from `JsInterop`, give it the module path, and expose a typed `AttachAsync`.
- Do give an operation wrapper a `JsModule` field rather than deriving from it, so only its own typed methods can be called.
- Do derive a component wrapper from `JsInterop` when its module attaches a surface, and give it a `JsModule` field when the module is operations alone.
- Do call an operation through `TryInvoke*`, so a dropped circuit leaves the call absent rather than throwing into a render.
- Do name a wrapper after the module it owns, with no `Bridge`, `Helper`, or `Interop` suffix.
- Do let `JsModule` and `JsInterop` handle importing, `JSException`, `JSDisconnectedException`, and disposal rather than repeating any of it.
- Do dispose the wrapper from the component's `DisposeAsync`.
- Do import from `./_content/Forma.Blazor/scripts/{behaviours|operations|components}/{Name}.js`.
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
