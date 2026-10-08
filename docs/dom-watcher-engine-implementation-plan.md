# Forma DOM Watcher and Engine Implementation Plan

The plan for building what [the DOM watcher and engine design](dom-watcher-engine-design.md) states, through complete consumer slices, and the record of what has landed. The capability mapping below is a destination; each capability is built with the consumer that first needs it.

## Status

Steps 0 to 3 have landed: `FormaFreezePane` runs on the watcher and the engine, with its crossings measured before and after under the first slice; the explorer's and the emoji picker's sentinels are intersection observations; and the browser session owns what an owner's watcher and engine share. Step 4, focus and anchored presentation, is next.

## Measure of success

The first measure of success is removing `FormaFreezePane`'s custom synchronization loop and component-owned callback plumbing while preserving its behavior. Existing scroll, wheel and frame scripts already do substantial work locally and suppress redundant reports, so performance improvements are established by measurement, and the goal is less repeated component infrastructure, never more scripts or a larger framework for their own sake.

## Steps

Each step is independently reviewable. The context-lifetime change stands alone; a larger consumer slice can use several cohesive commits, with its gate met before that slice is declared complete.

0. **Context lifetime.** ✅ `ComponentContext` becomes asynchronously disposable, and `FormaComponentBase` teardown disposes an already-created context after derived cleanup, as the design's component context section states. The step is confined to context ownership and disposal, with no DOM capability and no input dependency. Gate: teardown creates no context; repeated disposal is safe; a failing derived cleanup still releases the context; each with its own tests. The checks that need resources the context owns, the watcher and the engine, are the deferred context checks of step 1. Landed: `ComponentContext` is asynchronously disposable and says whether it has been disposed, and both component bases release an already-created context after their own cleanup, in a `finally` so a failing cleanup still releases it; the input base also runs the framework's own cleanup when the component's fails. Tests cover the three checks for both bases and the context's repeated disposal.
1. **The freeze pane slice.** ✅ The runtime-scoped shared-import bridge, the lazy watcher and engine roots, the shared target views, the common reconciliation, one receiver, the scroll boundary and frozen-geometry observation, the step command and the wheel interaction, with `FormaFreezePane` converted in the same step. The design's crossing rules hold from this step: one cached import per used root and runtime scope, creating and attaching together after import, the baseline in the result, no call for an unchanged applied request, and one owner-resource release. Cold imports, input configuration and framework reference releases are counted separately. The arrows step in the browser through the input router's click handling, as the first slice's scope states. Gate: the quality bar, the first slice's measurements and verification below, the deferred context checks below, and no foundation subsystem the slice does not use. Landed: the bridge, the watcher and engine with their element views, the shared registration with its generations and establishment barrier, the receiver, the scroll observation, the scroll interaction with its wheel route and step buttons, the step command and the context's single release, with `FormaFreezePane` converted and its callback reference, `[JSInvokable]` method, synchronization loop and arrow click handler gone. The script inventory moved with it: the boundary measurement, the frozen-region measurement, stop stepping and wheel movement are now the scroll container's, observed by its state observer and moved by its interaction, and `Scroll.js` and `Wheel.js` keep only the sentinel, centering and the wheel direction their remaining consumers use. Improvements taken: a pane reports its viewport extent only while its frozen edge is drawn, and draws the edge once that extent has arrived; one resize observer serves every observation in the document; each watcher keeps one element per native element its owner registers on, and a rendered element that leaves the document releases the registrations held on it, leaving their tokens free for a later generation; and `FormaButton` renders no click handler while it has no `OnClick`, so a click nothing handles stays in the browser. Departures from the design:
   - An observation is `DomObservation<TOptions, TState>` and an interaction `DomInteraction<TOptions>`, so `SyncAsync` takes its capability's options typed.
   - The router resolves a click on a native button as the button's own activation before any enclosing scope sees it, so each arrow is its own router scope. The engine finds the arrows among the pane's children by selector and follows them as they are rendered and removed, with no crossing.
   - `DomBridge` and `DomReceiver` are public types with internal surfaces: dependency injection constructs the bridge, which also releases its managed state synchronously for a scope disposed that way, and the receiver's notification is the browser's entry point. `JSModule` exposes its shared import for the bridge to hold one per entry.
   - A disposed context refuses its watcher and engine with `ObjectDisposedException`, so nothing is made after disposal.
   - The engine runs a capability's commands through one entry, which loads the concern and calls the named command.

   Breaking changes, while unreleased: `ScrollAxis` moved from `Behaviors` to `Scrolling`; `ScrollBoundary`, `ScrollBoundaryReport` and `ScrollStops` were removed, with `Scroll.StepAsync`, the boundary a `Scroll` attachment could watch, a `ScrollWheel` that moves a track, and attaching a `Wheel` without an owner.

   Settled in review: a .NET call the browser makes while the circuit is down is never answered, so the notification it carried stayed in flight and every later state waited behind it. Resuming an observation's delivery now gives up on such a send, and on Server a restored connection refreshes the established observations of the circuit's bridge in one call, through `Forma.Framework.Browser.Interop.Server`, a project of its own so the circuit hook has an explicit home. The browser check found that a hook awaiting that call stalls it, so the refresh is started, not awaited. Target cleanup became the contract the design's cleanup section states, since an element whose observed box is already zero-sized reports no change as it leaves; observing the document's child list waits for a consumer and a measured cost.
2. **An observation-only consumer.** ✅ The scroll sentinel, the `SentinelObserver` over `Scroll.js` that `FormaExplorerContentArea` and `FormaEmojiPicker` used, becomes an intersection observation in the Scrolling area, validating the observation interfaces, module independence, and sharing where it is actually needed. Native intersection observers are shared by subscribers with a compatible root, margin and thresholds, which script tests demonstrate, since the two consumers observe different roots. The sentinel's implementation and wrappers are removed, and `Scroll.js` and its wrapper keep centering alone until their last consumer converts. Gate: no engine or input imports and no routes; disposing one subscriber leaves another running; no second callback or lifetime implementation. Landed: the intersection observation, whose browser side reads its first state from the element's and the root's boxes and then reports only changes, over one pool of intersection observers keyed by root, margin and threshold, with `FormaExplorerContentArea` and `FormaEmojiPicker` converted and their `SentinelObserver`, `[JSInvokable]` method and disposal gone. Each owner observes its sentinel only while it has more to load, and refreshes the observation after a load, since a sentinel still in view after the new content renders reports no change. The sentinel imports the watcher alone. Against the sentinel it replaced, measured with the same probe on the same pages: opening the emoji picker page makes a refresh for each sheet loaded once its sentinel is watched, five in all, and receives four notifications, where it made four `Scroll.js` imports and received seven reached calls; each page of the explorer's automatic infinite scroll makes one refresh and receives two notifications, where it released and reattached its sentinel, released its reference and received thirteen to fifteen reached calls. The loaded items match the old version's at each step. Departures from the design:
   - Watching a scroll state or an intersection takes whether to observe, as an interaction's attach does. The sentinels' owners create their observations inactive and start them with their first sync, so the field holding an observation is set before its first state arrives, and the load that state starts can measure it afresh.

   Breaking changes, while unreleased: `SentinelObserver`, `ScrollBehaviors`, `ScrollSentinel` and `Scroll.AttachAsync` were removed, and `Scroll` keeps `ToCenterAsync` alone.
3. **The browser session's ownership.** ✅ The browser side of an owner's session owns what its watcher and engine share. `DomSession` keeps the owner's elements and makes its watcher, and takes its engine's class from the engine's entry, so an owner that only observes loads no engine; the watcher and the engine receive the elements through their constructors, the engine no longer reaches them through the watcher, and nothing outside the session assigns its parts. `DomWatcher.js` and `DomEngine.js` keep the surface their C# calls and route it to the session, with the classes in modules of their own. The registration counts only tests read are removed, and those tests assert on notifications and the platform's observers instead. `refreshAll` keeps naming each owner, token and generation, since only .NET knows which owners a reconnected circuit holds. Gate: releasing an observation leaves an interaction on the same element running and its element's removal still detected; releasing a session releases its engine, its watcher and then its elements; the watcher's import graph excludes the engine; no test reads a registration count or a session's parts. Landed: `DomSession` keeps the owner's elements, makes its watcher, and makes its engine from the class the engine's entry hands it; the `DomWatcher` and `DomEngine` classes live in `DomWatcher.mjs` and `DomEngine.mjs`, and the entries route each call to the session. The counts the registration table, the watcher, the engine, the session registry and both observer pools kept for tests are gone, with the registration table's unread `isDisposed`. Script tests show an interaction kept running, and its element's leaving noticed, once an observation on the same element is released, and the reverse for an engine released alone.
4. **Focus and anchored presentation.** Open. Focus and anchored surfaces move onto typed objects over their existing algorithms. Gate: mutation-time continuity, deliberate departure, nested scopes, native editing, wrapping, RTL, placement under scrolling and resizing, and host-style ownership.
5. **Gestures, files and reordering.** Open. The gesture algorithms are reused, and panning, drops, previews and reordering migrate by concern, with typed occurrences and bounded occurrence delivery arriving here as the first consumers of them. Gate: input ownership, touch hold, cancellation, capture, one accepted result, URL cleanup and keyboard parity.
6. **Dialog, frame and window.** Open. These compose presentation, placement and focus objects and keep their managed policy and domain state. Gate: a native close raised once, frame bounds and placement, deferred raise, scrim and focus order, and `WindowManager`'s decisions unchanged.
7. **Remaining commands.** Open. Text, controls, media and browser facilities convert one at a time as consumers use them. Gate: correct capability-specific outcomes, activation requirements, resource cleanup and no automatic replay.

Every conversion step, 1, 2 and 4 through 7, passes the quality bar below.

Steps 4 to 7 can be reordered by consumer need. Each concern finishes with its old entry points removed and its documentation updated, rather than leaving retirement to a final cleanup. `InputRouter` and `Virtualize` are exclusions, not later steps.

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

The component keeps its rendering, CSS variables, labels, options, frozen-edge callbacks and application state. The engine owns scroll movement, the qualified wheel route and the arrows' step, and the watcher measures. Step execution reads fresh frozen geometry in JavaScript.

An arrow requests a local action: the arrow buttons stay native buttons, and their activation reaches the step through the input router's click handling, so Enter and Space activate them as a click does, a disabled arrow does nothing, the host can remap the action, and one activation runs one step. The managed click handler goes, so an activation cannot step twice. C# receives the boundary state its rendering needs.

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

### Baseline

Measured on Blazor Server by counting the circuit's messages for each scenario on a page of freeze panes in arrows mode with wheel paging; each scenario is counted once the circuit has gone quiet. Crossings are calls in either direction; their completions are not counted, and neither are the Showcase layout's own calls.

| Scenario | C# to browser | Browser to C# | Render batches |
| --- | --- | --- | --- |
| First pane on a new circuit | Imports of the scroll, wheel and input router modules, the router's configuration and host registration, and an attach for the scroll and the wheel | Two boundary reports | 4, including the page's own first render |
| A second pane on the same circuit | Two imports and two attaches | Two boundary reports and the click that added it | 3 |
| Fifty panes added at once | 100 imports and 100 attaches | 100 boundary reports | 23 to 33 across runs |
| Scrolling within the middle, and wheel turns | None | None | None |
| Start to the middle | None | One report | 1 |
| Middle to the end | None | Two reports | 2 |
| End to the start | None | Three reports | 3 |
| Each arrow press | One step call | The click | 3 |
| Arrows switched off or on | None | The click and one report | 2 |
| Wheel paging switched off | The wheel's disposal and its reference release | The click | 1 |
| Wheel paging switched on | One attach | The click | 1 |
| One pane removed | Two disposals and four reference releases | The click | 1 |

The browser already keeps midrange scrolling and wheel paging local, and suppresses equal reports. The baseline shows four improvements for the first slice:

- **Follow-up reports.** A pane's report carries the viewport extent, which changes whenever an arrow appears or disappears, so a new pane reports twice and a boundary change up to three times. The component reads that extent only for the frozen-edge splitter, so the pane requests it only while the splitter is shown, and each creation and boundary change then reports once.
- **Arrow presses.** Each press crosses twice, the click to C# and the step back to the browser, and renders three times. A step run in the browser takes the press to no command round trip; a press that changes a boundary still produces a notification and a render, and one that does not produces neither.
- **Imports.** Each pane makes two import calls of its own. Through the bridge, each used root is imported once per runtime scope and shared, so a pane on a circuit that has already imported its roots makes none.
- **Removal.** Removing a pane makes six calls. One owner release is the ordinary teardown target; cleanup of a late attachment and the framework's reference disposal are counted separately.

### After the first slice

Measured with the same probe, page and scenarios as the baseline.

| Scenario | C# to browser | Browser to C# | Render batches |
| --- | --- | --- | --- |
| First pane on a new circuit | Imports of the watcher, the engine and the input router, the router's configuration and host registration, and an attach for the observation and the interaction | None | 3, including the page's own first render |
| A second pane on the same circuit | Two attaches | The click that added it | 2 |
| Fifty panes added at once | 100 attaches | The click | 12 |
| Scrolling within the middle, and wheel turns | None | None | None |
| Start to the middle | None | One notification | 1 |
| Middle to the end | None | One notification | 1 |
| End to the start | None | Two notifications, one for each state the smooth scroll passes through | 2 |
| An arrow press, by click, Enter or Space | None | One notification where a boundary changes, else none | 1 where a boundary changes, else none |
| Arrows switched off or on | None | The click | 1 |
| Wheel paging switched off | One detach | The click | 1 |
| Wheel paging switched on | One attach | The click | 1 |
| One pane removed | One release | The click | 1 |

Every target the baseline set is met. A new pane reports nothing beyond its attach, whose result carries its first state, and each boundary change reports once. An arrow press makes no command round trip, and a press that moves within the middle crosses nothing at all. A pane on a circuit that has already imported the entries imports nothing, and removal is one release, with no framework reference released per pane.

### Notification ordering

A notification the browser sends during an attach call, or in a microtask after it, reached C# before that call's result in every one of 2,400 attaches, sequential and concurrent alike; one sent in a later task arrived after the result in all 1,200. Overtaking is therefore the normal case for anything an attach reports synchronously, so the establishment barrier is required, not a guard against a rare race.

The first slice has the browser defer a registration's first sample until a task after its attach returns, folding anything measured meanwhile into one latest sample. That is an optimization: on Blazor Server the result then usually reaches C# first, so the barrier usually holds nothing, but the observed ordering is not a guarantee. The barrier stays independently correct: a failed attachment, a disposal and an obsolete generation each release any delivery it holds, without deadlock, whatever the deferral did.

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
| Target cleanup | Disposing or retargeting a registration releases its browser resources. Removing a target whose resize notification exposes the removal releases them without managed teardown; moving it within the document does not count as removal. A removal no resize notification reports, such as of a target already zero-sized, waits for its owner's release. |
| Functional parity | Inline and block axes, RTL, wheel enabling and disabling, action remapping, stop stepping, native scrolling, arrow boundaries, frozen and corner extents, and frozen-edge resizing. |
| Rendering ownership | Ordinary renders and the host's `Style` and `Class` keep their behavior; watcher measurements take no ownership of component-rendered CSS variables. |
| Independence | A watcher-only consumer imports no engine or input code, creates no input registration, and observes successfully. |
| Recovery | Known-inactive state retries on sync. Explicit refresh restores browser-side suspended reporting even when managed options are equal, gives up on a send never answered, and delivers a fresh state even where it equals the last, without replaying commands. A restored Server connection refreshes each established observation of the circuit's bridge in one refresh invocation, counted apart from the notifications it causes, revives no released or replaced registration, and calls nothing on the circuit's first connection. Retained circuits reuse objects and new owners attach normally. |
| Import scope | Concurrent owners share one in-flight import per root; watcher-only use leaves the engine unloaded. Check Server and WebAssembly lifetimes, prerender deferral, transient import failure, owner-specific diagnostics and disposal during import. |
| Deferred context checks | A context created and disposed concurrently with a watcher or an engine leaks no browser session and recreates none; a disposed context's `DomWatcher` and `DomEngine` create nothing; the context's release reaches every owned session even when one release fails. |
| Arrow activation | A click, Enter and Space on an arrow each step once; a disabled arrow does not step; a remapped action steps; no managed click handler remains. |
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
| Removal detection beyond resize notifications | A consumer whose targets are removed without disposal or retargeting and without a resize notification, with the cost of observing the document's child list measured. |
| Protocol epochs | An owner or session replacement that could confuse messages despite the registration lifetime guards. |
| A persistent capability binding | Consumers that hold a capability across renders and retarget it repeatedly, where obtaining it through `GetElement` each time proves awkward. The binding is a persistent, retargetable reference to a capability over the same implementation and registration lifetime, extending to commands the retargeting registrations already have through `SyncAsync`. |
| Sharing the settling helper with `InputRegistration` | The DOM registrations having demonstrated awaitable settlement. It changes observable behavior, since overlapping calls would then await settlement, and takes a review of its own. |

## Migrating folders

The scripting guide is updated before the first slice to state the concern-based Browser.Interop organization and its named exceptions: `components/Virtualize.js`, the input router's paths, product scripts and third-party adapters. The root and the Scrolling area are documented first, and later area folders arrive with their consumers.

Each concern moves with its consumers and removes its unused old entries before the next concern starts. A temporary compatibility wrapper forwards to the one implementation and carries a named removal gate, so no algorithm exists in two copies.

A conversion replaces its area's old scripts and wrappers with the area's concepts rather than relocating them first, so nothing moves twice. The kind folders, `behaviors/`, `operations/` and `components/` with their `Behaviors`, `Operations` and `Components` namespaces, are removed as conversions empty them. A script that survives as it is moves to its area when there is reason to work on it: `Virtualize.js` and its wrapper to Virtualization, `Embedding.js` and its wrapper to Integrations. Each public namespace a conversion or a move changes is recorded in its landed note as a breaking change.

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
