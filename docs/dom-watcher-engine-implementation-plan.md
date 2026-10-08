# Forma DOM Watcher and Engine Implementation Plan

The plan for building what [the DOM watcher and engine design](dom-watcher-engine-design.md) states, through complete consumer slices, and the record of what has landed. The capability mapping below is a destination; each capability is built with the consumer that first needs it.

## Status

No step has landed. Step 0 is next, and it proceeds independently of the rest.

## Measure of success

The first measure of success is removing `FormaFreezePane`'s custom synchronization loop and component-owned callback plumbing while preserving its behavior. Existing scroll, wheel and frame scripts already do substantial work locally and suppress redundant reports, so performance improvements are established by measurement, and the goal is less repeated component infrastructure, never more scripts or a larger framework for their own sake.

## Steps

Each step is independently reviewable. The context-lifetime change stands alone; a larger consumer slice can use several cohesive commits, with its gate met before that slice is declared complete.

0. **Context lifetime.** Open. `ComponentContext` becomes asynchronously disposable, and `FormaComponentBase` teardown disposes an already-created context after derived cleanup, as the design's component context section states. The step is confined to context ownership and disposal, with no DOM capability and no input dependency. Gate: an uncreated context stays uncreated; repeated disposal is safe; a failing derived cleanup still releases the context; concurrent creation and disposal cannot leak or recreate services; each with its own tests.
1. **The freeze pane slice.** Open. The runtime-scoped shared-import bridge, the lazy watcher and engine roots, the shared target views, the common reconciliation, one receiver, the scroll boundary and frozen-geometry observation, the step command and the wheel interaction, with `FormaFreezePane` converted in the same step. The design's crossing rules hold from this step: one cached import per used root and runtime scope, creating and attaching together after import, the baseline in the result, no call for an unchanged applied request, and one owner-resource release. Cold imports, input configuration and framework reference releases are counted separately. Gate: the quality bar, the first slice's measurements and verification below, and no foundation subsystem the slice does not use.
2. **An observation-only consumer.** Open. An existing consumer that needs only observation migrates its concern, validating the observation interfaces, module independence, and sharing where it is actually needed. The consumer is named when this step is planned in detail. Gate: no engine or input imports and no routes; disposing one subscriber leaves another running; no second callback or lifetime implementation.
3. **Focus and anchored presentation.** Open. Focus and anchored surfaces move onto typed objects over their existing algorithms. Gate: mutation-time continuity, deliberate departure, nested scopes, native editing, wrapping, RTL, placement under scrolling and resizing, and host-style ownership.
4. **Gestures, files and reordering.** Open. The gesture algorithms are reused, and panning, drops, previews and reordering migrate by concern, with typed occurrences and bounded occurrence delivery arriving here as the first consumers of them. Gate: input ownership, touch hold, cancellation, capture, one accepted result, URL cleanup and keyboard parity.
5. **Dialog, frame and window.** Open. These compose presentation, placement and focus objects and keep their managed policy and domain state. Gate: a native close raised once, frame bounds and placement, deferred raise, scrim and focus order, and `WindowManager`'s decisions unchanged.
6. **Remaining commands.** Open. Text, controls, media and browser facilities convert one at a time as consumers use them. Gate: correct capability-specific outcomes, activation requirements, resource cleanup and no automatic replay.

Every conversion step, 1 through 6, passes the quality bar below.

Steps 2 to 6 can be reordered by consumer need once the first slice lands. Each concern finishes with its old entry points removed and its documentation updated, rather than leaving retirement to a final cleanup. `InputRouter` and `Virtualize` are exclusions, not later steps.

## The quality bar for every conversion

Each converted consumer is held to the design's principles, with the specialized systems retaining their contracts:

- **An inventory before the change.** Every behavior the script being converted has today is listed, from its code, its tests and browser checks for its supported writing directions and input modes, and each listed behavior is checked again after the conversion.
- **Improvements taken and recorded.** A directly related missing edge case, redundant listener or observer, or unnecessary crossing is corrected and recorded in the step's landed note. Broader product changes are recorded as separate work with a reason, so a conversion remains reviewable.
- **Crossings counted.** The interop crossings and notifications of each defined scenario are counted before and after, and a conversion never adds one without recording why.
- **Scale checked.** Each scenario is also run with many instances on one page, and resource counts distinguish native observer instances, observed targets, listeners and managed subscriptions. Equivalent subscribers share compatible sources; distinct targets retain their necessary bookkeeping. Releasing the last subscriber returns shared resources to their baseline.
- **Modules reviewed.** Each module holds a cohesive concept and receives its collaborators explicitly. Behavioral tests cover the concept and integration tests cover composition; entry modules compose without accumulating capability mechanics. Published `.mjs` imports and cold-load requests are checked in the browser.

## The first slice

### Scope

The first slice consists of scrolling, a small watcher, the shared synchronization and failure reporting. It starts from the existing scroll measurement, equality and wheel behavior, extracting shared ownership around those proven algorithms. The freeze pane requests one combined report of its boundaries and its frozen, corner and viewport extents, and unrequested numeric scroll offsets stay in the browser.

The component keeps its rendering, CSS variables, labels, options, frozen-edge callbacks and application state. The engine owns scroll movement and the qualified wheel route, and the watcher measures. Step execution reads fresh frozen geometry in JavaScript.

The observation and the wheel interaction both use latest-request settlement, following `InputRegistration`'s requested-against-applied discipline. Its `SyncAsync` returns at once when another settle is underway; the new shared helper instead defines awaitable settlement, so every caller joins settlement of the latest known request; intermediate configurations can be superseded without being applied. `InputRegistration` itself is left unchanged in this migration.

One internal registration identifier and generation reject obsolete callbacks and late attachments. State transport is bounded per subscription, and application and transport failures are reported through the existing logger and browser diagnostics. Batching, acknowledgements, generic event descriptors, occurrence queues, command-causality tokens, reconnect epochs and a universal result hierarchy each wait for the evidence named under growing the design, below.

### Measuring the component plumbing

| Item | Freeze pane today | Required result |
| --- | --- | --- |
| Managed root-import invocations | Each used wrapper holds its own import task | One successful cached import per used root and runtime scope, shared across owners; watcher-only use imports only its root |
| Establishing the observation and optional wheel interaction | Wrapper imports and attaches, plus input-policy configuration where needed | After required imports, one attach invocation per registration; observation establishment returns its baseline. Input configuration is counted separately |
| Component-created `DotNetObjectReference` | One `_selfReference` | None |
| Component browser callback entry points | One `[JSInvokable] HandleOverflowAsync` | None; an ordinary typed callback remains |
| Custom attachment reconciliation | `_isSyncing`, `_attached`, a render-time loop and an attachment helper | None; shared registration synchronization |
| Browser wrappers | `_scroll` and `_wheel` | Typed observation and interaction handles, not necessarily fewer fields |
| Owner teardown | Explicit wrapper and reference release | One browser owner-resource release, plus separately counted framework reference bookkeeping; individual disposal remains available |
| Managed rendered state | The boundary report and a measured flag | Kept where rendering needs them |

Removed plumbing lines are recorded separately from new shared code, beside the handles retained, the actual interop calls, notifications, bytes and component renders. The same scenarios are compared before and after: initial render, midrange scrolling, boundary transitions, a wheel gesture, an option change and removal. The slice succeeds with less repeated component infrastructure and no unexplained increase in traffic. Any additional call must have a correctness or lifecycle reason recorded and reviewed at the gate; performance gains are established from measurements. Measure cold setup and warmed-root reuse separately, including actual browser module requests, and measure hot-path allocations rather than assuming zero allocation.

### Verification

| Risk | Verification |
| --- | --- |
| Initialization | Return an initial zero or false snapshot while a later callback arrives before the attach response is processed. Deliver the baseline once before later state, with bounded pending work. The callback works before the observation field is assigned. Early samples carry the managed-minted token and generation. A failed establishment holding an early sample completes that delivery and clears the barrier, and a retry then delivers. |
| Option and target races | Axis, options or target changing during setup leave only the latest desired configuration; stale callbacks cannot change the new report. |
| Late completion | Disabling or disposing during an attach releases any late object, and nothing comes back. |
| Await semantics | Concurrent callers join latest-request settlement; superseded configurations need not run. A failed attach remains unapplied and equal options can retry. Equal successfully applied options make no invocation. |
| Bounded state | With a handler held pending while samples arrive, only the protected baseline, the in-flight work and the latest state are kept, and unrelated subscriptions and commands keep working. |
| Failure visibility | A handler throwing and an interop callback rejected each produce diagnostics, bounded pending state and the documented recovery. |
| Self-disposal | Disposing from the handler completes without waiting for that handler, with no deadlock and no leaked callback reference. |
| Target cleanup | Removing the target during an interaction, without managed teardown, releases its browser resources; moving it within the document does not count as removal. |
| Functional parity | Inline and block axes, RTL, wheel enabling and disabling, action remapping, stop stepping, native scrolling, arrow boundaries, frozen and corner extents, and frozen-edge resizing. |
| Rendering ownership | Ordinary renders and the host's `Style` and `Class` keep their behavior; watcher measurements take no ownership of component-rendered CSS variables. |
| Independence | A watcher-only consumer imports no engine or input code, creates no input registration, and observes successfully. |
| Recovery | Known-inactive state retries on sync. Explicit refresh restores browser-side suspended reporting even when managed options are equal, without replaying commands. Retained circuits reuse objects and new owners attach normally. |
| Import scope | Concurrent owners share one in-flight import per root; watcher-only use leaves the engine unloaded. Check Server and WebAssembly lifetimes, prerender deferral, transient import failure, owner-specific diagnostics and disposal during import. |
| Owner release | Several settled registrations release in one owner-resource call. Disposal racing attachment rejects stale results and releases late resources, counting any compensating calls. Disposing one owner preserves another's session and shared imports. Count object-reference bookkeeping separately. |
| Loading and allocation | Direct commands load their capability without requiring an interaction first. Published `.mjs` assets load with the correct MIME type; cold-fetch counts and hot-path allocations are measured. |

Lifecycle and script tests and component tests cover the contracts, and real browsers cover layout, routing, cleanup and RTL. The repository's build gate applies to the implemented code.

### Acceptance

The slice is accepted when the freeze pane keeps its behavior on the shared objects, with no component-owned callback reference, `[JSInvokable]` bridge method or custom attachment loop; it may keep two capability handles. Watcher-only loading, initial state, latest-request synchronization, bounded delivery, visible failures and safe disposal work in the slice, and input routing, RTL, stop-aware scrolling, frozen extents and component-owned styles keep their behavior.

## Growing the design

Each of these is added when its evidence or first consumer arrives.

| Addition | Evidence or first consumer |
| --- | --- |
| Additional observer pools | The first scroll slice shares the native sources it uses across compatible requests, including several test consumers. New capability pools arrive with their own consumers, with one subscriber's disposal shown to leave another's source running. |
| Cross-subscription batching | Counters showing channel overhead worth addressing, kept within per-subscription timing and bounds. |
| Typed ordered occurrences | A dialog close, a drop or a completed reorder, with queue capacity, overflow failure and disconnection limits defined. |
| Additional public statuses or results | A real caller that branches differently on them. |
| Automatic reconnect integration | A retained-circuit consumer that needs recovery without another render or sync, with the host hook and browser-object identity verified. |
| Protocol epochs | An owner or session replacement that could confuse messages despite the registration lifetime guards. |
| A persistent capability binding | Consumers that hold a capability across renders and retarget it repeatedly, where obtaining it through `GetElement` each time proves awkward. The binding is a persistent, retargetable reference to a capability over the same implementation and registration lifetime, extending to commands the retargeting registrations already have through `SyncAsync`. |
| Sharing the settling helper with `InputRegistration` | The DOM registrations having demonstrated awaitable settlement. It changes observable behavior, since overlapping calls would then await settlement, and takes a review of its own. |

## Migrating folders

The scripting guide is updated before the first slice to state the concern-based Browser.Interop organization and its named exceptions: `components/Virtualize.js`, the input router's paths, product scripts and third-party adapters. The Scrolling and Observation areas are documented first, and later concern folders arrive with their consumers.

Each concern moves with its consumers and removes its unused old entries before the next concern starts. A temporary compatibility wrapper forwards to the one implementation and carries a named removal gate, so no algorithm exists in two copies.

Before any conversion, the DOM properties it writes are listed beside the component's renders of them, and exclusive ownership, an isolated layer, or an explicit transient-write and managed-commit protocol is settled. A conversion with unresolved competing style ownership, including replacement of the whole `style` attribute, is not accepted.

The browser interop README, the component foundations design and the repository vocabulary are updated with the API as each step implements it. Progress stays in this plan and stable responsibilities in the design.

## Repository assessment

The survey of the code this plan rests on covered the browser scripts and their wrappers, the interop foundations, the input registration and router, and representative consumers: the frame, window, dialog, freeze pane and virtualizer.

| Finding | Consequence |
| --- | --- |
| `JSInterop` already centralizes attaching, disposal and invocation, and `JSModule` holds an import task per instance. | The transport foundation is reused. The browser caches module loading independently of managed calls. Each used wrapper instance currently initiates its own import invocation; the bridge shares a successful import and in-flight attempt per used root and runtime scope. |
| `InputRegistration` already reconciles the latest requested registration through one settling loop. | Its lifetime discipline is generalized in the new registrations, and its contract stays intact. |
| `Frame.js` already applies movement and resizing locally and reports placement when a gesture completes. | It becomes a reusable placement capability; it already makes no callback per pointer movement. |
| `Scroll.js` already composes boundary and sentinel observers, schedules measurements and suppresses equal reports. | Those mechanisms become requested watcher projections, for reuse and clearer ownership. |
| `Wheel.js` already separates direct scrolling from reporting a direction. | Direct browser execution is kept, and managed reporting becomes an explicitly requested output. |
| `FormaFreezePane` manages scroll and wheel wrappers, a callback reference and a custom synchronization loop. | It is the first complete consumer of the object model and the shared registration lifetime. |
| `Focus.js` combines local focus recovery, roving state, traversal and semantic leave reporting. | Its capabilities are extracted with the event-order logic and browser-local recovery kept. |
| `Window.js` defers raising a pressed window and remembers focus. | Deferred top-layer writes and focus memory move into presentation and focus objects. |
| `DragGesture.js` already owns threshold, hold, capture, edge scrolling and an active input registration. | Its algorithm sits behind a shared gesture object, with its raw-listener path reconciled through the router API when it migrates. |
| `Virtualize.js` reports changed viewport measurements, and C# may find the rendered range unchanged. | That is a separate potential optimization, excluded from this work. |
| The composer's rich-text editor composes concept classes in `.mjs` modules behind one entry, each taking its collaborators once, with listener lifetimes released through `AbortController`. | It is the model for the watcher's and the engine's module structure. |
| `InputRouter.js` installs document listeners on first registration, decides in the browser, and names its rules in constant tables, with several routing concerns in one entry file. | Those practices carry over; its single-file shape is the one the new modules avoid. |
| `Owner.js` discards callback rejections. | The new notification transport surfaces failures and bounds pending work; legacy reporting is left as it is. |
| `Embedding.js` holds named third-party SDK integrations. | Those integrations stay outside the generic DOM engine and may consume the asset object. |

## Capability mapping

Existing names remain until each replacement's behavior is verified and its API reviewed.

| Existing browser module | Destination | Behavior that must survive |
| --- | --- | --- |
| `AnchoredPopover.js` | Presentation, anchored placement and an optional focus registration | Anchor alignment, flip and clamp, scrolling ancestors, focus return without unwanted scrolling |
| `AutofillTracker.js` | A text observation adapter and an opt-in presentation action | The stylesheet animation signal and class behavior |
| `DragGesture.js` | A pointer gesture registration | Threshold, touch hold, capture, click claiming, cancellation, active scope, edge auto-scroll |
| `DropZone.js` | A drop registration | Accepted types, effect, nested-target lookup, depth tracking, single owner |
| `FileDropZone.js` | A file-drop composition | Assigning accepted files to the enabled input and dispatching one change event |
| `FilePreview.js` | A file preview registration | Selection matching, an engine-owned image source, object URL cleanup after load, error, replacement or disposal |
| `Focus.js` | Focus observations and focus registrations | Roving, wrapping, arranged traversal, successor timing, leave semantics |
| `PanGesture.js` | A scroll panning registration | Mouse-specific behavior, the allowed-state condition, local scroll movement |
| `ReorderInteraction.js` | A reorder registration over a gesture and the router | Pointer and keyboard parity, nested list boundaries, one resulting move request; C# changes the item order |
| `Scroll.js` | Watcher scroll and intersection, and engine scrolling | Boundary equality, frozen-region measurement, sentinel root and margin, RTL, stop-aware stepping |
| `Wheel.js` | A scroll wheel registration or a direction occurrence subscription | Qualified actions and trackpad gesture grouping |
| `Dialog.js` | A native dialog presentation object | Native modality, focus target, scrim behavior, native close integration |
| `Frame.js` | A placement registration | Own or presenter box, movement, resizing, limits, bounds, final placement, keyboard parity |
| `Window.js` | Top-layer presentation and focus memory | Scrim order, presented state, deferred raise, focus restoration, no focus on raise alone |
| `Virtualize.js` | Kept as it is | Its specialized measurement, rendering and scroll contract |
| `Asset.js` | An engine asset object | Document-wide load deduplication, failed-load retry, sprite host recreation and symbol deduplication |
| `CheckBox.js` | An element checkbox capability | The native `indeterminate` property, separate from checked and validation state |
| `Clipboard.js` | An engine clipboard object | Native permission and activation rules, and the actual write result |
| `Download.js` | An engine download object | File name and media type, temporary URL lifetime, request completion semantics |
| `Embedding.js` | A dedicated integration adapter | Third-party rescan logic, explicit and outside the generic DOM contracts |
| `Playback.js` | An element media capability | Distinguishable muting and playback results, and callers' behavior during conversion |
| `Pointer.js` | A pointer capability | Conditional release and the correct pointer identity |
| `Print.js` | An engine printing object | Page and markup modes, styles, frame lifetime, completion limits, and the sanitization boundary |
| `Share.js` | An engine native sharing object | Capability availability and a canceled or rejected share outcome |
| `TextEntry.js` | An element text capability | Read and clear, native selection ranges, no implicit input event |
| `InputRouter.js` | Kept as it is | Input precedence, native control ownership, remapping, active scopes, the query API |

Frozen-region measurement is a named geometry projection composed with scrolling, requested only by the scroll subscriptions that need it. Engine-only consumers use that projection locally, and C# receives the extents only where it needs them for rendered layout.

Reordering and anchored placement remain real algorithms in their own classes. Bringing them into the object model gives them shared access, composition and lifetime around their logic.
