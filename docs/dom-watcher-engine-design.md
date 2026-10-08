# Forma DOM Watcher and Engine Design

`DomWatcher` observes document, browser window and element state. `DomEngine` uses the watcher and provides object APIs for DOM actions and ongoing interactions. Together they give browser integrations a shared registration lifetime and notification path, so a component calls typed capabilities instead of repeating attachment loops, callback references and error handling.

The [implementation plan](dom-watcher-engine-implementation-plan.md) holds the delivery order, the first consumer's scope, the measurements and the gates.

## Scope

| Decision | Contract |
| --- | --- |
| Two entry points | `DomWatcher` observes; `DomEngine` acts and borrows the watcher. |
| Independent watcher | Watcher-only use loads neither the engine's modules nor input, and configures no input. |
| One target model | Observation interfaces and full capability interfaces share one target identity and one observation implementation. |
| Object API | Typed capability methods hide module paths and individual exports from components. |
| Shared lifetime | Persistent observations and interactions reconcile requested state against successfully applied state. |
| One notification channel | One callback receiver per owner dispatches notifications and surfaces failures. |
| Existing specialist systems | `InputRouter.js`, its registration contracts, and `Virtualize.js` with its wrapper keep their responsibilities and public contracts. |
| Managed ownership | Blazor keeps rendering, application state, collections, validation and workspace policy. |
| Packaging | The API lives in `Forma.Framework.Browser.Interop`, served from `_content/forma/browser`. |

The watcher and the engine provide concrete browser capabilities and a small set of compositional contracts. Each capability arrives with a consumer that needs it.

## Principles

The watcher and the engine are the shared browser foundation for components whose capabilities they cover, alongside the specialized input router, virtualizer and product integrations. They hold to these principles.

- **At least as good as today.** A conversion preserves the existing behavior contract and takes directly related improvements: a missed edge case, a redundant listener, a needless crossing. Deliberate behavior corrections are documented and verified alongside the preserved behavior.
- **Fewest round trips.** Work that can finish in the browser finishes there, and each crossing carries the related work its timing and contract allow, as the section on crossing sets out.
- **Built to scale.** Resource use follows active targets and requested capabilities. Compatible native observers are pooled, document listeners are shared where event semantics allow delegation, and unused capabilities install nothing. Pooling reduces observer instances; each active target still requires bookkeeping.
- **Current platform, current language.** The code targets the repository's supported evergreen browsers and .NET version, using ES modules, classes with `#private` members and `AbortSignal` lifetimes without a polyfill or a JavaScript bundling/transpilation step. Optional browser facilities report capability availability. Hot paths avoid redundant allocations, and allocation measurements include serialization and immutable notification records.
- **Small modules, one concept each.** Every concept is a class in a module of its own, taking its collaborators once, and entry modules compose them. No module becomes the place concerns accumulate.

## Responsibilities and dependencies

A **target** is a live element, the document or the browser window. A **capability** is a concern such as scrolling or focus. An **observation** retains requested state and its listeners. An **interaction registration** retains an ongoing action and its resources. Observations and interactions share one private lifetime discipline.

`DomWatcher` only observes: it reads state and reports it. `DomEngine` moves focus and writes the DOM properties its capabilities own through explicit contracts. Its input interactions participate in the existing router's ownership and cancellation rules. The engine consumes watcher observations directly in JavaScript where managed reporting is unnecessary.

`InputRouter` remains the authority over input ownership; the [input router design](input-router-design.md) defines it. Engine interactions register through its existing API, and a watcher's event observation confers no input ownership. The virtualizer keeps its measurement, rendered-range, pinning, scrolling and callback algorithms, as the [virtualization design](virtualization-design.md) states them.

## Public object model

### One target and capability model

The watcher exposes `Document`, `BrowserWindow` and `GetElement(elementReference)` through observation-only interfaces. The engine exposes the same target model with its actions. An element has one `DomElement` implementation, backed by one owner-scoped target record, with read-only interface projections for the watcher. Capabilities are accessed through `GetElement`, a local call that makes no crossing.

`IDomElementObservation.Scroll` exposes `WatchAsync`; the engine's `DomElement.Scroll` exposes `WatchAsync` and `StepAsync`. Capability objects share their observation implementation, and document and browser-window capabilities follow the same rule. The interfaces express authority over one observation graph.

The shared target implementation belongs to the observation foundation and imports no engine code. The engine binds action adapters lazily, so obtaining a watcher interface never initializes them, and an action call requires a live engine binding whatever view the caller holds. Getting a target or a capability property installs no observation.

An element view refers to that exact element; a persistent registration moves to a replacement element only through an explicit synchronization. Targets belong to the current document, and a cross-document target is rejected. Shadow-root internals are reached only through a target given explicitly.

Browser-wide facilities, such as `Clipboard` and `Downloads`, are engine properties.

### Watching and acting

```csharp
// Watcher-only use.
_scroll = await Context.DomWatcher.GetElement(_viewport)
    .Scroll.WatchAsync(HandleScrollChangedAsync, observationOptions);

// The engine's element exposes observation and actions together.
var viewport = Context.DomEngine.GetElement(_viewport);
await viewport.Scroll.StepAsync(direction, stepOptions);
```

`WatchAsync` registers its asynchronous handler before browser establishment begins. A successful establishment provides an initial snapshot and an initial notification, including when every measured value is zero or false. The callback reads its immutable argument, so it works before the field receiving the returned observation is assigned. Components marshal rendered changes through `InvokeAsync`.

A `DomObservation<TState>` has a nullable `Current`, `IsActive`, `IsDisposed`, `SyncAsync`, `RefreshAsync` and `DisposeAsync`. `IsActive` means an established subscription with an accepted baseline. `Current` is absent before establishment and is cleared on disabling, retargeting, known detachment or known loss of reporting. These are local facts; they make no synchronous claim about the connection's health.

An unavailable establishment leaves the retained observation inactive and retryable on the next synchronization. Invalid options and unexpected failures are surfaced. A specific status or result is added when a consumer needs to tell it apart.

`SyncAsync(element, options, shouldObserve)` records the latest desired observation; interaction registrations use the same pattern with `shouldEnable`. Call and await `SyncAsync` after each relevant render, passing the current target, options and enabled state. The registration handles overlapping calls and reconciles the latest request; the component needs no synchronization guard. Disabling releases native resources, and enabling again establishes a fresh baseline. Setup begins once a valid element reference exists.

### Notifications and occurrences

Handlers return `Task`. The initial `WatchAsync` handler and additional listeners use one asynchronous notification mechanism, and adding a listener returns a disposable subscription. An observation's lifetime is separate from any one listener's. A new state listener receives the current baseline before later changes; an occurrence listener receives only occurrences after it subscribed.

State and occurrences differ in meaning. Scroll boundaries are state; a native close or an accepted drop is an occurrence. Occurrences are delivered through typed subscriptions, one per kind of occurrence.

## What the watcher observes

Observation is explicit: a capability installs listeners only when something observes it.

| Capability | State or notification | Observation and boundaries |
| --- | --- | --- |
| Document | Visibility, document focus, readiness | Native document and browser events; visibility and focus are separate facts. |
| Browser window | Layout viewport dimensions, scroll, device pixel ratio when requested | Resize, scroll and media-query invalidation. Visual viewport data is a separate requested projection. |
| Element connection | Connected or detached | Removal checks and scoped mutation observation. A hidden element remains connected. |
| Geometry | Size, or a rectangle in a declared coordinate space | Resize observations plus the invalidation sources position requires. |
| Intersection | Intersection with a declared root and margin | Intersection observer semantics; intersecting does not mean unobscured. |
| Scroll | Logical offset, available extent, viewport extent, start and end boundaries | Scroll, size and relevant content changes. Numeric offsets are optional where the consumer needs only boundaries. |
| Focus | Focus within a surface and the relevant focus transitions | Browser-local focus identity and event sequence; a document blur is distinct from leaving the surface. |
| Text entry | Requested value, selection and supported autofill signals | Input and selection events and explicit refresh. Unrelated property assignments are not universally observable; explicit refresh supplies a fresh read when no supported signal occurs. |
| Presentation | Native dialog and popover open state, and close and toggle occurrences | Native events plus direct reads after engine actions. Workspace activation remains managed state. |
| Media | Requested playback state | Native media events and property reads. |
| Attributes or structure | An explicitly named attribute set or subtree change | Narrow mutation observations, each scoped to what its consumer names. |

### Meaning of state

C# state is the last accepted browser snapshot, an asynchronous report of what the browser measured. Unknown state is absent, never a fabricated zero, empty string or false.

Each state projection has a documented unit and meaning. Geometry uses CSS pixels; text offsets use the native text-entry selection units. Client rectangles are relative to the layout viewport; document coordinates and visual viewport offsets are separate values. Logical scrolling normalizes direction, while geometry keeps explicitly physical coordinates. The existing `ScrollAxis` and start and end vocabulary carry over.

A boundary-only projection compares boundary values, so movement through the middle alone produces no notification. A combined projection still reports changes to any of its requested extents or other fields. A geometry projection compares its requested measurements within an explicit tolerance. Document and Boolean state compare exactly. Consumers choose a named projection; JavaScript evaluates only the projections it defines.

### Observation and position

Size observation covers size, and position has more causes than size: an anchor moves when an ancestor scrolls, when surrounding content changes, or when an animation transforms it ([ResizeObserver](https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver), [client rectangles](https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect)). Each position observation therefore declares its invalidation coverage: the relevant scrolling ancestors, size changes, owner-supplied layout invalidation, and optional continuous measurement while a live interaction or animation needs it.

`RefreshAsync` requests a new read, and JavaScript consumers can invalidate an observation locally. Continuous measurement is opt-in and ends with its registration.

Autofill is observed through CSS autofill animations, as a named browser adapter where needed; the watcher reports the signal the browser supports.

### Sharing observations

Where several consumers observe the same thing, sharing is keyed by the actual target, capability and normalized options, so different intersection roots or thresholds stay separate requests. Observer pools share compatible browser primitives while keeping each subscription distinct. A resize observer can cover several targets with target-specific options; an intersection observer shares only compatible roots, margins and thresholds. Mutation observation uses the coverage its source needs. Event listeners delegate only where the event's phase, bubbling and cancellation semantics permit it.

The browser watcher keeps document-level infrastructure only while subscribers need it. Native elements may be indexed internally with a `WeakMap`, and active subscriptions are explicitly reference-counted. Live registrations, listeners and callback references are still released by disposal.

## Crossing between C# and the browser

A managed-to-browser invocation on Blazor Server normally waits for a network response. Browser-to-managed notifications and module fetches are counted separately, so optimizing one does not hide the cost of another.

- **One cached import per used root and runtime scope.** The bridge shares the successful import and any in-flight import for each root. Watcher use loads only `DomWatcher.js`; the first action also loads `DomEngine.js`. The scope is a circuit on Server and the client application's service lifetime on WebAssembly. Browser-local capability imports are separate network fetches, not additional managed interop invocations.
- **Create and attach together.** Once its required root is imported, a registration call creates the owner's browser session if needed and attaches that registration. Subsequent registrations reuse it. Concurrent first requests share session creation, and a standalone command can create the session on first use as well. Required input-policy configuration remains visible in the crossing count.
- **The baseline in the result.** Establishment returns its initial snapshot with the attach result. The managed side delivers the initial notification from that snapshot, with the ordering barrier described below, saving a separate initial callback.
- **No call for an unchanged, applied request.** `SyncAsync` compares target and options against successfully applied state. An equal request still retries inactive or known-invalid state. `RefreshAsync` explicitly requests a browser read and restores suspended reporting even when options are equal; local equality cannot prove browser health.
- **Combined snapshots.** Related projections a consumer reads together travel as one snapshot, as scroll boundaries and frozen extents do.
- **Local interaction.** Input routes, gestures, panning, wheel movement, button-requested actions such as a scroll step, and placement run in the browser, and C# hears only the outcomes it requested. A local action makes no command round trip; a state change it causes still reaches C# as a notification.
- **One owner-resource release in ordinary teardown.** Context teardown marks all registrations disposed locally, then makes one browser call to release the owner's resources, engine before watcher. It uses this aggregate path rather than calling each child's public disposal method. An individual registration or borrowed engine still supports independent release. Compensating releases for late setup results and framework object-reference disposal calls are counted separately; correctness takes precedence over the ordinary-path call count.

Private registration tokens identify browser-owned objects under the owner session; a separate managed JavaScript object reference per registration is unnecessary. The bridge's dispatch surface remains private and finite, and the public C# and JavaScript APIs remain typed capability objects.

Interop crossings are counted per scenario by the opt-in diagnostics. Counts distinguish cold initialization, warmed-root registration, input-policy setup, ordinary synchronization, notifications and teardown.

## Notification and timing

### Initial state and ordering

After capability loading, establishment installs listeners and captures a baseline in one browser task. The managed observation exists before that work starts. The managed side mints the registration's token and generation before the call and passes them with the attach request; the browser stamps every sample with them, and the attach result returns the baseline under the same identity. The managed receiver holds any early sample behind an establishment barrier, retaining only the latest pending state until that result is accepted. It installs `Current` and queues the initial notification before releasing later samples. A late result for an obsolete generation is discarded and its browser registration released.

The browser attach path never awaits a managed notification, and the barrier opens before application handlers run. Establishment completes once the baseline is accepted, independently of callback completion. A failed establishment cancels its barrier, completes any delivery it was holding as discarded, so the browser's in-flight slot is free for a retry, and clears pending samples. This orders the returned baseline and asynchronous callbacks without a separate acknowledgement call.

The browser sends a registration's first sample no sooner than a task after its attach returns, folding anything measured meanwhile into one latest sample. That makes an early sample rare on Blazor Server, but the barrier does not depend on it: a failed attachment, a disposal and an obsolete generation each release any delivery the barrier holds, without deadlock.

Each registration has an internal identifier and a configuration generation. Replacing or disabling a target invalidates the previous generation immediately, and messages from obsolete generations are rejected. Delivery within a generation is serialized; a sequence number is added only where the transport needs one to enforce that order, and stays private.

Changes from separate subscriptions carry no global order. A capability that needs an atomic combined result supplies one snapshot, as scrolling and frozen-region measurement do together.

### Timing each capability preserves

| Timing | Use | Rule |
| --- | --- | --- |
| Synchronous browser handling | Input ownership, cancellation, capture, action preconditions | Runs in the browser handler without waiting for C#. |
| Mutation-time browser handling | Recovery after a focused node is removed | Runs at mutation delivery, before deferred reporting, in the browser. |
| Scheduled state | Geometry and scroll measurements | Coalesces invalidations and reports changed requested projections. |
| Ordered occurrences | Close, accepted drop, completed reorder | Preserves accepted occurrences and their order while delivery is healthy. |

These are constraints on each capability's implementation. Local watcher subscriptions keep the timing their consumers require; managed observations are asynchronous snapshots.

### Bounded delivery and visible failure

The owner has one managed callback reference and one typed receiver, shared by watcher and engine notifications. Subscriptions dispatch independently, and no lock is held across application code.

For state, each subscription keeps one delivery in flight and one latest pending sample, with the initial baseline protected. The interop invocation can stay outstanding until that subscription's handler work completes; establishment, commands and unrelated subscriptions proceed meanwhile. Several listeners on one observation are awaited deliberately, each failure isolated and logged.

A rejected application handler is reported with the owner, capability and registration identity. A transport failure is reported locally in the browser and suspends managed delivery; bounded state is retained, and delivery resumes through explicit resynchronization. Every failure is reported: a shared helper never swallows a rejection the way `.catch(() => {})` does, and failure reporting never runs through the channel that failed.

Occurrences are delivered through a bounded ordered queue per consumer, with an explicit failure on overflow, and are never coalesced into state. Ordering applies while delivery is healthy; an occurrence whose delivery becomes uncertain on disconnection is not automatically replayed. State recovery uses a new snapshot, while occurrence recovery follows the capability's contract.

### Commands and notifications

An action can invalidate a watcher observation, and the watcher publishes the measured resulting state with its normal comparisons. A command never fabricates a successful observation.

## Engine capabilities

The engine is a facade over concern-specific objects, each composing ordinary classes with typed options and collaborators.

| Concern | Capability |
| --- | --- |
| Scrolling | Step, reveal, offsets, wheel movement, panning, edge scrolling |
| Focus | Focus, memory, restoration, roving traversal, wrapping, mutation-time recovery |
| Placement | Anchoring, bounds, movement, resizing |
| Presentation | Native dialogs, manual popovers, scrim pairing, top-layer order |
| Pointer | Capture and release, and reusable gesture mechanics |
| Text and controls | Selection, read and clear, checkbox indeterminate state, autofill adapter |
| Events | Supported native dispatch and custom events with typed options |
| Files | Drops, previews, temporary resource ownership |
| Media and browser facilities | Playback, clipboard, downloads, printing, sharing, assets |

Composition lives in classes: anchored presentation combines placement and focus, and panning combines gestures and scrolling. A combination is expressed as a class with typed options, never as a serialized description of actions.

`Scroll.AttachAsync(options)` returns a persistent scroll interaction, and `Scroll.WatchAsync(...)` returns its requested state projection; both use the shared synchronization. Input policy is configured lazily before a qualified wheel route attaches. Movement stays local, and the component receives only its requested projection.

JavaScript callers create a watcher with `DomWatcher.create()` and an engine with `DomEngine.create(watcher)`, then obtain target objects; the modules export these classes. Targets are supplied to each observation. Capability loading is awaited before synchronous input handlers are installed, so such handlers call already-loaded local methods. A JavaScript-only watcher needs no managed callback reference.

Event dispatch distinguishes native event constructors from custom JSON detail. It reports synchronous cancellation, which is complete when dispatch returns; asynchronous managed handlers run later. Synthetic dispatch creates no trusted input and grants no user activation ([dispatch semantics](https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/dispatchEvent), [user activation](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/User_activation)). Setting a property and dispatching an event are separate actions: the file-drop composition assigns the accepted files and then raises its one change event.

## Component browser mechanics

The browser mechanics of dialogs, frames and windows become compositions of the shared objects; their Blazor components and domain decisions remain.

### Native dialogs

A native dialog element uses the engine's native dialog presentation object and the watcher's close and open observation. Native modality and the dialog element remain intact. Input dismissal is still routed through the component's existing input registration.

The component owns its `Opened` parameter, its confirmation and cancellation callbacks, and its close policy. A browser close can originate from a routed request, a dialog form or native behavior; these paths converge on one component close transition, so `OnClose` is raised once. The engine never interprets a native close as approval of an application-level confirmation.

### Frames

A frame uses a placement registration over an explicitly designated box, which may be its own element or its presenter's element. Options supply the drag region, resize handles, bounds, limits, keyboard actions and visual markers, so the framework names no Forma component class.

Movement and resizing stay local through a gesture, with a placement result at completion. A bounds change can also report a corrected placement. Gesture cancellation retains and reports the position reached, as the frame does today; a rollback policy is a separate API decision.

The component retains `Placement`, `Maximized`, `Locked`, its disabled state, handles, labels and callbacks. Frame geometry carries no workspace identity or stacking policy. Its arrow actions are ordinary routes through the existing router API.

### Windows

A window composes manual-popover presentation, optional scrim pairing, focus memory, and the frame's placement registration. `WindowManager` remains the authority over activation, ordering, parentage, minimization, blocking and close approval, as the [window design](window-design.md) states.

The engine reflects the order the manager supplies; activation and the logical window stack remain the manager's. Raising a surface preserves pointer capture: a hide-and-show that would interrupt a press is deferred until release, cancellation or browser-window blur. Raising a surface leaves the focus where it is.

Native dialogs and manual popovers are separate presentation modes, each with its own contract.

### Focus continuity

Focus recovery uses the watcher locally at mutation time. The owner chooses the successor and marks it in the same render that removes the focused node. Recovery proceeds only when the old node has disappeared and focus has fallen away, so focus the reader moved elsewhere stays where they put it.

Focus memory, Tab wrapping and roving traversal remain distinct options. Native controls, nested scopes, disabled elements, reading direction and the router's ownership rules keep their behavior. Recovery stays in the browser, because a focus-change callback sent to C# would lose the timing it needs.

## Identity, ownership and lifetime

### Ownership

An owner has one watcher and an optional engine, sharing target identity, diagnostics and notification transport. The engine borrows the watcher: disposing the engine leaves the watcher, and disposing the watcher first ends dependent engine use. Each lifetime belongs to whoever created the object.

A private bridge is registered with `TryAddScoped` through the browser interop registrar. Its lifetime follows the interactive `IJSRuntime`: a circuit on Server and the application's service scope on WebAssembly, as described by [Blazor dependency injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection). Each root has its own lazy import; an owner borrows those imports and owns its own session, callbacks, runtime identity and diagnostics. Sharing imports does not retain the first component's environment or logger as the identity of every subsequent owner.

Prerendering creates no browser session and performs no import. Concurrent requests share an in-flight import. A rejected managed import attempt is cleared for a later explicit retry; unexpected module evaluation failures remain visible and are not retried in a loop. Disposing the bridge prevents new imports and releases any reference arriving from a late import. Owners never dispose shared module references. Application-resolved watchers use the registered bridge. Directly constructed watchers own a private bridge; only the registered construction path promises import reuse between owners.

Targets are cheap managed views. Observations and interactions own resource leases and release them independently. Private identifiers are tokens, never HTML ids. The bridge and its owners use the same interactive runtime scope; process-wide state never captures a Server circuit's `IJSRuntime`. Independent use constructs a watcher from an `IJSEnvironment`; the caller owns it and any engine it creates.

### Component context

`IComponentContext` exposes lazy `DomWatcher` and `DomEngine` properties. `ComponentContext` owns the objects it creates, using its existing JavaScript environment, runtime identity and logger. Constructing the engine configures no input; routed capabilities resolve the existing input collaborator when they first need it.

`FormaComponentBase.DisposeAsync` runs derived cleanup, then disposes an already-created context in a `finally` block, creating no context during teardown. Cleanup is idempotent, attempts every owned release even when one fails, and creates nothing after disposal. The context uses the aggregate owner-resource release: engine resources are released before watcher resources within that call. Late setup results are invalidated and released as they arrive. Shared imports remain owned by the bridge.

### Synchronization and racing renders

Every observation and interaction separates desired state from successfully applied state. `SyncAsync` records the latest target and value-compared options, and one settling loop applies changes until the latest request is settled, following the discipline `InputRegistration` established.

A request arriving during an interop call remains pending. A late attachment made obsolete by replacement or disposal is released. Failed setup is never recorded as applied. Every caller joins the active settling task. Completion means the loop has processed the latest request known at settlement; intermediate requests can be superseded without being applied. A Boolean establishment result, where one is needed, describes current application, not proof that each caller's original configuration ran. A later call can begin a new settlement. The existing `InputRegistration` keeps its current await semantics; the new helper applies to DOM registrations.

Configuration is latest-wins; commands are each kept. Two explicit dispatches or downloads remain two requests, invocation order within a capability is preserved, and asynchronous completion is documented separately. Cancellation or supersession happens only where a method's contract defines it.

Generation invalidation guards every registration, because setup, parameter changes and disposal race. It is a private guard. Disposal invalidates queued work immediately, and a disposed registration never comes back.

### Cleanup and reconnection

Registrations release their listeners, observers, scheduled frames, gestures, temporary URLs and helper frames. Target removal is checked after the mutation batch, so an element moved within the document is still the same target. Permanent removal releases target-bound browser work without waiting for a managed disposal call, following [Blazor's DOM cleanup guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0#dom-cleanup-tasks-during-component-disposal); a later attachment comes through synchronization.

A document or browser-window subscription is released by explicit owner disposal, since no element removal marks its owner's end. A cleanup anchor beyond that is an explicit lifetime option. DOM cleanup cannot detect every disconnected or abandoned circuit.

A failed transport suspends reporting with bounded pending state. A connection interruption alone leaves the current page's JavaScript objects in place ([Blazor SignalR and reconnection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr)): a resumed circuit synchronizes the same registrations and refreshes their snapshots, and a new circuit creates new component owners. Recovery is triggered by explicit synchronization or refresh, and non-idempotent actions are never repeated on reconnect.

Disposal from inside a notification invalidates the registration immediately and schedules the remaining release after dispatch unwinds, never awaiting the notification's own completion. Native handlers are removed before callback references are released. Managed release is best-effort when the transport is unavailable; local target cleanup remains effective.

## Command outcomes and freshness

A command documents what its completion means. Focusing can confirm where focus landed. Starting smooth scrolling does not establish that the scroll finished. A download confirms that the request was issued, not that a file reached disk. Printing cannot confirm that the reader printed anything.

Each command returns the smallest typed result its consumer needs. A Boolean expresses applied or unavailable where that distinction suffices; event dispatch returns its cancellation, and sharing can report that the user canceled. Existing wrappers keep their contracts until their consumers move.

Invalid arguments and incompatible options fail validation before any mutation. Expected unavailability, browser rejection and unexpected script failure stay distinguishable wherever they change a caller's behavior or diagnosis. An unexpected failure is never turned into a successful no-op or an empty observation.

An action needing current geometry reads it in JavaScript. A managed `Current` is informational and never a precondition for a browser action. Stop-aware scrolling measures the frozen region when it executes.

Cancellation stops queued work where it can; canceling a managed wait leaves an issued action issued. A result lost to disconnection is uncertain, and an action whose result was lost is never automatically replayed.

## Rendering and DOM ownership

Blazor owns rendered structure, domain state and ordinary markup. The engine owns only the browser properties and resources its capability contracts assign to it. A capability declares its DOM writes, how they are updated, and how its ownership ends.

The engine owns, for example, native focus, scroll offsets, pointer capture, a checkbox's `indeterminate` property, object URLs for engine-owned preview sources, and a placement surface's geometry. Blazor-rendered content stays Blazor's: the engine offers no general `innerHTML` replacement or reordering of rendered children.

Continuous visual updates need a deliberate ownership boundary. A placement surface uses an engine-owned property or layer, or an explicit transient-write and managed-commit protocol, and the component renders no competing value into the same property. A managed render acknowledges a committed placement before the transient state is released, and an external placement update during a gesture follows an explicit cancel-and-apply policy.

Separating individual CSS properties is not enough on its own, because Blazor can replace an entire rendered `style` attribute. Each converted component establishes an exclusive style owner or an isolated presentation layer, and keeps the host's ordinary `Style` and `Class` working.

Observers read after the DOM changes, and engine writes invalidate the observations they affect. Measurements and writes are scheduled to avoid needless layout work, with focus recovery keeping its mutation-time timing. Equality checks and bounded resampling keep a change notification from causing an endless cycle of render, measure and write.

## Implementation boundaries

The API builds on `IJSEnvironment` and `JSModule`, adding a focused object-reference invocation helper where the roots need one, beside the existing `JSInterop` wrappers. The managed receiver dispatches private typed messages, and public callers use capability methods, so a component declares no `[JSInvokable]` method per capability.

Browser.Interop's shared code is organized by concern, with matching managed and browser implementation areas:

| Area | Contents |
| --- | --- |
| Root | `DomWatcher`, `DomEngine`, target interfaces and views |
| Observation | Source lifetime, state notification, private synchronization support |
| Scrolling | Scroll state, frozen geometry projection, commands, wheel and panning |
| Focus | Focus observations and interactions |
| Placement | Anchors, bounds, movement and resizing |
| Presentation | Native dialog and manual-popover mechanics |
| Pointer, Events, Text, Files, Media | The corresponding typed capabilities and adapters |
| Assets, Clipboard, Downloads, Printing, Sharing | The corresponding browser-wide facilities |

This organization governs shared Browser.Interop code; the rest of the repository keeps the scripting guide's behavior, operation, component and service taxonomy.

### Module structure

The composer's rich-text editor is the model. Its entry class composes concept classes, each in a `.mjs` module of its own, each taking its collaborators once in its constructor and keeping its mechanics `#private`, with listeners grouped under an `AbortController` for each owning lifetime. Observers, frames and other resources retain their own explicit cleanup. The watcher and the engine are built the same way:

- `DomWatcher.js` and `DomEngine.js` are the entries. Each composes its concept classes and holds no capability's mechanics of its own.
- Each concern folder holds one concept per `.mjs` module, as a class: a scroll boundary source, scroll commands, a wheel interaction. Cohesive stateless helpers may stay in their concern's module; the class boundary follows responsibility rather than the count of API methods.
- What one document needs once, such as the measure-then-write frame scheduler, the shared native observer pools and the delegated document listeners, is a small service module with module-level state, installed on first use and removed with its last user.
- The watcher's entry statically imports its observation core and keeps optional observation concerns lazy. The engine loads a concern's modules with a dynamic `import()` when its first command or interaction needs them, and awaits them before installing any synchronous handler. Getting a managed capability property remains local.
- Imports stay shallow and are declared statically within a loaded concern where they are always needed. The entry does not eagerly import every observation concern: observation modules are also loaded on first use. Shallow dependency graphs limit discovery delays.

The input router shows both sides. What it does well carries over: document listeners installed on the first registration and removed after the last, decisions made in the browser with no crossing, and constant tables naming its rules. Its single file, holding binding matching, native-control claims, scopes, ambient dismissal and click suppression together, is the shape these modules avoid.

Behavioral concepts are testable through their constructor's collaborators; script tests cover their contracts, and integration tests cover composition. The built-in module-loading path serves `.mjs` files with a JavaScript MIME type from the published static assets. `InputRouter` and `Virtualize` keep their paths and contracts. Product-owned editor and data-view scripts and third-party embedding adapters keep their ownership. Browser.Interop can depend on Input.Interop and never the reverse, and the watcher's JavaScript import graph excludes the input and engine modules.

Diagnostics identify the owner's `RuntimeId`, the capability, the registration generation and the operation. Small opt-in counters measure registrations, notifications and crossings, and record no text, clipboard or file contents.
