# Forma DOM Watcher and Engine Design

`DomEngine` and `DomWatcher` are the bridge between C# components and the browser. A component goes through them whenever it calls a browser function, asks how the document looks, listens for something that happened, or establishes behavior that runs on in the browser: it says "do this", "tell me the current state" or "let me know when this happens", whichever capability carries out the work. Shared imports, registrations, callbacks and resource management sit beneath that one model, so no component repeats them.

The [implementation plan](dom-watcher-engine-implementation-plan.md) holds the delivery order, the measurements and the gates.

## Scope

| Decision | Contract |
| --- | --- |
| One bridge | Every browser need of a component, input routing and virtualization included, goes through the engine and the watcher; no capability keeps a component-facing interop wrapper of its own. |
| Two entry points | `DomWatcher` reads, watches and listens; `DomEngine` acts and establishes behavior, and borrows the watcher. |
| One request model | A component names its targets and the facts it needs of them, and one snapshot answers, read once or watched. |
| Typed requests | Facts, occurrences, actions and behaviors are typed, and so are their results; nothing dispatches on strings, and no single method takes every request. |
| Independent watcher | Watcher-only use loads neither the engine's modules nor input, and configures no input. |
| Shared lifetime | Watches and behaviors reconcile requested state against successfully applied state. |
| One notification channel | One callback receiver per owner delivers snapshots and occurrences and surfaces failures. |
| Managed ownership | Blazor keeps rendering, application state, collections, validation and workspace policy. |
| Packaging | The API lives in `Forma.Framework.Browser.Interop`, served from `_content/forma/browser`; `Forma.Framework.Browser.Interop.Server` connects a Server circuit's reconnection to it. |

The watcher and the engine provide concrete browser capabilities through that model. Each capability arrives with a consumer that needs it.

## Principles

The watcher and the engine hold to these principles.

- **At least as good as today.** A conversion preserves the existing behavior contract and takes directly related improvements: a missed edge case, a redundant listener, a needless crossing. Deliberate behavior corrections are documented and verified alongside the preserved behavior.
- **Fewest round trips.** Work that can finish in the browser finishes there, and each crossing carries the related work its timing and contract allow, as the section on crossing sets out.
- **Built to scale.** Resource use follows active targets and requested capabilities. Compatible native observers are pooled, document listeners are shared where event semantics allow delegation, and unused capabilities install nothing. Pooling reduces observer instances; each active target still requires bookkeeping.
- **Current platform, current language.** The code targets the repository's supported evergreen browsers and .NET version, using ES modules, classes with `#private` members and `AbortSignal` lifetimes without a polyfill or a JavaScript bundling/transpilation step. Optional browser facilities report capability availability. Hot paths avoid redundant allocations, and allocation measurements include serialization and immutable notification records.
- **Small modules, one concept each.** Every concept is a class in a module of its own, taking its collaborators once, and entry modules compose them. No module becomes the place concerns accumulate.

## Responsibilities and dependencies

A **target** is a live element, the document or the browser window. A **fact** is one piece of browser state about a target, such as an element's scroll boundaries or whether the reader is within it. A **query** names targets and the facts it needs of each, and a **snapshot** is the browser's answer, its facts captured together. A **watch** keeps a query running, delivering a first snapshot and each changed one after it. An **occurrence** is something that happened, such as an input action or an accepted drop. An **action** is a request the browser carries out once, returning its result, and a **behavior** is one that runs on in the browser until it changes or is released. A **capability** is an area, such as scrolling or focus, contributing its facts, occurrences, actions and behaviors to the one model.

`DomWatcher` reads and reports: queries, watches and occurrence subscriptions. `DomEngine` moves focus, writes the DOM properties its capabilities own through explicit contracts, runs actions and establishes behaviors. Browser work that needs immediate handling, such as routing input, canceling a native event or moving during a gesture, stays in the browser, and C# hears only the results, snapshots and occurrences it asked for. The engine consumes watcher state directly in JavaScript where no managed report is needed.

`InputRouter` remains the browser's authority over input ownership, as the [input router design](input-router-design.md) defines it. Components establish routes through the engine and receive the actions they asked for as occurrences through the watcher, and the router's component-facing registration moves behind them. The virtualizer keeps its measurement, rendered-range, pinning and scrolling algorithms, as the [virtualization design](virtualization-design.md) states them; components configure its browser work through the engine, and read and watch what it measures through the watcher.

## Public object model

### Targets and facts

A target is an element, the document or the browser window. A fact is a typed descriptor of something a component needs to know about a target, carrying the options that shape it, such as the axis scroll boundaries are measured along, and naming the type of its value. Each capability contributes its facts, and a component composes any of them, across capabilities and targets, into one query. Composing a query makes no crossing and installs nothing.

An element target refers to that exact element; a watch moves to a replacement element only through an explicit synchronization. Targets belong to the current document, and a cross-document target is rejected. Shadow-root internals are reached only through a target given explicitly.

### Reading and watching

The same query is read once or watched. A read makes one crossing and returns one snapshot. A watch returns its first snapshot with its establishment and delivers each changed snapshot after it. A snapshot answers each fact the query named with its value or with its unavailability, so a missing value is never mistaken for zero, false or an empty string.

```csharp
// One query: the viewport's scroll boundaries and whether the reader is within it.
var query = new DomQuery();
var boundaries = query.Select(_viewport, ScrollFacts.Boundaries(ScrollAxis.Inline));
var within = query.Select(_viewport, FocusFacts.Within);

// Read it once.
var snapshot = await Context.DomWatcher.ReadAsync(query);

// Or watch it: the first snapshot, then each change.
_watch = await Context.DomWatcher.WatchAsync(query, HandleSnapshotAsync);

private Task HandleSnapshotAsync(DomSnapshot snapshot)
    => snapshot[boundaries] is { IsAvailable: true, Value: var value } ? ShowAsync(value) : Task.CompletedTask;
```

The sketch shows the shape. A selection names the type of its value, so reading it from a snapshot needs no cast and no key string.

`WatchAsync` registers its handler before browser establishment begins, and the handler reads its immutable snapshot, so it works before the field receiving the watch is assigned. A watch has a nullable `Current`, `IsActive`, `IsDisposed`, `SyncAsync`, `RefreshAsync` and `DisposeAsync`. `IsActive` means an established watch with an accepted first snapshot. `Current` is absent before establishment and is cleared on disabling, retargeting, known detachment or known loss of reporting. These are local facts; they make no synchronous claim about the connection's health.

`SyncAsync` records the latest desired query and whether it should run, and behaviors follow the same pattern. Call and await it after each relevant render, with the current targets. The registration handles overlapping calls and reconciles the latest request; the component needs no synchronization guard. Disabling releases native resources, and enabling again delivers a fresh first snapshot. An unavailable establishment leaves the watch inactive and retryable on the next synchronization; invalid requests and unexpected failures are surfaced.

### Occurrences

An occurrence answers what happened, and stays meaningful when the state before and after it is the same, so no snapshot replaces it. A component subscribes to the occurrences it wants, and a subscription receives only occurrences after it began. An occurrence can carry a snapshot of the facts its subscription names, captured at the moment its contract defines, such as before or after the browser action it reports, so the component never reads a later state in its place.

Occurrences keep their order and are never coalesced the way a watch's snapshots are, as the section on delivery sets out.

### Actions and behaviors

An action is a typed request the browser carries out once, with a typed result whose meaning its contract defines. A behavior is established on a target with typed options and runs on in the browser until it is synchronized to new options or released: a focus interaction, input routes, anchored placement, the virtualizer's browser work. A behavior can offer occurrences, as input routes offer the actions they matched.

```csharp
// An action and its result.
var started = await Context.DomEngine.RunAsync(_viewport, ScrollActions.Step(ScrollDirection.Forward, steps));

// A behavior, and the occurrences it offers.
_routes = await Context.DomEngine.EstablishAsync(RootElement, new InputRoutes(bindings));
_actions = await Context.DomWatcher.ListenAsync(_routes.Actions, HandleActionAsync);
```

Handlers return `Task`. Snapshot and occurrence handlers share the one notification mechanism, and adding a listener returns a disposable subscription whose lifetime is separate from its watch's or behavior's. Browser-wide facilities, such as the clipboard and downloads, are engine actions that take no element target.

## Facts and occurrences by capability

A capability installs listeners only while a read or a watch asks for its facts or a subscription for its occurrences.

| Capability | Facts and occurrences | Observation and boundaries |
| --- | --- | --- |
| Document | Visibility, document focus, readiness | Native document and browser events; visibility and focus are separate facts. |
| Browser window | Layout viewport dimensions, scroll, device pixel ratio when requested | Resize, scroll and media-query invalidation. The visual viewport's data is a fact of its own. |
| Element connection | Connected or detached | Removal checks and scoped mutation observation. A hidden element remains connected. |
| Geometry | Size, or a rectangle in a declared coordinate space | Resize observations plus the invalidation sources position requires. |
| Intersection | Intersection with a declared root and margin | Intersection observer semantics; intersecting does not mean unobscured. |
| Scroll | Logical offset, available extent, viewport extent, start and end boundaries | Scroll, size and relevant content changes. Numeric offsets are optional where the consumer needs only boundaries. |
| Focus | Focus within a surface and the relevant focus transitions | Browser-local focus identity and event sequence; a document blur is distinct from leaving the surface. |
| Text entry | Requested value, selection and supported autofill signals | Input and selection events and explicit refresh. Unrelated property assignments are not universally observable; explicit refresh supplies a fresh read when no supported signal occurs. |
| Presentation | Native dialog and popover open state, and close and toggle occurrences | Native events plus direct reads after engine actions. Workspace activation remains managed state. |
| Media | Requested playback state | Native media events and property reads. |
| Attributes or structure | An explicitly named attribute set or subtree change | Narrow mutation observations, each scoped to what its consumer names. |
| Input | The actions and dismissals an owner's routes matched, as occurrences | The input router's ownership and matching rules, decided in the browser. |
| Virtualization | The rendered range and the measurements its owner needs | The virtualizer's own measurement of what scrolls it and what it draws. |

### Meaning of a snapshot

A snapshot is what the browser measured when it answered, delivered asynchronously; it is no live DOM object and no promise that the browser has not changed since. The facts of one query are captured in one browser task, so a snapshot never mixes moments. An unavailable fact is reported as unavailable, never as a fabricated zero, empty string or false.

Each fact has a documented unit and meaning. Geometry uses CSS pixels; text offsets use the native text-entry selection units. Client rectangles are relative to the layout viewport; document coordinates and visual viewport offsets are separate values. Logical scrolling normalizes direction, while geometry keeps explicitly physical coordinates. The existing `ScrollAxis` and start and end vocabulary carry over.

A watch reports a snapshot when one of its facts changes. Scroll boundaries compare their boundary values, so movement through the middle alone reports nothing; geometry compares its measurements within an explicit tolerance; document and Boolean facts compare exactly. JavaScript evaluates only the facts a query names.

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
- **Create and attach together.** Once its required root is imported, a registration call creates the owner's browser session if needed and attaches that registration. Subsequent registrations reuse it. Concurrent first requests share session creation, and a standalone action or read can create the session on first use as well. Required input-policy configuration remains visible in the crossing count.
- **The first snapshot in the result.** A read is one invocation returning its snapshot. A watch's establishment returns its first snapshot with the attach result, and the managed side delivers the first notification from it, with the ordering barrier described below, saving a separate initial callback.
- **No call for an unchanged, applied request.** `SyncAsync` compares target and options against successfully applied state. An equal request still retries inactive or known-invalid state. `RefreshAsync` explicitly requests a browser read and restores suspended reporting even when options are equal; local equality cannot prove browser health.
- **One snapshot per query.** The facts a component needs together travel together, as one query's snapshot.
- **Local interaction.** Input routes, gestures, panning, wheel movement, button-requested actions such as a scroll step, and placement run in the browser, and C# hears only the outcomes it requested. A local action makes no round trip of its own; a state change it causes still reaches C# as a notification.
- **One owner-resource release in ordinary teardown.** Context teardown marks all registrations disposed locally, then makes one browser call to release the owner's resources, engine before watcher. It uses this aggregate path rather than calling each child's public disposal method. An individual registration or borrowed engine still supports independent release. Compensating releases for late setup results and framework object-reference disposal calls are counted separately; correctness takes precedence over the ordinary-path call count.

Private registration tokens identify browser-owned objects under the owner session; a separate managed JavaScript object reference per registration is unnecessary. The bridge's dispatch surface remains private and finite, and the public API remains typed requests.

Interop crossings are counted per scenario by the opt-in diagnostics. Counts distinguish cold initialization, warmed-root registration, input-policy setup, ordinary synchronization, notifications and teardown.

## Notification and timing

### Initial state and ordering

After capability loading, establishment installs listeners and captures a baseline in one browser task. The managed observation exists before that work starts. The managed side mints the registration's token and generation before the call and passes them with the attach request; the browser stamps every sample with them, and the attach result returns the baseline under the same identity. The managed receiver holds any early sample behind an establishment barrier, retaining only the latest pending state until that result is accepted. It installs `Current` and queues the initial notification before releasing later samples. A late result for an obsolete generation is discarded and its browser registration released.

The browser attach path never awaits a managed notification, and the barrier opens before application handlers run. Establishment completes once the baseline is accepted, independently of callback completion. A failed establishment cancels its barrier, completes any delivery it was holding as discarded, so the browser's in-flight slot is free for a retry, and clears pending samples. This orders the returned baseline and asynchronous callbacks without a separate acknowledgement call.

The browser sends a registration's first sample no sooner than a task after its attach returns, folding anything measured meanwhile into one latest sample. That makes an early sample rare on Blazor Server, but the barrier does not depend on it: a failed attachment, a disposal and an obsolete generation each release any delivery the barrier holds, without deadlock.

Each registration has an internal identifier and a configuration generation. Replacing or disabling a target invalidates the previous generation immediately, and messages from obsolete generations are rejected. Delivery within a generation is serialized; a sequence number is added only where the transport needs one to enforce that order, and stays private.

Separate watches and subscriptions carry no global order. Facts that must agree belong to one query, and an occurrence carries the snapshot it needs.

### Timing each capability preserves

| Timing | Use | Rule |
| --- | --- | --- |
| Synchronous browser handling | Input ownership, cancellation, capture, action preconditions | Runs in the browser handler without waiting for C#. |
| Mutation-time browser handling | Recovery after a focused node is removed | Runs at mutation delivery, before deferred reporting, in the browser. |
| Scheduled state | Geometry and scroll measurements | Coalesces invalidations and reports changed requested facts. |
| Ordered occurrences | Close, accepted drop, completed reorder | Preserves accepted occurrences and their order while delivery is healthy. |

These are constraints on each capability's implementation. Local watcher subscriptions keep the timing their consumers require; managed observations are asynchronous snapshots.

### Bounded delivery and visible failure

The owner has one managed callback reference and one typed receiver, shared by watcher and engine notifications. Subscriptions dispatch independently, and no lock is held across application code.

Each watch keeps one delivery in flight and one latest pending snapshot, with the first snapshot protected. The interop invocation can stay outstanding until that watch's handler work completes; establishment, actions and unrelated subscriptions proceed meanwhile. Several listeners on one watch are awaited deliberately, each failure isolated and logged.

A rejected application handler is reported with the owner, capability and registration identity. A transport failure is reported locally in the browser and suspends managed delivery; bounded state is retained, and delivery resumes through explicit resynchronization. Every failure is reported: a shared helper never swallows a rejection the way `.catch(() => {})` does, and failure reporting never runs through the channel that failed.

Occurrences are delivered through a bounded ordered queue per consumer, with an explicit failure on overflow, and are never coalesced into state. Ordering applies while delivery is healthy; an occurrence whose delivery becomes uncertain on disconnection is not automatically replayed. State recovery uses a new snapshot, while occurrence recovery follows the capability's contract.

### Actions and snapshots

An action can invalidate a watch, and the watcher publishes the measured resulting snapshot with its normal comparisons. An action never fabricates a snapshot.

## Actions and behaviors by capability

The engine runs each capability's actions and establishes its behaviors through concern-specific objects, each composing ordinary classes with typed options and collaborators.

| Capability | Actions and behaviors |
| --- | --- |
| Input routing | Routes, the input policy they follow, and dismissal |
| Virtualization | The rendered range's browser work, and scrolling to an offset or an item |
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

Scrolling shows the model whole: its boundaries and frozen extents are facts, a step is an action, and its wheel route and step buttons are a behavior. Input policy is configured lazily before a qualified route attaches, movement stays local, and the component receives only the facts it asked for.

A component reaches the browser only through the engine and the watcher; a component's own script becomes the implementation of the actions and behaviors its component requests. Inside the browser, a capability's implementation composes its area's classes, and code implementing another capability may compose them too, owning and disposing the listeners and observers it creates. Capability loading is awaited before synchronous input handlers are installed, so such handlers call already-loaded local methods.

Event dispatch distinguishes native event constructors from custom JSON detail. It reports synchronous cancellation, which is complete when dispatch returns; asynchronous managed handlers run later. Synthetic dispatch creates no trusted input and grants no user activation ([dispatch semantics](https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/dispatchEvent), [user activation](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/User_activation)). Setting a property and dispatching an event are separate actions: the file-drop composition assigns the accepted files and then raises its one change event.

## Component browser mechanics

The browser mechanics of dialogs, frames and windows become compositions of the shared objects; their Blazor components and domain decisions remain.

### Native dialogs

A native dialog element uses the engine's native dialog presentation object and the watcher's close and open observation. Native modality and the dialog element remain intact. Input dismissal arrives as an occurrence of the dialog's input routes.

The component owns its `Opened` parameter, its confirmation and cancellation callbacks, and its close policy. A browser close can originate from a routed request, a dialog form or native behavior; these paths converge on one component close transition, so `OnClose` is raised once. The engine never interprets a native close as approval of an application-level confirmation.

### Frames

A frame uses a placement registration over an explicitly designated box, which may be its own element or its presenter's element. Options supply the drag region, resize handles, bounds, limits, keyboard actions and visual markers, so the framework names no Forma component class.

Movement and resizing stay local through a gesture, with a placement result at completion. A bounds change can also report a corrected placement. Gesture cancellation retains and reports the position reached, as the frame does today; a rollback policy is a separate API decision.

The component retains `Placement`, `Maximized`, `Locked`, its disabled state, handles, labels and callbacks. Frame geometry carries no workspace identity or stacking policy. Its arrow keys are input routes established through the engine.

### Windows

A window composes manual-popover presentation, optional scrim pairing, focus memory, and the frame's placement registration. `WindowManager` remains the authority over activation, ordering, parentage, minimization, blocking and close approval, as the [window design](window-design.md) states.

The engine reflects the order the manager supplies; activation and the logical window stack remain the manager's. Raising a surface preserves pointer capture: a hide-and-show that would interrupt a press is deferred until release, cancellation or browser-window blur. Raising a surface leaves the focus where it is.

Native dialogs and manual popovers are separate presentation modes, each with its own contract.

### Focus continuity

Focus recovery uses the watcher locally at mutation time. The owner chooses the successor and marks it in the same render that removes the focused node. Recovery proceeds only when the old node has disappeared and focus has fallen away, so focus the reader moved elsewhere stays where they put it.

Focus memory, Tab wrapping, arranged regions, roving traversal and recovery remain distinct options of the focus interaction, each selectable alone or with the others. Native controls, nested scopes, disabled elements, reading direction and the router's ownership rules keep their behavior. Recovery stays in the browser, because a focus-change callback sent to C# would lose the timing it needs.

Focus's within fact reports whether the reader is within a surface. Focus inside the surface's DOM subtree is within, a popover rendered inside it included, and so is focus that goes away with an element a render removes, that leaves the browser window, or that a press inside the surface drops to the page. Focus leaving is a reported change from within to not within; a first state of not within is no leave. Snapshots coalesce as every watch's do: a leave and a return before the next report produce no reported change, while a return after not within was reported is a change of its own.

## Identity, ownership and lifetime

### Ownership

An owner has one watcher and an optional engine, sharing target identity, diagnostics and notification transport. The engine borrows the watcher: disposing the engine leaves the watcher, and disposing the watcher first ends dependent engine use. Each lifetime belongs to whoever created the object.

A private bridge is registered with `TryAddScoped` through the browser interop registrar. Its lifetime follows the interactive `IJSRuntime`: a circuit on Server and the application's service scope on WebAssembly, as described by [Blazor dependency injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection). Each root has its own lazy import; an owner borrows those imports and owns its own session, callbacks, runtime identity and diagnostics. Sharing imports does not retain the first component's environment or logger as the identity of every subsequent owner.

Prerendering creates no browser session and performs no import. Concurrent requests share an in-flight import. A rejected managed import attempt is cleared for a later explicit retry; unexpected module evaluation failures remain visible and are not retried in a loop. Disposing the bridge prevents new imports and releases any reference arriving from a late import. Owners never dispose shared module references. Application-resolved watchers use the registered bridge. Directly constructed watchers own a private bridge; only the registered construction path promises import reuse between owners.

Targets are cheap managed views. Watches, subscriptions and behaviors own resource leases and release them independently. Private identifiers are tokens, never HTML ids. The bridge and its owners use the same interactive runtime scope; process-wide state never captures a Server circuit's `IJSRuntime`. Independent use constructs a watcher from an `IJSEnvironment`; the caller owns it and any engine it creates.

### Component context

`IComponentContext` exposes lazy `DomWatcher` and `DomEngine` properties. `ComponentContext` owns the objects it creates, using its existing JavaScript environment, runtime identity and logger. Constructing the engine configures no input; routed capabilities resolve the existing input collaborator when they first need it.

`FormaComponentBase.DisposeAsync` runs derived cleanup, then disposes an already-created context in a `finally` block, creating no context during teardown. Cleanup is idempotent, attempts every owned release even when one fails, and creates nothing after disposal. The context uses the aggregate owner-resource release: engine resources are released before watcher resources within that call. Late setup results are invalidated and released as they arrive. Shared imports remain owned by the bridge.

### Synchronization and racing renders

Every watch and behavior separates desired state from successfully applied state. `SyncAsync` records the latest target and value-compared options, and one settling loop applies changes until the latest request is settled, following the discipline `InputRegistration` established.

A request arriving during an interop call remains pending. A late attachment made obsolete by replacement or disposal is released. Failed setup is never recorded as applied. Every caller joins the active settling task. Completion means the loop has processed the latest request known at settlement; intermediate requests can be superseded without being applied. A Boolean establishment result, where one is needed, describes current application, not proof that each caller's original configuration ran. A later call can begin a new settlement. The existing `InputRegistration` keeps its current await semantics; the new helper applies to DOM registrations.

Configuration is latest-wins; actions are each kept. Two explicit dispatches or downloads remain two requests, invocation order within a capability is preserved, and asynchronous completion is documented separately. Cancellation or supersession happens only where a method's contract defines it.

Generation invalidation guards every registration, because setup, parameter changes and disposal race. It is a private guard. Disposal invalidates queued work immediately, and a disposed registration never comes back.

### Cleanup and reconnection

Registrations release their listeners, observers, scheduled frames, gestures, temporary URLs and helper frames. Disposal and retargeting release registrations. Browser-side removal detection provides additional cleanup when a resize notification exposes that a target has left the document, following [Blazor's DOM cleanup guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0#dom-cleanup-tasks-during-component-disposal): the target is checked a frame after the notification, so an element moved within the document is still the same target. It does not guarantee detection of every removal; a target whose observed box was already zero-sized reports no change as it leaves. A later attachment comes through synchronization.

A document or browser-window subscription is released by explicit owner disposal, since no element removal marks its owner's end. A cleanup anchor beyond that is an explicit lifetime option. DOM cleanup cannot detect every disconnected or abandoned circuit.

A failed transport suspends reporting with bounded pending state. A connection interruption alone leaves the current page's JavaScript objects in place ([Blazor SignalR and reconnection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr)): a resumed circuit refreshes the same registrations, and a new circuit creates new component owners. On Server, when a circuit's connection is restored, its bridge asks the browser in one call to measure afresh and deliver anew each established observation of the sessions that reached the browser through it, naming each by owner, token and current generation. A state delivered while the connection was down is replaced, even where the new measurement equals it, and a released or replaced registration is not revived. The hook belongs to the Server integration, and the bridge keeps the sessions and the refresh. The hook starts the refresh and never awaits the browser: a circuit handles one invocation from the browser at a time, so a reply awaited while the connection is coming up would queue behind the very invocation awaiting it. A watcher with a bridge of its own recovers through `RefreshAsync`. Non-idempotent actions are never repeated on reconnect.

Disposal from inside a notification invalidates the registration immediately and schedules the remaining release after dispatch unwinds, never awaiting the notification's own completion. Native handlers are removed before callback references are released. Managed release is best-effort when the transport is unavailable; local target cleanup remains effective.

## Action outcomes and freshness

An action documents what its completion means. Focusing can confirm where focus landed. Starting smooth scrolling does not establish that the scroll finished. A download confirms that the request was issued, not that a file reached disk. Printing cannot confirm that the reader printed anything.

Each action returns the smallest typed result its consumer needs. A Boolean expresses applied or unavailable where that distinction suffices; event dispatch returns its cancellation, and sharing can report that the user canceled. Existing wrappers keep their contracts until their consumers move.

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

The API builds on `IJSEnvironment` and `JSModule`, adding a focused object-reference invocation helper where the roots need one, beside the existing `JSInterop` wrappers. The managed receiver dispatches private typed messages, and public callers make typed requests, so a component declares no `[JSInvokable]` method and holds no callback reference.

Browser.Interop's shared code keeps its foundation at the root and gives each capability area a folder of its own, with matching managed and browser implementation areas. A folder exists for an area a reader recognizes, never for a mechanism the root's types share:

| Area | Contents |
| --- | --- |
| Root | `DomWatcher`, `DomEngine`, targets, queries, snapshots and occurrence subscriptions, the registrations, sessions and delivery they share, and the clipboard, downloads, printing and sharing |
| Input | Input routes and the occurrences they offer, over the input router |
| Scrolling | Scroll state, intersection with a root and margin, frozen geometry, stepping, wheel movement and panning |
| Virtualization | The virtualizer's measurement, rendered range and scrolling contract |
| Focus | Focus facts, actions and behaviors |
| Placement | Anchors, bounds, movement and resizing |
| Presentation | Native dialogs and manual popovers |
| DragDrop | Drag gestures, drops and reordering |
| Pointer | Pointer capture and release |
| Files | File previews and the temporary resources they own |
| Forms | The native state of form controls, such as a checkbox's indeterminate state |
| Text | Text entry: its selection, reading and clearing it, and autofill |
| Events | Native and custom event dispatch |
| Media | Playback |
| Assets | Modules, scripts, stylesheets and sprites, each loaded once per page |
| Integrations | Third-party SDK adapters, such as embedding |

This organization governs all of Browser.Interop; the rest of the repository keeps the scripting guide's behavior, operation, component and service taxonomy.

### Module structure

The composer's rich-text editor is the model. Its entry class composes concept classes, each in a `.mjs` module of its own, each taking its collaborators once in its constructor and keeping its mechanics `#private`, with listeners grouped under an `AbortController` for each owning lifetime. Observers, frames and other resources retain their own explicit cleanup. The watcher and the engine are built the same way:

- `DomWatcher.js` and `DomEngine.js` are the entries. Each composes its concept classes and holds no capability's mechanics of its own.
- Each concern folder holds one concept per `.mjs` module, as a class: a scroll container, the observer of its state, the buttons that step it. A calculation belongs to the concept whose state it reads, as a member, rather than to a class of its own; the class boundary follows responsibility rather than the count of API methods.
- What one document needs once, such as the measure-then-write frame scheduler, the shared native observer pools and the delegated document listeners, is a small service module with module-level state, installed on first use and removed with its last user.
- The watcher's entry statically imports its observation core and keeps optional observation concerns lazy. The engine loads a concern's modules with a dynamic `import()` when its first action or behavior needs them, and awaits them before installing any synchronous handler. Getting a managed capability property remains local.
- Imports stay shallow and are declared statically within a loaded concern where they are always needed. The entry does not eagerly import every observation concern: observation modules are also loaded on first use. Shallow dependency graphs limit discovery delays.

The input router shows both sides. What it does well carries over: document listeners installed on the first registration and removed after the last, decisions made in the browser with no crossing, and constant tables naming its rules. Its single file, holding binding matching, native-control claims, scopes, ambient dismissal and click suppression together, is the shape these modules avoid.

Behavioral concepts are testable through their constructor's collaborators; script tests cover their contracts, and integration tests cover composition. The built-in module-loading path serves `.mjs` files with a JavaScript MIME type from the published static assets. The input router's and the virtualizer's browser implementations keep their algorithms, and their component-facing contracts move behind the engine and the watcher. Product-owned editor and data-view scripts keep their ownership, and third-party embedding adapters stay outside the generic DOM contracts in the Integrations area. Browser.Interop can depend on Input.Interop and never the reverse, and the watcher's JavaScript import graph excludes the input and engine modules.

Diagnostics identify the owner's `RuntimeId`, the capability, the registration generation and the operation. Small opt-in counters measure registrations, notifications and crossings, and record no text, clipboard or file contents.
