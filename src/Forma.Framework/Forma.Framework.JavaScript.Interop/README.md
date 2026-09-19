# Forma.Framework.JavaScript.Interop

The floor every interop assembly stands on: the C# bases a wrapper is built from, and the scripts
the other interop assemblies' scripts import. It is not a package; it ships inside
`2Site.Forma.Blazor`, and a host never references it.

Its static assets are served from `_content/forma/core`, so a script in another interop assembly
imports one of these by climbing to that root, as `../../../core/scripts/Owner.js` does from a
behavior in the browser interop assembly. No C# reaches a script here; the scripts have no wrappers.

## The bases

| Type | What it is |
| --- | --- |
| `IJSEnvironment` | The runtime a wrapper calls, carrying the `RuntimeId` and `Logger` of the owner whose interop it runs. |
| `JSEnvironment` | An `IJSEnvironment` over any `IJSRuntime`, built for one owner from its log and identity. |
| `JSModule` | A lazily-imported module: `InvokeAsync` and `InvokeVoidAsync` throw on failure, `TryInvokeAsync` and `TryInvokeVoidAsync` report it as `false` or `default`. |
| `JSInterop` | The base of a wrapper whose module attaches to an element: `AttachCoreAsync`, `DetachAsync`, `InvokeAsync` on the attached instance, `InvokeModuleAsync` on the module, and disposal as teardown. |
| `UnavailableJSRuntime` | The runtime a component gets where none is registered; every call throws. |
| `JSRuntimeUnavailableException` | Thrown by that runtime, naming the identifier that could not be invoked. |

A failure a wrapper absorbs — an import, a call or a release that threw or lost its circuit — is
written at `Debug` under the owner's log, naming the stage, the module, the export where a call
reached one, and the owner's `RuntimeId`.

## The scripts

Each is one concept as a class or a single export, under `wwwroot/scripts/`.

| Script | What it is |
| --- | --- |
| `Controls.js` | The selectors and keys native controls answer themselves: what Tab reaches, what owns its own press, what moves a caret, and the navigation keys a list owns. |
| `Direction.js` | Whether an element lays out right to left. |
| `FrameScheduler.js` | One piece of work held to the next animation frame, however often it is requested before then. |
| `Owner.js` | `tell(owner, method, ...args)`: reports to a behavior's .NET owner, letting go of the rejection a disposed owner answers with. |
| `Sanitizer.js` | `sanitize(html)`: untrusted markup reduced to the elements, attributes and URL schemes a document may carry. |
| `Scrolling.js` | Which elements scroll: whether one does, whether it scrolls on its own, and the nearest ancestor that does. |

## Reaching a module from a component

A component constructs a wrapper from its `Context.JS`, the `IJSEnvironment` carrying the
component's log and `RuntimeId`, and disposes it from its `DisposeAsyncCore`; a service that owns a
module builds its own `JSEnvironment` from the runtime and the `ILogger<T>` it takes.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
