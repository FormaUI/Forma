---
name: forma-scripting
description: The JavaScript and interop standard — when JS is allowed at all, the behaviors/operations/components/services split that decides where a script lives and how it is written, the four module contracts, how C# reaches a module through JSInterop and JSModule over the JS environment and what those log, and the external-library carve-out for the composer. Use when adding or changing anything under wwwroot/scripts/ or in an interop assembly, naming a script or wrapper, or deciding whether a capability needs JS.
---

# JavaScript

JS-free is the default, not a prohibition.

- Do prefer HTML, CSS, or Blazor when they provide the behavior cleanly.
- Do use interop when the platform exposes the needed behavior only to script or when declarative techniques leave a real usability problem.
- Do keep behavior in C# and use JS only for what the DOM cannot otherwise provide.
- Do make every key, press, click, wheel turn and drop a component acts on a route or an active registration on the input router, so which of two nested components owns an input is decided in one place; a native control's own activation, a button's click, stays the control's, and a component that only observes a press another owns, as one brought to the front by any press inside it, listens without preventing or stopping it.
- Do use only native modern browser APIs.
- Do write every script in strict mode: a script is an ES module, which is strict by construction, and anything that ever stops being one opens with `'use strict'`.
- Do name JS parameters by role (`input`, `dialog`, `label`), not type (`inputEl`, `dialogElement`, `el`).
- Don't use a deprecated platform or language feature — `document.execCommand`, `queryCommandState`, `queryCommandValue`, and everything on MDN's deprecated JavaScript list; when one is found in reach of a change, replace it as part of that change rather than leaving it.
- Don't contort a component or ship a worse experience merely to remain script-free.
- Don't treat the amount of JS required as a reason to leave a rough edge unresolved.

## Behaviors, operations, components and services

Every script is one of four kinds. The kind decides its shape; the assembly that ships it decides its home.

```text
behavior  → reusable attached behavior
operation  → reusable stateless operation
component  → interop owned by a specific component
service    → one instance per document that many callers register with
```

A script and its wrapper ship together, with the project that owns them. A product built on the
component library, such as a data view or an editor, ships the scripts only it uses itself: the
scripts under its `wwwroot/scripts/`, their wrappers in its `Interop/` folder. Everything else ships
in an interop assembly, since the component library's components and the Framework both reach it and
a Framework project cannot reference a product. The JavaScript interop assembly is the floor: the
interop bases every wrapper builds on, `JSModule`, `JSInterop` and the `IJSEnvironment` they take,
and the scripts every other script builds on, directly under its `wwwroot/scripts/` with no wrapper.
The browser interop assembly holds the behaviors and operations the component library's components
use or several products share, with the wrappers of the component library's own component scripts.
A Framework feature with scripts of its own ships them, and their wrappers, in an interop assembly of
its own. Every interop assembly is built on the JavaScript one, and a dependency between two others is
declared as a project reference, so the graph stays visible.

Every assembly's static assets are served under one content root, each assembly with its scripts under
`scripts/`. A script reaches another assembly's script by climbing to that root, so the import
stays relative to the module and survives a host path base.

The browser interop assembly is organized by area, under its DOM watcher and engine, as the section on
them below sets out. Its kind folders, `wwwroot/scripts/behaviors/`, `operations/` and `components/`
with their wrappers in the matching `Behaviors/`, `Operations/` and `Components/` folders, hold the
scripts no area has replaced yet, and are retired as conversions empty them. A feature interop
assembly is named for its one concept, so its scripts sit directly under `wwwroot/scripts/` and its
wrappers at the assembly root, a service's script beside the wrapper that owns it.

| Kind | What it is | Script | C# wrapper |
|---|---|---|---|
| Behavior | binds to an element, holds listeners or observers, needs teardown | `{Capability}.js` | `{Capability}.cs`, when C# attaches it |
| Operation | a stateless call against an element or a value | `{Concern}.js` | `{Concern}.cs`, when C# calls it |
| Component | the whole browser side of one component, both kinds together | `{Component}.js` | `{Component}.cs` |
| Service | one instance per document, holding state across every caller that registers with it, configured by the host and queryable | `{Service}.js` | `{Service}.cs`, a scoped service owning the module and the host's policy |

- Do file a script as a behavior when it attaches to an element for a lifetime and is later torn down.
- Do file a script as an operation when it is a stateless DOM operation, or a set of related ones.
- Do file a script as a component when it owns the browser-side interop for one component and naturally holds both kinds; keep that interop together rather than splitting it by implementation shape.
- Do file a script as a service when there is one of it per document and its callers register with it rather than attach it: an input router keeps every scope's routes and the active ordering across the whole page.
- Do file what several scripts share, and nothing in C# reaches, in the JavaScript interop assembly: a module that tells a .NET owner what happened, a module naming what native controls claim for themselves. A shared piece is never filed as an operation merely because it is stateless.
- Do name a service for what it is to the page: `InputRouter`.
- Do name a component script after the component it supports with the `Forma` prefix dropped, so a dialog component is served by `Dialog.js` and a rich-text editor by `RichTextEditor.js`.
- Do split a component script that has grown past one concern into `{Name}/*.mjs` modules behind a `{Name}.js` entry point beside them; the entry keeps its path, so no wrapper changes, and it composes the modules rather than re-exporting them.
- Do shape each such module as one concept expressed as a class — a formatter, a memory, an inspector — taking its collaborators once in the constructor, with `#`-private mechanics; not a bag of exported functions threaded the same context parameter.
- Do give a family of pure helpers a named class with static members rather than loose exports, so a caller imports the concept, not stray functions.
- Do name a behavior for what the target can do while it is attached: `DropZone`, `DragGesture`, `Scroll`, `Breakpoint`, `AnchoredPopover`.
- Do settle that name by asking what `attach(target)` leaves the target able to do, and naming that.
- Do name an operation module for the browser concern it serves: `TextEntry.js`, `Pointer.js`, `Playback.js`, `Clipboard.js`, `Asset.js`.
- Do lift a capability out of a component script into a behavior or an operation once a second component uses it; a component script is for what only its own component wants.
- Do move a script from a product into the browser interop assembly only when a second product needs it, never in anticipation of one.
- Do compose behaviors in JS, `FileDropZone` over `DropZone` and `ReorderInteraction` over `DragGesture`, so C# only ever holds the outermost one.
- Do leave a behavior no C# wrapper when only JS attaches it, as `DragGesture` has none; where a capability sits never depends on who consumes it.
- Do read the existing behaviors before writing a new one; capability names make their folder its own inventory.
- Don't name a behavior after the browser primitive implementing it: something holding no state and needing no teardown, such as pointer capture or asset loading, is an operation.
- Don't name an operation module after the DOM type it receives; `MediaElement.js` accretes everything taking a `<video>`, where `Playback.js` and `Geometry.js` divide by what the calls are for.
- Don't name a shared behavior or operation after a component that calls it. A consumer-shaped file in the shared assembly hides the capabilities inside it, so the next component that needs one writes it again.
- Don't place a script beside the component that uses it; a project's scripts live under its `wwwroot/scripts/`, never in a component's folder.
- Don't group unrelated capabilities in one file because one component happens to use both.

## The DOM watcher and engine

The browser interop assembly's DOM watcher and engine are the shared browser foundation for the
capabilities they cover: an owner observes its elements through the watcher and acts on them through
the engine, with one lifetime, one notification path and one release for everything it holds. They
are organized by concern rather than by kind.

- Do put a capability the watcher or the engine offers in a folder for its area under the assembly's `wwwroot/scripts/`, such as `scrolling/`, one concept per `.mjs` module written as a class taking its collaborators once, and its C# in the matching folder and namespace.
- Do keep the foundation the capabilities share, the element, the registrations and the sessions, at the root beside `DomWatcher.js` and `DomEngine.js`; a folder is for an area a reader recognizes, never a home for the mechanisms its neighbors share.
- Do make a calculation a member of the concept whose state it reads, as stepping and measuring belong to the scroll container, rather than a class of its own holding one static method.
- Do keep `DomWatcher.js` and `DomEngine.js` to the private surface their C# calls, routing each of an owner's calls to its session; the `DomWatcher` and `DomEngine` classes in modules of their own hold an owner's registrations by the token and generation its C# minted and load a concern's modules on first use, and a capability's mechanics stay in its concern.
- Do keep each owner's session at module level under its runtime identity, as a service keeps its registrations, so one call releases everything the owner holds, its engine, then its watcher, then the elements they share.
- Do let the session own what its watcher and engine share, the owner's elements among them, and make its parts itself, handing each its collaborators through its constructor; the engine's class reaches the session from the engine's entry, so observing never loads it.
- Do share what the document needs once, such as the resize observer, as a small module with module-level state, installed with its first user and removed with its last.
- Do keep the watcher's import graph free of the engine and the input router, so observing loads neither.
- Do give a capability its C# as a typed object on the element an owner gets, `GetElement(element).Scroll`, made through the owner's session; a component declares no `[JSInvokable]` method and holds no `DotNetObjectReference` for it.
- Do send each state through the owner's receiver with one sample in flight and only the latest waiting, and report a failed send in the browser, under the owner, the capability and the registration.
- Do route an input an interaction acts on through the input router, so the host's remapping reaches it and a native control keeps its own activation.
- Do convert a script in a kind folder by replacing it and its wrapper with its area's concepts, never by relocating it first; move a script as it is only where it survives unchanged, as the virtualizer's and the embedding adapter's do, and only when there is reason to work on it.
- Do leave the input router and a product's own scripts out of this organization; they keep their own assemblies.
- Do move a concern with its consumers and remove the old entries it replaces; an older wrapper its remaining consumers still use keeps only what they use, and no algorithm exists in two copies.
- Don't discard a rejection with `.catch(() => {})` in this foundation; a failure is reported where it happens and never through the channel that failed.

## Behavior contract

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
- Do qualify an export where its module covers more than one thing, so `TextEntry.js` exports `take` beside `readSelection` and `writeSelection`.
- Don't reach for a class, private fields, an `AbortController`, or a `dispose`; needing any of them means a behavior has been filed as an operation.

## Component contract

- Do export `attach(target, options)` for the component's own surface when it has one, beside whatever operations it needs, so one module covers the component's whole browser side.
- Do write each exported part to the contract above that fits it; the folder decides where the module lives, not how its parts are written.
- Don't reimplement a shared behavior or operation inside a component module; import it.

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
- Don't turn a behavior into a service because two components share it; a service exists only where the page needs one arbiter.

## Reaching a script from C#

- Do derive a behavior wrapper from `JSInterop`, take `(IJSEnvironment js)` and pass it on with the module path, and expose a typed `AttachAsync`.
- Do give an operation wrapper a `JSModule` field built from the same `js` rather than deriving from it, so only its own typed methods can be called.
- Do derive a component wrapper from `JSInterop` when its module attaches a surface, and give it a `JSModule` field when the module is operations alone.
- Do make a service's wrapper a scoped service at the root of its interop assembly with a `JSModule` field, registered by the aggregate registrar, that pushes the host's policy once per circuit and hands out registration handles; it is not a plain wrapper, because it owns policy as well as the module.
- Do call an operation through `TryInvoke*`, so a dropped circuit leaves the call absent rather than throwing into a render.
- Do name a wrapper after the module it owns, with no `Bridge`, `Helper`, or `Interop` suffix.
- Do let `JSModule` and `JSInterop` handle importing, `JSException`, `JSDisconnectedException`, and disposal rather than repeating any of it.
- Do construct a wrapper from the context's `JS`, `new Wrapper(Context.JS)`, and dispose it from the component's `DisposeAsyncCore`; `Context.JS` is the component's `IJSEnvironment`, the runtime carrying the component's log and identity, so the wrapper logs under the component and names it. A service that owns a wrapper builds its own `JSEnvironment` from the runtime and the `ILogger<T>` it takes, minting an identity for the instance.
- Do import through the content root of the assembly that ships the script, with the script's area or kind folder in the path, so the wrapper names the assembly that ships the script.
- Do suppress CA1812 on a private record that exists only to receive a JS return shape, since nothing constructs it in C#.
- Don't make a wrapper static; `IJSRuntime` is scoped per circuit and the module reference has to be disposed with it.
- Don't import a module by hand with `JS.InvokeAsync<IJSObjectReference>("import", …)`.
- Don't repeat a module path across components; the wrapper owns it.

### The silent-failure contract

`JSInterop.InvokeAsync` returns `false` when nothing is attached and when the circuit has dropped — by design, so a dead circuit cannot throw into a render. That silence cuts both ways: a call made before `AttachAsync` has answered is indistinguishable from one that ran. What the wrapper absorbs it writes at `Debug` under the owning component's category, naming the stage it failed at, import, call or release, the export where a call reached one, the module and the owner's `RuntimeId`, so turning the category on shows what a `false` hid; a call made before attaching is not a failure and is not logged.

- Do treat that returned `bool` as the record of whether the call happened, and drive state from it rather than assuming success.
- Do remember that `OnAfterRenderAsync` runs after *every* render, so a second pass can overtake a first one that is still awaiting its attach round-trip.
- Don't record state as applied beside an interop call whose result you discarded; the state sticks, nothing retries, and no error is raised anywhere.

## External libraries

- Don't use jQuery, npm packages, polyfills, or other external JS libraries. The composer is the single carve-out, because an editor hosts capabilities — Markdown rendering, maths typesetting, syntax highlighting — that are solved problems nobody should reimplement.
- Do confine every such library to its own add-on package, one per capability, reached only through a plugin contract, so a host that does not want the capability never downloads it and the engine never depends on it.
- Do keep those libraries in a supporting role: a capability renders, converts, or classifies. The editing surface, the selection, the undo stack, and the toolbar stay the library's, in C#.
- Do ship them as plain static assets under `_content/…`, loaded through the plugin's declared assets. A bundler, a build step, or an `npm install` is still out.
- Do make a capability that is absent or fails to load degrade to absent, never to a control that throws when pressed.
- Don't widen the carve-out past the composer, and don't let a missing capability break the editor around it.
