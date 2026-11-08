# Replacing Forma's JavaScript with DOMQL

Forma's components do their browser work through 80 scripts, 10,282 lines of JavaScript. Each is to become DOMQL infrastructure: a query, a watch, a subscription, an execution, a Forma module's implementation, or Forma's host layer around DOMQL. This document records what that replacement needs of DOMQL, and what each script becomes.

DOMQL's [planned capabilities](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-capabilities) give each capability an identifier, and name no consumer. Each is now implemented, or declined with an alternative, as DOMQL's [decisions](https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md) record. This document adds what those records leave to Forma:

- for each capability, its decision and the Forma scripts that use it;
- for each declined one, what Forma does instead;
- what each script becomes, and Forma's work, step by step.

| Surveyed | At |
| --- | --- |
| Forma | `592b12b7`: the 84 scripts under `src/**/wwwroot/scripts/`, tests, `node_modules` and `.local/` excluded, with their C# wrappers and the components that use them; and the culture initializer outside `scripts/`. The 80 of `ca158ef5` are byte-identical to the survey's `bf7f94df`, moved under `src/Framework/` and `src/Blazor/`, and the [browser transfer](browser-transfer-design.md), `87b4866a` to `592b12b7`, added four and changed fourteen |
| DOMQL | `6519833`: specification `1.0.0-beta.2`; the queries moved to `2ae0413` |
| Nexo | Release `1.0.0-beta.1`: language `1.0.0-beta.40`, which DOMQL runs on; release `1.0.0-beta.2`, language `1.0.0-beta.41`, from DOMQL's `a366048` |

The survey was rechecked against these, script by script, line counts included: each script was read with its C# callers, and the plan's texts and module declarations were run against DOMQL. At Forma `592b12b7` the rows follow the transfer as it landed: every value a reader can make large crosses to C# through `Transfer.js`, a text field and the rich-text editor send their edits rather than their text, through `TextEdits.mjs` and `TextSources.mjs`, a field's selection is a watch of DOMQL's own `selection`, and focus reaches C# state by state, ahead of what the reader does next. Its queries then moved to DOMQL `2ae0413`, on Nexo `1.0.0-beta.2`: a state read as a property, `it.focusable`, an action by its verb, `focus()`, `markIndeterminate()`, `writeToClipboard(text)`, `mute()`, `replaceValue(value)` and `select({ start, end })`, a capability by what it tells, `canShare` and `hasClipboard`, and Nexo's rounding called, `round()`, as DOMQL's [D87] and [D88] record; each query this moved resolves there. Of the scripts, only `Print.js` and `CultureCoordinator.js` have changed since the survey's first baseline, Forma `e9e61371`. The gaps the recheck found in DOMQL are settled by [D82], [D83] and [D84], and so is every gap the surveys before it found. What happy-dom can't decide was then probed in headless Chromium, through DOMQL's own tests, which name no Forma code: a real layout, an editor's selection, a blur before a press and an input method's composition.

Forma's own plan for the conversion is its [DOM watcher and engine design](dom-watcher-engine-design.md) and its [implementation plan](dom-watcher-engine-implementation-plan.md). Steps 0 to 4 have landed: context lifetime, the freeze pane, the scroll sentinel, `DomSession` and focus. This document names the remaining steps by number:

| Step | Converts |
| --- | --- |
| 5 | The request model |
| 6 | Input routing |
| 7 | Anchored presentation |
| 8 | Virtualization |
| 9 | Gestures, files and reordering |
| 10 | Dialogs, frames and windows |
| 11 | The remaining actions |

A script the plan doesn't convert is named by its product: the composer, the data view, the explorer, culture or the Showcase.

The design's DOMQL examples predate today's notation (`@viewport`, `focus.within`, `attribute-of`). This document writes today's:

- a query imports the modules it uses, `import FormaScrolling`, each on a line of its own, and DOMQL's own module, `Domql`, without writing it;
- the document and the window are `Domql.document` and `Domql.window`;
- DOMQL's own members are read with `.`, a module's extensions of DOMQL's types with `:`, as `viewport:scrollStep({ … })`;
- object fields are written with `=`, and the DOM's types are named in PascalCase, as `Element`.

Every name this document gives a Forma module's member is provisional, Forma's to settle as it builds the module. DOMQL's names are its specification's.

In this document:

1. [The verdict](#the-verdict)
2. [How the scripts map onto DOMQL](#how-the-scripts-map-onto-domql)
3. [Decisions the replacement rests on](#decisions-the-replacement-rests-on)
4. [What Forma uses of DOMQL](#what-forma-uses-of-domql)
5. [The scripts, one by one](#the-scripts-one-by-one)
6. [The order of the work](#the-order-of-the-work)
7. [Findings in Forma along the way](#findings-in-forma-along-the-way)

## The verdict

DOMQL's model fits Forma, and nothing in DOMQL blocks Forma's plan any longer.

- **Every capability the survey asked of DOMQL is settled.** Each V, E, O, X, C, T and A item is implemented, or declined with an alternative Forma uses, by a decision of DOMQL's. [What Forma uses of DOMQL](#what-forma-uses-of-domql) gives each one's decision and the scripts that use it.
- **The two gaps the recheck found are closed:**
  - A query couldn't declare a binding's type, so the input router's routes and overrides and a bound structured option were refused (NEXO3042). A query now declares them, `domql.query(text, bindings, { types })` ([D82]).
  - A class or a property a module keeps gave way to a component rendering it twice in quick succession, so a window's `is-dragging` could be lost mid-drag. It now gives way only to a writer restoring its own value, which Blazor never is ([D83]).
- **The recheck corrected the plan where it would have failed.** Of most consequence:
  - the sentinel's refresh hands C# its snapshot, since an equal refresh raises no event, and infinite scroll stalls without it;
  - the composer's key reactions look the editor's command up by the route's action, and C# runs no command a reaction ran;
  - the frame's placement results name their drag, and C# subscribes to them before the frame starts, since it reports as it starts;
  - a behavior whose options change binds them, since an update replaces bindings, never a text's literals;
  - a member reading maintained values is nullable (NEXO4071), and each C# record takes the null or the host passes on none.
- **The C# half waits for its design** ([D46]), and A5's bounded queue with it ([D69]). Forma's host layer covers both meanwhile. It carries each watch's snapshots, each behavior's occurrences and each execution's answer to C#, which DOMQL hands it in the order they happen, across handles and owners.
- **The reading half is done.** DOMQL serves these outright, through the host layer:
  - the data cards' column count, `GridColumns.js`;
  - the explorer's header tier, `Breakpoint.js`;
  - the data view's height reservation, `HeightReservation.js`;
  - the menu's document order, `DocumentOrder.js`;
  - the freeze pane's scroll state, `ScrollStateObserver.mjs`, through `scroll` ([D9]) and the `children` target ([D11]);
  - the scroll sentinel, `IntersectionStateObserver.mjs`, refreshed with a new sample after each load, its snapshot then sent whether or not it changed ([D66]);
  - whether the reader is within the date-time picker, `FocusStateObserver.mjs`, and the virtualizer's measurements, `Virtualize.js`, each a member a Forma module keeps with its own observation type.

  Every observation Forma's foundation runs by hand, its frame coalescing, its change-only reporting and its detached elements, is what a DOMQL watch does already. Its resize and intersection pools are not: DOMQL keeps a native observer for each element, where the pools share one, which costs observers and changes no behavior.
- **Forma's algorithms stay Forma's.** Under the [decisions](#decisions-the-replacement-rests-on) below, the input router, the gestures, placement, focus, the virtualizer and the rich-text editor become Forma modules' implementations, and the culture coordinator keeps its own bridge, outside DOMQL, since its registration precedes a circuit's first render ([culture](#culture)). Their listeners, reads and writes stay native, inside them. They reach DOMQL through its extension contract: occurrences, commands, the owner, a behavior's state, and owned writes beside Blazor's render.
- **What remains is Forma's work,** step by step, beginning with the host layer at step 5, as [the order of the work](#the-order-of-the-work) sets out.

## How the scripts map onto DOMQL

Each script becomes one of DOMQL's request kinds, or stays where it is. A script is counted once, by what its main part becomes; [the scripts, one by one](#the-scripts-one-by-one) names the parts it adds.

| Today | Becomes | DOMQL operation | Scripts |
| --- | --- | --- | --- |
| A read C# asks for once | A query | `evaluate`, `evaluateAsync` | 1 |
| A state C# keeps current: a size, a boundary, a tier, whether the reader is within | A watch | `watch`, its snapshots or live state | 10 |
| What a module decided, which C# hears | A subscription | `subscribe`, to a module's event source | 1 |
| A change C# asks for once: focus, scroll, copy, print | A once execution | `executeAsync` | 11 |
| A behavior attached for a lifetime: routes, a gesture, a placement, an editor | A continuous execution | `run`, `update`, `dispose` | 19 |
| An algorithm no request can express: input ownership, gestures, focus, rich-text editing, culture | A Forma module's implementation | Declared with `domql.createModule`, and loaded on request through a loader registered by its name ([D68]) | 22 |
| Transport, sessions, tokens and generations, one delivery in flight, reconnection | Forma's host layer around DOMQL | None: they are Forma's interop concerns | 11 |
| A helper module code calls synchronously, or a pure transform | Stays as it is | None | 5 |

Forma's design calls a once execution an **action**, a continuous one a **behavior**, and what a behavior's event source delivers an **occurrence**. DOMQL calls a once execution that reaches a running behavior a **command**.

The Forma modules this produces, each beside the capability it exposes:

| Module | Its executions, sources and members | From |
| --- | --- | --- |
| `FormaInput`, which `InputRouterDomqlModule` declares | `inputRoutes`, `hostInputRoutes` (continuous); `configureInput` (once); `inputActions`, `inputDismissals`, `inputDecisions` (sources); `inputState`, `inputStack` (members) | `InputRouter.js`, `InputInspector.js` |
| `FormaScrolling` | `scrollStep` (once); `scrollInteraction`, `pan`, `wheelTurns` (continuous); `wheelDirections` (source) | `ScrollContainer.mjs`, `ScrollInteraction.mjs`, `ScrollButtons.mjs`, `PanGesture.js`, `Wheel.js` |
| `FormaFocus` | `focusInteraction` (continuous); `focusFirst` (once), the first element Tab reaches within a part; `readerWithin` and `focusedItem` (maintained members with their own observation type) | `focus/*.mjs` |
| `FormaText` | `edits` (continuous), with its count, `write`, `resend`, `flush` (commands), `changes` (a source) and `revision` (a member reading the state `edits` offers); `autofillMark` (continuous) | `TextEdits.mjs`, `TextSources.mjs`, `CharacterCount.mjs`, `AutofillTracker.js` |
| `FormaPlacement` | `anchoredPlacement` with the commands `open` and `close`; `placement` with `placementChanges` and the commands `measure` and `commit`; `followSelection` (continuous) | `AnchoredPopover.js`, `Frame.js`, `SelectionFollower.js` |
| `FormaPresentation` | `topLayerPresentation` with the commands `raise` and `refocus`; `scrimClose` (continuous, opt-in) | `Window.js`, `Dialog.js`'s scrim close |
| `FormaDragDrop` | `dropZone` with `drops`, `fileDrop`, `reorder` with `reorderMoves` (continuous) | `DropZone.js`, `FileDropZone.js`, `ReorderInteraction.js` |
| `FormaGestures` | The drag gesture, which other modules compose; `splitterDrag` with `splitterDrags` | `DragGesture.js`, `Pointer.js`, `Splitter.js` |
| `FormaFiles` | `filePreview` with the command `show` (continuous) | `FilePreview.js` |
| `FormaVirtualizer`, which `VirtualizerDomqlModule` declares | `virtualizer` (a maintained member of a nullable type with its own observation type); `reveal`, `scrollTo` (once) | `Virtualize.js` |
| `FormaAssets`, `FormaPrint`, `FormaEmbeds` | `loadModule`, `loadStyle`, `loadSprite`; `printDocument`, `composeDocument`, `composePage`; `activateEmbed` (once) | `Asset.js`, `Print.js`, `Embedding.js` |
| `FormaRichText` | The view `richText`: `edit` (continuous), `execute`, `commit`, `beginLinkDialog` (commands), `changes`, `activations` (sources), `selection` (a member reading the state `edit` offers); `blockAlignment` (continuous, on the gutter) | `RichTextEditor.js` and its modules, `BlockAlignment.js` |
| `FormaDataView` | The view `dataView`: `selection` (continuous), `selections` (source), `focusItem`, `cancelSeek` (commands); the view `dataGrid`: `wantedWidths` (a maintained member answering `List<ColumnWidth>?`, with its own observation type) | `SelectionInteraction.js`, `ColumnFit.js` |
| The Showcase's own | `readingPlace` with the command `reveal` | `ReadingPlace.js` |

## Decisions the replacement rests on

What Forma uses of DOMQL rests on these decisions. D1 to D3 are DOMQL's, recorded in its [decisions](https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md) as [D1], [D2] and [D3]. F1 to F3 are Forma's, named apart from DOMQL's, which this document cites by their own numbers.

| | Decision | Whose | Why |
| --- | --- | --- | --- |
| D1 | Generic web-platform changes are DOMQL's built-in executions, and Forma's algorithms are Forma's modules. A change is built in where it is one platform call with a platform result, as focusing, scrolling, showing a popover and writing the clipboard are. It is a module's where it carries a policy, as stop-aware stepping, placement, gestures and roving focus do. | DOMQL | DOMQL stays independent of its consumers, and every consumer needs the generic half. |
| D2 | `on` stays passive and never cancels. Canceling an event, consuming a key, claiming a click and capturing a pointer stay synchronous inside a module's own listeners: the input router's, and those of the modules that compose it. `on` listens in the capture phase too, for observing alone ([D27]). | DOMQL | Every input a component acts on is a route on the input router, so a canceling `on` would let a component act past it. A canceling subscription would also have to decide synchronously, before any caller sees the event. |
| D3 | A behavior's occurrences are event sources of the module that declares the behavior. A source is bound to the running continuous execution of the same owner on the same receiver, and outlives its replacement and release ([D49]). | DOMQL | Forma's design states this contract, and every behavior that reports needs it. |
| F1 | Forma keeps its host layer: the transport to C#, sessions by runtime key, tokens and generations, a watch's state delivered one at a time with only the latest waiting, but focus's, every state of which is sent in order as it is measured, since a press or a key acts on where the focus went, occurrences sent in order as they happen and never held for an earlier one's acknowledgment or coalesced ([D69]), every value a reader can make large crossing through the [browser transfer](browser-transfer-design.md), suspension and resumption, and reconnection. While the C# half waits for its design ([D46]), the host layer carries every result to C#, and loads each Forma module through DOMQL's loader before a query that imports it ([D68]). | Forma | They are Forma's interop concerns, and DOMQL's `signal` already gives each owner its disposal. |
| F2 | The scripts Forma's modules call synchronously stay plain classes that work without DOMQL, composed inside the modules: `FocusSurface`, `TabOrder`, the drag gesture, `Controls`, `Direction`, `Scrolling` and `FrameScheduler`. | Forma | Their callers decide inside native handlers, where no query can run. |
| F3 | A frame hands its geometry back to the render through a `commit` command carrying the placement Forma accepted and naming the drag its placement result reported, run from `OnAfterRenderAsync` of the render that carries it, for the frame's owner, never through DOMQL's acknowledgement ([D50]). A drag that starts while an earlier drag's hand-over is pending takes the holdings over, so the earlier `commit` releases nothing. The design's [frames](dom-watcher-engine-design.md#frames) state the rule. | Forma | A placement Forma rejects or keeps renders nothing. Ending the holdings would then leave the drag's geometry, and restoring would undo a placement Forma accepted. The acknowledgement carries no placement to settle either. |

What the decisions mean for what Forma uses:

- **A module's own code reads and writes the DOM natively,** inside its listeners. It uses DOMQL's extension contract, its host side and its timing, not its vocabulary, events or built-in executions. Where it writes a target Blazor renders, it holds the target through an owned write it runs with `env.domql` ([D70]).
- **A capability counts for a script** where C# reads, hears or asks for something through DOMQL, or where a module builds on DOMQL by choice, as with `env.observe` or `env.domql`.
- **Another form of a script,** as a query of what its module reads natively today, is named where it helps. What only that form would use is listed under "Also".

## What Forma uses of DOMQL

DOMQL settled every capability the survey asked of it. Each table gives, for each of DOMQL's identifiers:

- **Decision:** the decision of DOMQL's that settled it, or its specification where no decision was needed; a part DOMQL declined says what Forma does instead;
- **Used by:** the Forma scripts that use it, in the form this document chooses for them;
- **Also:** the scripts another form would use it for, an improvement on today's behavior, or a cost it saves.

No capability blocks a step. Each is defined in DOMQL's [specification](https://github.com/FormaUI/DOMQL/blob/main/spec/README.md), which its decision links to, and summarized here in a few words.

### The language

The earlier survey's language gaps, L1 to L6, are Nexo's, and DOMQL adopted them in its [migration to Nexo](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#migration-to-nexo): list members, imports and extension access; numbers and strings; structured values written in place, or bound where the query declares the binding's type ([D82]); an item's position in a list; and resolutions that stay current when a module registers. A host call's kept function, C13, is Nexo's language `1.0.0-beta.20`'s ([D61]). No language gap remains for Forma.

### Vocabulary

DOMQL's [vocabulary](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-vocabulary): V1 to V18, each implemented, a part declined where its decision says.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| V1 | `Element.scroll`, the logical offset, extent and viewport of each axis | [D9] | `ScrollStateObserver.mjs`, `Scroll.js` | Query forms: `Scrolling.js`, `ReadingPlace.js` |
| V2 | `Element.scrollContainer({ axis })`, the nearest box the user can scroll | [D10] | — | Query forms: `Scrolling.js`, `Virtualize.js` |
| V3 | `Document.activeElement`, through open shadow roots | [D12] | — | Read natively by module code: `TabOrder`, `RovingFocus`, `FocusRecovery`, `FocusStateObserver`, `Window.js`, `SelectionInteraction.js`, `InputRouter.js` |
| V4 | `Element.direction`, as `dir` decides it along the ancestry | [D13] | `ScrollStateObserver.mjs`: the frozen extent under RTL | Improvement: the freeze pane follows an in-place change of `<html dir>`, which today's measurement misses. Query forms: `Splitter.js` reporting logical travel. `Direction.js` reads the computed direction, `style["direction"]`, a different question |
| V5 | `displayed` and `focusable`; no function answers `checkVisibility({ visibilityProperty: true })` | [D14], [D96] | — | Query forms of whether an element can take focus: `FocusSurface.mjs`, `Controls.js`. Tab's reach is sequential focus navigation, which `focusable` doesn't answer ([D96]), so `FormaFocus`'s `TabOrder` keeps it, `Dialog.js`'s first control among it |
| V6 | `Element.value`, `indeterminate` | Implemented: [`value`](https://github.com/FormaUI/DOMQL/blob/main/spec/vocabulary.md#property-element-value), [`indeterminate`](https://github.com/FormaUI/DOMQL/blob/main/spec/vocabulary.md#state-element-indeterminate) | — | `CheckBox.js`, reading what the element holds rather than what was written ([findings](#culture-operations-and-helpers)) |
| V7 | `Element.text`, `Element.html`; no `hasText`, which `it.text != ""` asks | [D15] | — | `BlockAlignment.js` as a watch; `MediaMarkup.mjs` read through an activation's projection |
| V8 | The document selection and a `Range` type | [D20], [D21] | — | `SelectionInspector.mjs` as a query |
| V9 | `Node.contains`, element siblings and `index`; `follows` declined, since a list's order is `all` with `indexOf` | [D22] | `DocumentOrder.js`, through `all` and `indexOf` | `FocusSurface.mjs` as a query |
| V10 | `Element.localName`, `classes`, `inlineStyle[property]`; the planned `classList` is `classes` | [D16] | `InputInspector.js`: describing the element that owns an input | `SelectionInspector.mjs` as a query |
| V11 | `Element.clientRect` | Implemented: [`clientRect`](https://github.com/FormaUI/DOMQL/blob/main/spec/vocabulary.md#property-element-clientrect) | — | `Virtualize.js` as a query |
| V12 | `Element.sizeLimits`, in pixels | [D24] | — | `Frame.js` as a query |
| V13 | A tolerance on measuring members, a twentieth of a pixel | [D8] | Every watch of a measurement: `ScrollStateObserver.mjs`, `HeightReservation.js`, `GridColumns.js` | `BlockAlignment.js` as a watch. The virtualizer, the frame and the anchored popover round in their modules |
| V14 | `Element.files`, without content | [D25] | — | `FilePreview.js`, with `ownFileSource` (X10) |
| V15 | `autofilled`; no autofill observation | [D17] | — | `AutofillTracker.js` as a watch, which would miss a fill as the page loads; the module's own `animationstart` listener hears it |
| V16 | `playback` | [D18] | — | None: `Playback.js` reads no playback state |
| V17 | `Window.location` and `shortcutModifier`; `documentElement` declined, since `Domql.document.first(":root")` reads the root element | [D23] | — | `ReadingPlace.js` as a decomposed script. The router reads its primary modifier natively ([findings](#the-input-router)) |
| V18 | `hasPointerCapture(pointerId)` | Implemented: [`hasPointerCapture`](https://github.com/FormaUI/DOMQL/blob/main/spec/vocabulary.md#function-element-haspointercapture) | — | None since `Pointer.cs` was removed; the gesture captures in its own press handler |

### Events

DOMQL's [events](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-events): E1 to E9, each implemented. Forma uses none of them: the input router hears every input natively ([D2]), and C# hears what it decided as occurrences ([D49]). Each serves only a form this document doesn't choose.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| E1 | `on(type, { capture = true })` | [D27] | — | `ReadingPlace.js` as a decomposed script, which must note the place before Blazor's enhanced navigation cancels the click. Blazor's interception is a bubbling listener at the document, so a capturing one sees the click uncancelled and the old `location`, and a note it writes to session storage is read on the new page, after an enhanced navigation and a full load alike |
| E2 | Pointer events, `pointerId` null where none is, `detail`, `timeStamp`, modifiers; `touchstart` | [D28], [D29] | — | `ReadingPlace.js`: click modifiers, `touchstart` |
| E3 | `wheel` | [D29] | — | `ReadingPlace.js`: a wheel turn ends its hold |
| E4 | Drag events, `DragEvent.types` | [D29] | — | None: `DropZone.js` listens itself, and the router reads the types |
| E5 | `focusin`, `focusout`, `relatedTarget` | [D30] | — | None: the window, the data view's selection and the selection follower listen themselves |
| E6 | Editing and clipboard events, `selectionchange` | [D31] | — | None: the rich-text editor listens itself |
| E7 | `close`, `cancel`, `beforetoggle`, `toggle` | [D32] | — | `Dialog.js`, if its close became an occurrence; Blazor's `@onclose` follows every close today |
| E8 | Animation, `load`, `error`, media events | [D32] | — | `AutofillTracker.js` as a subscription to `animationstart` |
| E9 | `repeat`, `isComposing`, a key's modifiers and AltGraph, `defaultPrevented`, `composedPath` | [D33] | — | `ReadingPlace.js`: `defaultPrevented` |

### Observation

DOMQL's [observation](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-observation): O1 to O4 and O6, implemented, O4's autofill observation declined. O5 needs nothing: a module's page-wide state is a member of its extension of `Document`, as the input router's active stack is.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| O1 | An observation target `children` | [D11] | `ScrollStateObserver.mjs`: `scroll` follows its content's sizes | — |
| O2 | `all`, `first`, `matches` and `closest` observed for what their selector reads, across the tree, in place of the receiver's subtree | [D26] | Every watch reading `all` or `first`: `ScrollStateObserver.mjs`, `HeightReservation.js` | Cost saved: the selection's `tabindex` writes and the router's layer stamps evaluate no watch whose selectors don't test them |
| O3 | Continuous measurement each frame, the `frame` observation | [D56] | — | None: the drag gesture's edge scrolling is its own |
| O4 | `dir` along the ancestry; media events; no `autofill` observation, which would write a stylesheet into the page | [D13], [D18], [D17] | `ScrollStateObserver.mjs`: `dir`, through `direction` | `AutofillTracker.js` as a watch |
| O6 | An invalidation beyond a watch's members, which a body's local reading a member that observes it gives | [D19] | — | `BlockAlignment.js` as a watch |

### Executions

DOMQL's [executions](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-executions): X1 to X10, each implemented, `readClipboardText` and `writeCookie` declined.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| X1 | `focus({ preventScroll })`, answering whether focus landed | [D34] | `Dialog.js`: the first control after opening | `AnchoredPopover.js`'s return of focus to the trigger, which its module makes natively; `FormaDataTree`'s focusing, which Blazor's own `FocusAsync` does today |
| X2 | `scrollTo`, `scrollBy`, `scrollIntoView`, logical, answering once they have ended, an instant one at once | [D35], [D67] | `Scroll.js`: scrolling to the center | `Virtualize.js`'s `reveal` and `scrollTo`, which its module carries out natively; `ReadingPlace.js`; `BlockFormatter.mjs`'s reveal, outside its command |
| X3 | `showPopover`, `hidePopover`, `showModal`, `close`, answering whether they changed the element | [D36] | `Dialog.js` | `AnchoredPopover.js` and `Window.js`, inside their modules |
| X4 | `capturePointer`, `releasePointer` | [D37] | — | None since `Pointer.cs` was removed |
| X5 | Owned writes, `ownAttribute`, `ownClass`, `ownStyle` and `ownText`, each saying whether it restores and keeps its value | [D38], [D70] | Module writes beside Blazor's render, through `env.domql`: `AnchoredPopover.js`, `AutofillTracker.js`, `FileDropZone.js`, `ReorderInteraction.js`, `Frame.js`, `CharacterCount.mjs`, `BlockAlignment.js` (a row's `hidden`), `SelectionFollower.js` (its active class) ([below](#owned-writes-beside-blazors-render)) | `RovingFocus.mjs`'s `tabindex`, should Blazor render one on a toolbar's child |
| X6 | `markIndeterminate()` and `clearIndeterminate()`, `replaceValue` and `select`, answering what they changed | [D39] | `CheckBox.js`, `TextEntry.js` | — |
| X7 | Clipboard, download, print, share and media playback, answering how the browser took each request. `readClipboardText` is declined, since `readFromClipboard()` answers plain text by default, and `writeCookie` too, since the culture coordinator writes its cookies inside its own acceptance | [D40], [D41] | `Clipboard.js`, `Download.js`, `Print.js` (the page), `Share.js`, `Playback.js` | `readFromClipboard`, for the rich-text editor's paste, through `env.domql` |
| X8 | `dispatch({ type, bubbles, detail })` | [D42] | — | None: the file drop zone and the router dispatch inside their own handlers |
| X9 | Selection placement | [D43] | — | None: the composer places the selection inside its commands |
| X10 | A chosen file shown as an element's source, `ownFileSource`, its object URL owned | [D44] | — | `FilePreview.js`, in place of `FormaFiles`' preview, each thumbnail's index found by name |

### The extension contract

DOMQL's [extension contract](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-extension-contract): C1 to C14, each implemented. C14 was this survey's; DOMQL built it as [D50].

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| C1 | Occurrences a behavior offers, through its module's event sources, which hear every instance for their owner on their receiver | [D49] | `InputRouter.js`, `DropZone.js`, `ReorderInteraction.js`, `Wheel.js`, `Frame.js`, `Splitter.js`, `SelectionInteraction.js`, `RichTextEditor.js`, `CultureCoordinator.js`; `Owner.js`'s reports retire into it | — |
| C2 | State a behavior offers, read by its module's members and followed through the `behavior` observation | [D52] | `RichTextEditor.js`: the selection `edit` offers; `InputRouter.js`: `inputState` | `Frame.js`, its placement read as state rather than heard; `ColumnFit.js`, as a behavior a watch reads |
| C3 | Commands to a running behavior, reaching the latest instance for their owner on their receiver, and failing with `DOMQL5016` where none runs | [D48] | `AnchoredPopover.js` (`open`, `close`), `Window.js` (`raise`, `refocus`), `Frame.js` (`measure`, `commit`), `FilePreview.js` (`show`), `SelectionInteraction.js` (`focusItem`, `cancelSeek`), `RichTextEditor.js` (`execute`, `setHtml`, `beginLinkDialog`), `CultureCoordinator.js` (`requestCulture`, `reportCultureLanding`, `followCulture`), `ReadingPlace.js` (`reveal`) | — |
| C4 | The owner's identity, an `owner` every operation takes | [D47] | Every behavior with occurrences or commands: `InputRouter.js`, where several registrations share a scope, `DropZone.js`, `ReorderInteraction.js`, `Wheel.js`, `Splitter.js`, `Frame.js`, `AnchoredPopover.js`, `Window.js`, `FilePreview.js`, `SelectionInteraction.js`, `RichTextEditor.js`, `CultureCoordinator.js`, `ReadingPlace.js` | — |
| C5 | DOMQL's observations and frame schedule for module code, `env.observe` and `env.frame`, a frame's writes before its reads | [D53] | — | Sharing DOMQL's pools: `AnchoredPopover.js`, `Frame.js`, `Virtualize.js`, `RovingFocus.mjs`, `FocusRecovery.mjs`, `ScrollButtons.mjs`, `SelectionInteraction.js`, `BlockAlignment.js`, `SelectionFollower.js`. `FrameScheduler.js` stays for the plain classes (F2) |
| C6 | Queries from module code, `env.domql` | [D54] | The modules that hold a rendered target through an owned write ([below](#owned-writes-beside-blazors-render)) | `ScrollButtons.mjs`, `RovingFocus.mjs` |
| C7 | A transient measurement, `env.measureUnder`, which DOMQL's own observers never hear | [D55] | `ColumnFit.js`: measuring under a class without invalidating every other watch | — |
| C8 | Module state per document, one across every instance of DOMQL, which the module keeps by `env.document` | [D57] | `InputRouter.js`, `Window.js` and `Asset.js`, whose module-level state becomes one per document | — |
| C9 | Captured members with history per holder | [D58] | — | None: the virtualizer keeps its history in its observation type's instance, the wheel and the window in their running instances |
| C10 | Synchronous handling: `deliver` calls every handler before it returns, and never throws into the listener | [D51] | `ScrollInteraction.mjs`, `FocusInteraction.mjs`, `TabOrder.mjs`, `RovingFocus.mjs`, `InputRouter.js`, `DragGesture.js`, `DropZone.js`, `FileDropZone.js`, `PanGesture.js`, `ReorderInteraction.js`, `Wheel.js`, `Frame.js`, `Splitter.js`, `SelectionInteraction.js`, `Window.js` | — |
| C11 | A null argument passed as it stands where its parameter accepts null | [D59] | `FocusInteraction.mjs` (`roving`, `heir`), `ScrollInteraction.mjs`, `ScrollContainer.mjs` (`stops`, `frozen`), `CharacterCount.mjs` (`maxLength`), `InputRouter.js` (`escape`, `closeRequest`, `pressAway`, `parent`), `Wheel.js` (`modifier`), `AnchoredPopover.js` (the trigger), `DropZone.js`, `Frame.js` (`bounds`), `Window.js` (`scrim`), `RichTextEditor.js` (`value`), `SelectionFollower.js` (`source`), `CultureCoordinator.js` (`start`) | — |
| C12 | Nested option objects holding an element, Nexo's structured values | [D60] | `ScrollInteraction.mjs`: `buttons`, holding the freeze pane's root element, bound as `FormaScrolling.ScrollButtons?` ([D82]) | — |
| C13 | A kept function argument | [D61] | — | None: the routes C# gives are data, and the JavaScript callers keep the router's own API |
| C14 | **A delivery the host acknowledges:** `deliver` answers a promise that settles once each subscription applied the occurrence | [D50] | `CultureCoordinator.js`: a switch answers `"accepted"` only once the requester has applied it. `run` returns as the registration starts, so the host layer completes a registration once its own handler has applied the first pair. Its subscription is made before `run`, which delivers the pair inside the call; a subscription made after it loses the pair (`DOMQL5017`) | `Frame.js`'s hand-over, which carries the accepted placement, and so is a command ([below](#owned-writes-beside-blazors-render)) |

### API and host

DOMQL's [API and host](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-api-and-host): A2 to A10, implemented or declined. A1, the C# half, and A5's queue wait for its design.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| A1 | The C# half | Deferred until it is designed ([D46]). Forma's host layer carries every result to C# meanwhile (F1) | Every script with a C# wrapper, through the host layer | — |
| A2 | Change sets against an acknowledged result | Declined ([D65]): change sets compose, so a host concatenates the pending ones, or sends the latest snapshot whole as one root replace | `DomWatcher.mjs`, `StateDelivery.mjs`, once change sets cross | — |
| A3 | A refresh that takes new samples, `refreshAsync({ resample: true })`, its wait canceled by a `signal` | [D66]; `rebaseline` declined, since the host reads `watch.snapshot` once the refresh settles | `IntersectionStateObserver.mjs` and the pool it retires, `IntersectionObserverPool.mjs`: the explorer and the emoji picker refresh their sentinel after each load, sending C# the `snapshot` the refresh leaves. `DomWatcher.js`'s `refreshAll`, reading `snapshot` after a reconnection | — |
| A4 | Modules loaded on request, `domql.registerModuleLoader` and `domql.loadModulesAsync` | [D68]. Nothing loads a module implicitly: a request importing a module whose loader hasn't run yet is refused with `DOMQL3007` | Every Forma module, through the host layer: `DomWatcher.js`, `DomEngine.js` and `DomEngine.mjs` | — |
| A5 | Ordered occurrence delivery to C# | The JavaScript side ([D69]): what DOMQL delivers reaches its handlers in the order it happens, across handles and owners. The C# half's bounded queue waits with A1 ([D46]), and Forma's host layer keeps a queue of its own meanwhile, which sends each occurrence as it is queued. In a Blazor Web App of its own, a queue that sent each only once .NET had completed the one before let a button's click overtake an editor's flush on blur in every run, Server and WebAssembly | `InputRouter.js`, `DropZone.js`, `ReorderInteraction.js`, `Wheel.js`, `Splitter.js`, `SelectionInteraction.js`, `RichTextEditor.js`, `CultureCoordinator.js`; `Owner.js`'s reports | — |
| A6 | An execution's lifetime anchored to its element | Declined ([D74]): the caller watches `attached` and disposes the execution once it reads false | `DomElement.mjs`, `DomElements.mjs`, `DomEngine.mjs`: the host ends an execution through a watch of `it.attached` | — |
| A7 | Reactions: once executions a subscription carries out inside each event's delivery, `event` bound, before its handler, so they keep the user's activation | [D71] | `Clipboard.js` and `Share.js`: the copy and the share inside the press that asks for them. `RichTextEditor.js`: its editor commands inside the key's dispatch, through a dictionary from the router's action to the editor's command, and a paste from a menu inside its press | `Dialog.js`'s scrim close, which a reaction doesn't fit, since it runs for every click its subscription hears. `TextEntry.js`'s selection after Go, which waits for C#'s render of the entry |
| A8 | The element a watch's list item came from, `watch.sourceOf(...path)` | [D72] | — | `BlockAlignment.js` as a watch |
| A9 | A delayed schedule, which a refresh flushes; reporting on parts; a subscription's throttle. Delivery by frame is declined, since a throttle paces events and what a frame renders reads the latest itself | [D73] | `InputInspector.js`: a throttle keeping the latest wheel decision, on a subscription to wheels of its own. `TextEntry.js`: the plain text editor's selection watched under a delay | — |
| A10 | Ownership of writes against Blazor's render: each target the framework's or an owned write's, `ownText` for a rendered text, a kept value yielding to another holder, and only to one restoring its own value; isolated layers declined | [D70], [D83] | `AnchoredPopover.js` (inline `left` and `top`), `Frame.js` (its geometry and `is-dragging`), `FileDropZone.js`, `ReorderInteraction.js`, `AutofillTracker.js`, `SelectionFollower.js` (classes), `BlockAlignment.js` (a row's `hidden`), `CharacterCount.mjs` (a rendered text node); `RovingFocus.mjs`, latent (`tabindex`) | — |

#### Owned writes beside Blazor's render

[D70] makes each target of an element a framework renders, an attribute, a class, an inline style property or a text, either the framework's or an owned write's, never both. That settles what Forma's [implementation plan](dom-watcher-engine-implementation-plan.md) asks of each conversion: each target held by one owner, and the frame's geometry through a transient write and a managed commit. An isolated layer per component is declined. What Forma does, as DOMQL's [owned writes beside a framework](https://github.com/FormaUI/DOMQL/blob/main/spec/vocabulary.md#execution-element-owned-writes-framework) set out:

- **Renders nothing into a target a module holds.** `FormaPopover` renders no `left` or `top` into its `style`. `FormaTextBox`, `FormaPasswordBox`, `FormaFilePicker`, `FormaSortableList` and `FormaRichTextSelectionBar` render none of the classes their modules hold into their `class`. `FormaRichTextGutter` renders each row `hidden` as it creates it and never patches it, so no render meets the module's holding; rendering no `hidden` would settle it outright.
- **Holds a class, a style property or an attribute with `keep = true`.** The module runs `ownClass({ name, keep = true })`, `ownStyle({ property, value, keep = true })` or `ownAttribute({ name, value, keep = true })` through `env.domql` ([D54]), updating it as its value changes. This covers the anchored popover's `left` and `top`, the autofill mark, the file drop zone's active class, the reorder interaction's classes, the frame's `is-dragging`, the selection bar's active class and the gutter rows' `hidden`.
  - Blazor rewrites a whole `class` or `style` attribute as it renders, and DOMQL writes the held value again before paint ([D63]).
  - A second render soon after is a patch too, and is undone as the first was. Blazor writes an attribute only as its render changes it, so it never writes the value it wrote just before, and DOMQL yields only to a writer that restores its own value in answer ([D83]).
  - So a value a module writes once per gesture, as the frame's `is-dragging`, the reorder classes, the file drop zone's active class and the autofill mark are, survives a component that renders its class twice in quick succession. DOMQL's Chromium test renders the element again a frame after DOMQL's correction, then again, and the class and the property stand. In a Blazor Web App of its own, a component wrote its whole `class` with `setAttribute`, only as its rendered value changed. A press's `pointerdown` and `focusin` renders landed in one frame. With a focus handler that awaited 20 or 40 ms, before [D83] the held `is-dragging` was lost in 8 to 20 of 20 presses, and after it in none.
- **Commits geometry, then hands it over.** `Frame.js` holds `left`, `top`, `width` and `height` with `keep = true` and `restore = false` while a gesture or a key moves the frame, and delivers the placement it ends with, naming its drag. The hand-over follows the design's [frames](dom-watcher-engine-design.md#frames):
  - The component whose render writes the box's geometry, `FormaFrame` or the `FormaWindow` presenting it, runs the behavior's `commit` command with the accepted placement. It does so from `OnAfterRenderAsync` of the render carrying that placement, which Blazor calls only once the browser has applied the render.
  - `commit` runs for the owner `FormaFrame` ran `placement` for, on the frame's element. A command reaches only the instance of its own owner on its own receiver ([D48]), so `FormaWindow` commits through its frame, or with the frame's owner.
  - `commit` writes the placement into the holdings and ends them, so the release exposes the placement Forma accepted, rejected and adjusted ones included.
  - The occurrence's acknowledgment ([D50]) can't serve here. It carries no value, and a placement Forma keeps unchanged renders nothing, so ending the holdings would leave the drag's geometry, and restoring would undo an accepted one.
  - A `commit` names its drag, the one its placement result carries. A drag that starts while an earlier drag's `commit` is pending starts its own holdings and then ends the earlier drag's. Ending a holder that isn't the latest writes nothing, so the late `commit` finds nothing to release, and no order of arrival brings the earlier geometry back.
  - The frame places its box itself too. As it starts, it centers an unplaced box that has bounds, and as the window or its bounds resize, it clamps a placed box, reporting each. Each is held and handed over as a drag's placement is, under an identity of its own.
  - While the frame is maximized it fills its bounds and reports nothing. That geometry is held only while `is-maximized` stands, and the holding ends as the class goes, since one kept past it undoes the render that writes the placement back.
- **Writes a rendered text in place.** `CharacterCount.mjs` writes the count through `ownText({ value, restore = false })`, into the text node Blazor rendered, so ending it leaves the count as it stands, as today's script does. It needs no `keep`: Blazor writes that node only with the committed count, which then equals the field's.
- **Measures an update each frame.** The anchored popover updates two owned writes as it follows its anchor, and each update prepares its request again. Step 7 of the [implementation plan](dom-watcher-engine-implementation-plan.md) measures that against today's direct writes: the same popover following the same moving anchor, with the time spent updating and the frames missed recorded on every host, the Android web view included. In Chromium two owned writes updated each frame cost about 25 µs, 0.15% of a frame, once the positions are bound as numbers and the unit written in the text, `value = $"{left}px"`. A bound string is part of what a query is resolved for, so a new string each update is resolved anew, about 20 µs more ([D84]). Step 7 measures the Android web view.

### Timing

DOMQL's [timing](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-timing): T1 to T3, each implemented.

| | Capability | Decision | Used by | Also |
| --- | --- | --- | --- | --- |
| T1 | An `immediate` watch's handlers in the mutation's microtask checkpoint | [D62] | — | None: focus recovery runs in its own observer, or through `env.observe` |
| T2 | A `frame` watch's handlers inside the animation frame, after the frame's writes, owned writes kept among them | [D63] | The owned writes kept beside Blazor's render ([A10](#api-and-host)) | Modules writing under `env.frame` (C5) |
| T3 | Executions carried out in one task keep their order | [D64] | `Dialog.js`: opening, then focusing | `AnchoredPopover.js`, if its `close` weren't a command |

### Corrections to DOMQL's definitions

The survey found 23 of DOMQL's definitions incomplete or wrong: V5, V9, V17, E2, E9, O2, X2, X3, X5, X7, C1, C3, C5, C7, C8, C10, A1, A2, A3, A5, A7, A9 and A10. DOMQL adopted each but A1's, in the decision the tables above give for the item.

A1's correction waits with the C# half, whose design takes it up ([D46]):

- **A host that keeps its own transport** (F1) takes over delivery and recovery. The C# half's outline queues every change after the baseline and rebuilds from a fresh watch on uncertainty, while Forma delivers the latest state alone with one delivery in flight, sends occurrences in order and never coalesces them, and refreshes established watches on reconnection. So such a host uses the typed queries and records without the adapter's synchronization.
- **A result maps onto records** a field written first discriminates, as the router's `InputInfo` is by `family`.

#### Enhanced navigation

Blazor's enhanced navigation merges the new page into the document in place, and in a Blazor Web App of its own, DOMQL followed every merge:

- **Elements are kept by their place.** The nav, its links, a shared element and a list's first item stayed the same elements, their attributes and text written anew. An element in the same place can stand for another: a list's second item took the next page's id and text, so what a host binds to an element it identifies by what it reads of it, never by its identity alone.
- **Each watch reports once,** after `enhancedload`. An `immediate` watch reports in the merge's own task, before `enhancednavigationend`, and a `frame` watch a frame later.
- **An execution on an element the merge removes ends a frame later,** where its caller watches `attached` and disposes it ([D74]); nothing restarts on the element a return creates.
- **DOMQL loads once** for the document, a module script in the layout surviving every enhanced navigation.

#### The browser transfer and the edit boundary

Forma's [browser transfer](browser-transfer-design.md) carries every value a reader can make large from the browser to .NET, inline within a fixed budget and as a stream beyond it, and holds an action back until the edits before it are applied. It is the host layer's, so DOMQL changes nothing for it, and every converted script keeps it:

| Part | Under DOMQL | Rests on |
| --- | --- | --- |
| The transfer | The host layer sends each snapshot, occurrence and answer DOMQL hands it through the transfer: the DOM watcher's states, the editor's activations, the data view's range of tokens, the menu's document order, a taken text and a composed print document | — |
| A text field's edits | A behavior of `FormaText` on the field, delivering `{ revision, start, removed, inserted }` to a source as its own `input` listener computes each, and offering its revision as state a member reads. .NET's own change reaches the field through a command that writes it and moves the revision on. The browser holds the text, so Blazor renders no `value` into the field | Occurrences ([D49]), in order ([D69]), acknowledged ([D50]); state ([D52]); commands ([D48]) |
| The edit boundary | At a participating press or submit, inside its dispatch, the host layer reads the revisions in the form and runs each field's flush command, which delivers its debounced edit at once. It then waits until C# has applied every edit through those revisions, which each occurrence's acknowledgment tells it, before the action is validated. A failed or refused edit stops the action | Commands within the call ([D48], [D64]); acknowledgment ([D50]) |
| The order | Edits are sent as they happen, never held behind an earlier edit's acknowledgment nor coalesced, and C# applies them in revision order, so an edit that arrives late, behind a newer one, is dropped | [D69] |

A Blazor Web App of its own showed why the boundary is needed, Server and WebAssembly alike. A flush sent at once on blur reached .NET before the button's `@onclick` in every run, but a handler that awaited before applying it let the click run first and read the old text. A flush still in flight finished after a newer one, so an older edit could land last, and a queue that waited for each acknowledgment let the click overtake the flush outright.

### Additions considered and not proposed

| Addition | Why not | Script |
| --- | --- | --- |
| A `type` member on every event type | Needed only if the router's occurrences reused DOMQL's event types; its module declares its own `Input` type | `InputRouter.js` |
| `Element.ancestors({ within })` | Only a query form of the composer's selection state would use it, and that state stays the module's | `SelectionInspector.mjs` |
| Session storage, `popstate`, the Navigation API's `navigate`, a link's resolved URL | Only a decomposed reading place would use them; this document keeps it one module, whose note must be written inside the click | `ReadingPlace.js` |
| An invalidation when web fonts load | `resize` reports a font load that changes a box already; only a measurement under a transient class misses it, as today's script does, and the measuring observation type can listen to `document.fonts` itself | `ColumnFit.js` |

## The scripts, one by one

Each row says who uses the script, what it becomes, what of DOMQL it rests on, and the step that converts it. Nothing a row rests on is missing from DOMQL:

- **The C# half** waits for its design (A1, [D46]), so Forma's host layer carries every snapshot, occurrence and execution answer to C#.
- **Every Forma module** is loaded through the loader the host layer registers for it, before a query that imports it (A4, [D68]), so no row lists A4.

Line counts are the script's at the baseline. Examples leave out the import of the Forma module they use, since an import takes a line of its own (NEXO1044).

| Area | Folder | Scripts | Lines |
| --- | --- | --- | --- |
| [The DOM watcher and engine foundation](#the-dom-watcher-and-engine-foundation) | Browser.Interop `scripts/` | 12 | 930 |
| [Text](#text) | Browser.Interop `scripts/text/` | 4 | 360 |
| [Scrolling](#scrolling) | Browser.Interop `scripts/scrolling/` | 5 | 486 |
| [Focus](#focus) | Browser.Interop `scripts/focus/` | 6 | 512 |
| [Behaviors](#behaviors) | Browser.Interop `scripts/behaviors/` | 10 | 1,151 |
| [Components](#components) | Browser.Interop `scripts/components/` | 5 | 871 |
| [Operations](#operations) | Browser.Interop `scripts/operations/` | 11 | 388 |
| [The JavaScript interop foundation](#the-javascript-interop-foundation) | JavaScript.Interop `scripts/` | 7 | 283 |
| [Culture](#culture) | Culture.Browser.Interop `scripts/` | 1 | 676 |
| [Input](#input) | Input.Interop `scripts/` | 2 | 1,300 |
| [The composer](#the-composer) | `Forma.Blazor.Composer` | 15 | 2,745 |
| [The data view, the explorer and the Showcase](#the-data-view-the-explorer-and-the-showcase) | `Forma.Blazor.DataView`, `.Explorer`, `.Showcase` | 6 | 1,090 |
| Total | | 84 | 10,792 |

What they become, counted once each:

| Becomes | Scripts | Which |
| --- | --- | --- |
| A query | 1 | `DocumentOrder` |
| A watch | 11 | `ResizeObserverPool`, `IntersectionObserverPool`, `ScrollStateObserver`, `IntersectionStateObserver`, `FocusStateObserver`, `TextSelectionObserver`, `Virtualize`, `ColumnFit`, `GridColumns`, `HeightReservation`, `Breakpoint` |
| A subscription | 1 | `InputInspector` |
| A once execution | 11 | `Scroll`, `Dialog`, `Asset`, `CheckBox`, `Clipboard`, `Download`, `Embedding`, `Playback`, `Print`, `Share`, `TextEntry` |
| A continuous execution | 19 | `TextEdits`, `ScrollInteraction`, `FocusInteraction`, `AnchoredPopover`, `AutofillTracker`, `DropZone`, `FileDropZone`, `FilePreview`, `PanGesture`, `ReorderInteraction`, `Wheel`, `Frame`, `Splitter`, `Window`, `RichTextEditor`, `BlockAlignment`, `SelectionFollower`, `SelectionInteraction`, `ReadingPlace` |
| A Forma module's implementation | 24 | `CharacterCount`, `TextSources`, `ScrollContainer`, `ScrollButtons`, `FocusSurface`, `TabOrder`, `RovingFocus`, `FocusRecovery`, `DragGesture`, `Pointer`, `CultureCoordinator`, `InputRouter`, and the twelve modules of the rich-text editor |
| Forma's host layer | 12 | The ten scripts of the foundation but its two pools, `Owner` and `Transfer` |
| Stays as it is | 5 | `Controls`, `Direction`, `FrameScheduler`, `Sanitizer`, `Scrolling` |

The culture initializer outside `scripts/` stays as it is too, which makes 85 files in all.

### The DOM watcher and engine foundation

`src/Framework/Forma.Framework.Browser.Interop/wwwroot/scripts/`, the bridge between C# and the browser that steps 1 and 3 built. Most of it becomes Forma's host layer around DOMQL (F1). Its pools retire into DOMQL's own observations. Every row moves onto the request model at step 5.

| Script | Lines | Used by | Becomes | Rests on |
| --- | --- | --- | --- | --- |
| `DomWatcher.js` | 47 | C#'s `DomWatcher`: `FormaFreezePane`, `FormaExplorerContentArea`, `FormaEmojiPicker`, `FormaDateTimePicker` | **Host layer** over `watch`, its `readyAsync` baseline, `refreshAsync` and `signal`. `refresh`, after a sentinel's load, and `refreshAll`, after a reconnection, each deliver `watch.snapshot` once the refresh settles, since a refresh whose result equals the last raises no event. Each refresh can resample, since an observation that takes no new sample is read as it stands | A new sample on a sentinel's refresh ([D66]) |
| `DomEngine.js` | 76 | C#'s `DomEngine`: the freeze pane, the focus capabilities and the text fields' edits | **Host layer** over `executeAsync` and `run`, an interaction's commands among them; `scroll.step` becomes `scrollStep`. An execution that declares routes runs once `configureInput` has, as `DomInteraction` awaits the input policy before such an attach today | The owner ([D47]) |
| `DomWatcher.mjs` | 102 | The watcher's entry | **Host layer**: tokens, generations, and one `StateDelivery` per watch, fed by the `"change"` events after its first, each state crossing through the [browser transfer](browser-transfer-design.md), focus's every state sent as it is measured. The first event is the baseline `readyAsync` answers, which the attach returns: every listener's first `"change"` is the ready state, raised within the evaluation for one subscribed before it, and in a microtask after `subscribe` for one subscribed after ([DOMQL's test](https://github.com/FormaUI/DOMQL/blob/main/tests/dom/Watch.browser.js)). Sending both loads one extra page. A new generation starts a new watch with its bindings, and disposes the old one once the new one is ready | Change sets that compose, once they cross ([D65]) |
| `DomEngine.mjs` | 115 | The engine's entry | **Host layer** over `run`, the occurrences a behavior reports to its owner, and its commands, as the edits' `write`, `resend` and `flush`: a new generation updates the running execution, `update(bindings)`, which every behavior answers, and disposes it and runs it anew where the update fails. Since an update replaces the bindings whole, the host writes every option as a binding, as `wheel = wheel`, and declares the type of a structured one, as `{ buttons: 'FormaScrolling.ScrollButtons?' }`, which every update keeps | The owner ([D47]); declared binding types ([D82]) |
| `DomSession.mjs` | 61 | The entries | **Host layer**: an owner's elements, watcher and engine, released in order through one `AbortController` per part | — |
| `DomSessionRegistry.mjs` | 54 | The entries | **Host layer**: sessions by runtime key | — |
| `DomElement.mjs` | 80 | The session | **Host layer** for executions, ending each once a watch of `it.attached`, accepting partial observation, reads false; a watch follows a detached element back by itself, and `attached` catches the zero-sized removal its resize check misses | A watch of `attached`, where `run({ anchor })` is declined ([D74]) |
| `DomElements.mjs` | 29 | The session | **Host layer**, retiring with `DomElement` | — |
| `RegistrationTable.mjs` | 76 | The watcher and the engine | **Host layer**: tokens and generations; its resources become DOMQL handles | — |
| `StateDelivery.mjs` | 123 | The watcher | **Host layer**: one delivery in flight with the latest waiting, or, for focus, every state sent in the order it is measured, each as it is queued; suspension and resumption | Change sets coalesced by concatenation, or the latest snapshot whole ([D65]) |
| `ResizeObserverPool.mjs` | 68 | `DomElement`, `ScrollStateObserver` | **Watch**: retires into DOMQL's `resize` observation, shared by target and box, which `scroll` holds on the track and, through the `children` target, on its children; `DomElement`'s removal check retires into the `attachment` observation `attached` holds | [D11] |
| `IntersectionObserverPool.mjs` | 99 | `IntersectionStateObserver` | **Watch**: retires into `intersects({ root, margin })`, one observation for each element, root and margin, each with a native observer of its own, where the pool shares one native observer among every element of a root, margin and threshold; Forma uses threshold 0 alone | Its `remeasure` as `refreshAsync({ resample: true })` ([D66]) |

C# reads a whole state record from each observation today; no change set crosses yet. Once change sets cross, the host coalesces those waiting behind the delivery in flight by concatenating them, or sends the latest snapshot whole as one root replace ([D65]).

Until the C# half is designed ([D46]), this host layer carries everything to C#, and loads what each query needs:

- **Each watch's snapshots,** one delivery in flight, the latest waiting.
- **Each behavior's occurrences,** in order, through a bounded queue that never coalesces and fails on overflow, as Forma's [design](dom-watcher-engine-design.md) has it. DOMQL hands them over in the order they happen, across handles and owners ([D69]).
- **Each execution's answer.** A command that finds nothing running rejects with `DOMQL5016`, which the host answers as today's "the call didn't run".
- **Each Forma module, loaded first.** The host registers each module's loader with `domql.registerModuleLoader`, and awaits `domql.loadModulesAsync(query)` before carrying out a query that imports one ([D68]). A query it forgot to load for is refused with `DOMQL3007`. A module from outside `Forma.Framework.Browser.Interop`, as `FormaDataView`, `FormaRichText` or the Showcase's, reaches it with a loader by the module's name, where each C# wrapper names its script's path today; how a package or an app registers that loader is the host layer's to settle.

### Text

`scripts/text/`, added with `4b2b0ac4` for a text field whose text reaches C# as its edit commits, and since the [browser transfer](browser-transfer-design.md) a field's edits, the text they read and write, and its selection.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `CharacterCount.mjs` | 43 | `TextEdits`, for a field that counts its characters, `FormaTextArea` among them | **Forma module's implementation**, part of the edits' behavior, `field:edits({ count = { output = "count-id", maxLength = 200 } })`, writing the count into the output's rendered text node through `ownText({ value, restore = false })` as the reader types, with no crossing, so ending it leaves the count as it stands, as today's script does | A null `maxLength` passed as it stands ([D59]); `ownText` beside Blazor's render ([D70]) | 5 |
| `TextEdits.mjs` | 173 | C#'s `DomTextEdits`, through `DomEngine`: `FormaTextBox`, `FormaTextArea`, `FormaPasswordBox`, `FormaSearchBox`, `FormaNumberInput`, `FormaTimeBox`, `FormaPlainTextEditor`, `FormaDataGrid`'s editing, and the rich-text editor's canvas | **Continuous execution** `field:edits({ sendsOnInput = true, count = null, text = text, revision = revision })` of `FormaText`, delivering each edit, `{ revision, start, removed, inserted, isCommit }`, to the source `field:edits.changes()` as its own `input` and `change` listeners compute it, in order and never coalesced, and offering its revision as state a member reads. Its commands: `write({ text })`, .NET's own text written into the field, answering the revision it then stands at; `resend({ revision })`, the whole text delivered as a replacement where .NET's copy stands at another revision; and `flush()`, its pending edit committed at once, answering its revision. A field drawn anew goes on from the revision the module keeps for its registration, per document, so its revisions number every registration of it | Occurrences ([D49]), in order ([D69]), acknowledged ([D50]); state ([D52]); commands within the call ([D48], [D64]); state per document ([D57]) | 5 |
| `TextSources.mjs` | 75 | `TextEdits`, and the rich-text editor, which provides its canvas | **Forma module's implementation**, part of the edits' behavior: the text an element's edits read and write, a field's value or the source an editor's script provides for an element it owns, reached by the plain class the editor and the edits share (F2) | — | 5 |
| `TextSelectionObserver.mjs` | 69 | C#'s `DomText` selection: `FormaPlainTextEditor` | **Watch** `field.selection { start = it.start, end = it.end }`, DOMQL's own `selection`, observed through the field's `selectionchange` and `select` and the document's `selectionchange` while the field holds the focus, as this script listens | DOMQL's `selection` ([D20]) | 5 |

### Scrolling

`scripts/scrolling/`, converted onto the foundation by step 1.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `ScrollContainer.mjs` | 167 | The scrolling classes, `DomEngine.js` | **Forma module's implementation**: the measurement and stepping class inside `FormaScrolling`'s handlers (F2). Its stop-aware step is the once execution `viewport:scrollStep({ direction = "forward", axis = "inline", stops = …, frozen = … })`, answering a `Boolean`, a policy ([D1]) that scrolls natively | Null `stops` and `frozen` passed as they stand ([D59]) | 5 |
| `ScrollStateObserver.mjs` | 109 | `FormaFreezePane` | **Watch**, accepting partial observation, below, its corner extent among its fields. A detached track gives null where today it keeps its last state, and each frozen part's own size is followed, which today's script misses. C#'s `ScrollState` takes `bool?` for `CanScrollBack` and `CanScrollForward`, which the watch answers as `Boolean?`, null for a detached track | `scroll` ([D9]) over the `children` target ([D11]); the computed `direction`, read through `style`; selectors observed for what they read ([D26]); the tolerance ([D8]) | 5 |
| `IntersectionStateObserver.mjs` | 75 | `FormaExplorerContentArea`, `FormaEmojiPicker` | **Watch** `sentinel { isIntersecting = it.intersects({ root = panel, margin = 200 }) }`, its first state the browser's own report, as step 5 asks. After each load, the host refreshes it with `refreshAsync({ resample: true })`, so it reads where the load left the sentinel, and once the refresh settles hands C# `watch.snapshot`, whether or not it changed. A refresh whose result equals the last raises no `"change"` ([D66]), and `FormaExplorerContentArea` and `FormaEmojiPicker` load their next page only on a state they are sent, as today's `refresh` sends one by forgetting the last. The result is `Boolean?`, null only until the browser first reports the sentinel, which the host passes on none of; a detached or hidden sentinel or panel reads false, so C#'s `IntersectionState` keeps its `bool`. In Chromium this loop fills a list page by page, stops once a load leaves the sentinel out of reach, and loads again as a scroll brings it near, where a loop loading only on a `"change"` stalls after the first page ([DOMQL's test](https://github.com/FormaUI/DOMQL/blob/main/tests/dom/Watch.browser.js)) | A new sample on a refresh ([D66]); the snapshot read once it settles, an equal result raising no event | 5 |
| `ScrollInteraction.mjs` | 54 | `FormaFreezePane` | **Continuous execution** `viewport:scrollInteraction({ axis = axis, stops = stops, frozen = frozen, wheel = wheel, buttons = buttons })`, its wheel route on the input router, `wheel` as `"freeze-pane.scroll"` or null. Its options are bound, so an update carries a new generation's. `buttons`, its `container` the freeze pane's root element, is bound with its declared type, `FormaScrolling.ScrollButtons?`, since data typed from itself is no module's structure | Handling inside the dispatch ([D51]); null options ([D59]); a nested option holding an element ([D60]), bound with a declared type ([D82]) | 5 |
| `ScrollButtons.mjs` | 81 | `ScrollInteraction` | **Forma module's implementation**, part of `scrollInteraction`: the arrow buttons found among the children and routed as clicks | `env.observe` ([D53]) or a query through `env.domql` ([D54]), if it chooses | 5 |

The freeze pane's state along the inline axis, in either direction:

```domql
viewport {
    let along = it.scroll?.inline,
    let rtl = it.style["direction"] == "rtl",
    let width = it.size?.width ?? 0,
    let frozen = it.all(".forma-freeze-pane-frozen"),
    let reach = frozen.max(if rtl then width - (it.rectRelativeTo(viewport)?.left ?? width) else it.rectRelativeTo(viewport)?.right end) ?? 0,
    canScrollBack = if let s = along then s.offset > 1 else null end,
    canScrollForward = if let s = along then s.extent - s.viewport - s.offset > 1 else null end,
    frozenExtent = reach.round(),
    cornerExtent = if reach > 0 then frozen[0]?.rectRelativeTo(viewport)?.bottom?.round() ?? 0 else 0 end,
    viewportExtent = if showsEdge then it.clientSize?.width.round() else null end
}
```

- **The block axis** reads `block`, `bottom` for the extent, the inline end for the corner, and `clientSize?.height`.
- **`showsEdge`** is a binding, true where the freeze pane measures its viewport's extent, as `viewportExtent` asks of the script today.
- **The direction is the computed one,** `it.style["direction"]`, as the script reads it and as the browser's scroll offsets follow it. `it.direction` is what `dir` decides, and Forma's stylesheets set `direction: ltr` inside right-to-left pages, as the calendar does.
- **The corner is the first frozen part's,** the header's cell, which sticks at the block start. The script takes the smallest block end among all frozen parts, so a block scroll sliding the body's frozen cells beneath the header shrinks the corner and then makes it negative (see the findings). The reach is tested before it is rounded, as the script tests it.
- **In Chromium the query gives what the script's arithmetic gives** at the start, the middle and the end of the inline scroll and through block scrolls, in LTR and RTL, with and without classic scrollbars, with a border and a fractional frozen width, but for the corner the script loses under a block scroll. DOMQL's [geometry tests](https://github.com/FormaUI/DOMQL/blob/main/tests/dom/Geometry.browser.js) hold the members it rests on in such a layout.
- **Under RTL** the frozen extent is measured from the right edge. `rectRelativeTo` measures from the viewport's border box, so the inline end is that box's width less the part's `left`.
- **The direction** is what `dir` decides along the ancestry, which follows a change of `<html dir>` ([D13]). Today's script reads the computed `direction`, which agrees wherever no stylesheet sets `direction`, as none does around the freeze pane.

### Focus

`scripts/focus/`, converted onto the foundation by step 4. Its decisions are made inside key handlers, so all of it is `FormaFocus`'s, with the classes other modules compose (F2).

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `FocusInteraction.mjs` | 45 | `FormaToolbar`, `FormaComposer`, `FormaDataTree` | **Continuous execution** `toolbar:focusInteraction({ roving = "horizontal", wrap = false, order = [], heir = null })` | Handling inside the dispatch ([D51]); the null `roving` and `heir` C# sends, passed as they stand ([D59]) | 5 |
| `FocusSurface.mjs` | 82 | `FocusInteraction`, `TabOrder`, `RovingFocus`, `Dialog.js`, `Window.js`, `AnchoredPopover.js`, `SelectionFollower.js`, each synchronously | **Forma module's implementation**: a plain class (F2) | —; as a query, `focusable` ([D14]) and `contains` ([D22]) | 5 |
| `TabOrder.mjs` | 66 | `FocusInteraction`, `AnchoredPopover.js`, `Window.js`, `SelectionFollower.js` | **Forma module's implementation**: Tab and Shift+Tab routed, decided before the default, and the order `focusFirst` takes the first of, which `Dialog.js` focuses as it opens | [D51] | 5 |
| `RovingFocus.mjs` | 126 | `FormaToolbar` | **Forma module's implementation**: one Tab stop, its `tabindex` writes restored on dispose | [D51]. Should a toolbar's child render a `tabindex`, `ownAttribute({ name = "tabindex", value = "-1", keep = true })` parks and restores it as its map does ([D38], [D70]); `env.observe` if it chooses ([D53]) | 5 |
| `FocusRecovery.mjs` | 57 | `FormaDataTree` | **Forma module's implementation**: focus moved to the heir at mutation delivery | `env.observe`, notifying a mutation at its delivery, if it chooses ([D53]) | 5 |
| `FocusStateObserver.mjs` | 136 | `FormaDateTimePicker`, `FormaTimeBox`, `FormaPagination`, `FormaWindow`, `FormaDataTree`, `FormaExplorerSearch` | **Watch** `field { isWithin = it:readerWithin, item = it:focusedItem({ itemAttribute = "data-key" }) }` under `immediate`, maintained members `FormaFocus` keeps with its own observation type, every state the host layer sends as it is measured, so a press or a key never acts on the focus as it stood before: an immediate watch reports within the microtask after the focus moves, ahead of the next task ([DOMQL's order](https://github.com/FormaUI/DOMQL/blob/main/spec/api.md#order-across-handles)). `item` names the surface's item holding the focus by its attribute, or null. `isWithin` is nullable as a member reading maintained values must be (NEXO4071), and reads null only until its first sample, which the host passes on none of, and its implementation answers a Boolean from then on, so C#'s `FocusState` keeps its `bool`. Forma's "within" differs from `focusWithin` for a press inside that drops focus to the page, focus leaving the window, and focus removed with its element | — | 5 |

DOMQL serves `readerWithin` today: a module's observation type with `notify`, `stop` and `sample`, and a maintained member awaited in a watch's first snapshot. Frame coalescing drops a leave and a return within one frame, as Forma's design wants.

### Behaviors

`scripts/behaviors/`, which steps 5, 6, 7, 9 and 11 convert. Most become a Forma module's continuous execution, and those that report deliver occurrences through their module's event sources ([D49]).

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `AnchoredPopover.js` | 145 | `FormaPopover` | **Continuous execution** `popover:anchoredPlacement({ anchor = anchor, keepsFocus = false, align = "auto" })` of `FormaPlacement`, with the commands `popover:open({ trigger = trigger })` and `popover:close({ returnsFocus = true })`. `close` tests focus before hiding, so it can return focus to the trigger, and C# awaits it before the content goes. The module shows, hides and focuses natively, as `showPopover()`, `hidePopover()` ([D36]) and `focus({ preventScroll = true })` ([D34]) would, and holds its inline `left` and `top` through owned writes ([above](#owned-writes-beside-blazors-render)). Tab is held by its own `TabOrder` with `wrap`, as `focusInteraction({ wrap = true })` holds it. DOMQL serves `popoverOpen`, `focusWithin`, and `anchor.rect`, which follows its scroll containers and the window | Commands ([D48]), the owner ([D47]), a null trigger ([D59]); owned writes beside Blazor's render ([D70]); `env.observe` and `env.frame` if it chooses ([D53]) | 7 |
| `AutofillTracker.js` | 36 | `FormaTextBox`, `FormaPasswordBox` | **Continuous execution** `field:autofillMark({ filledClass = "has-value" })` of `FormaText`, local as today, holding its class with `ownClass({ name = "has-value", keep = true })`. A CSS rule on `:autofill` could retire it outright, since only CSS reads the class | Owned writes beside Blazor's render ([D70]). As a watch, `autofilled` would miss a fill as the page loads ([D17]), which the module's `animationstart` listener hears | 11 |
| `DragGesture.js` | 326 | `PanGesture`, `ReorderInteraction`, `Frame`, `Splitter`, `SelectionFollower` | **Forma module's implementation**: gesture mechanics `FormaGestures` holds and other modules compose, canceling, capturing and claiming the click inside the native dispatch ([D2], F2) | [D51]; its edge scrolling on `FrameScheduler`, as a plain class that works without DOMQL (F2), as C5's row says | 9 |
| `DropZone.js` | 126 | `FormaExplorerSidebar`; `FileDropZone` | **Continuous execution** `zone:dropZone({ action = "explorer.drop", dropEffect = "move", targets = "[data-drop-target]" })` of `FormaDragDrop`, offering `zone:drops() { key = it.target?["data-drop-key"] }`, projected inside the drop's dispatch, so it reads the element as it stands | Occurrences ([D49]), the owner ([D47]), [D51], null options ([D59]). No owned write for the sidebar, which gives no active class. `DropZoneOptions` still offers `ActiveClass` and `Accepts`, so the behavior takes `accepts` and holds an `activeClass` with `ownClass`, `keep = true`, as `fileDrop` does, or the options lose them | 9 |
| `FileDropZone.js` | 47 | `FormaFilePicker`, and through it `FormaImagePicker` | **Continuous execution** `zone:fileDrop({ action = "file-picker.drop", activeClass = … })`, assigning the files to the input inside the drop, while the transfer is readable; C# hears `InputFile`'s `change`. Its active class is held with `ownClass`, `keep = true`, beside the label's rendered `class` | [D51]; owned writes beside Blazor's render ([D70]) | 9 |
| `FilePreview.js` | 86 | `FormaImagePicker` | **Continuous execution** `container:filePreview()` of `FormaFiles`, owning the object URLs, with the command `container:show({ names = names })`, since a new selection under the same names must show again. Where none runs, the command rejects with `DOMQL5016`, and C# shows again at its next render. Built in instead, each thumbnail could hold `thumb.ownFileSource({ input = input, index = input.files?.indexWhere(it.name == name) })`, skipped where no file has the name, where today's script removes the `src`, since C# names only the accepted files | Commands ([D48]), the owner ([D47]); built in instead, [D44] | 9 |
| `PanGesture.js` | 68 | `FormaFileViewer` | **Continuous execution** `frame:pan({ action = "file-viewer.pan", whileClass = "zoomed" })` of `FormaScrolling`; the class is data tested at the press | [D51] | 9 |
| `ReorderInteraction.js` | 220 | `FormaSortableList` | **Continuous execution** `list:reorder({ dragFromItem = true })`, offering `list:reorderMoves() { from = it.from, to = it.to, after = it.after }`; C# changes the order. Its classes, on the list and on the items it marks, are held with `ownClass`, `keep = true`, beside their rendered `class` | Occurrences ([D49]), the owner ([D47]), [D51]; owned writes beside Blazor's render ([D70]) | 9 |
| `Scroll.js` | 13 | `FormaFileViewer`, after a zoom | **Once execution**, built in: `frame.scrollTo({ inline = ((frame.scroll?.inline.extent ?? 0) - (frame.scroll?.inline.viewport ?? 0)) / 2, block = ((frame.scroll?.block.extent ?? 0) - (frame.scroll?.block.viewport ?? 0)) / 2 })`, answering whether it moved once the scroll ends, at once where it is instant | `scroll` ([D9]); logical scrolls ([D35]), an instant one answering at once ([D67]) | 5 |
| `Wheel.js` | 84 | `FormaPagination`, `FormaFileViewer` | **Continuous execution** `surface:wheelTurns({ action = "pagination.page", modifier = null })` of `FormaScrolling`, offering `surface:wheelDirections() { direction = it.direction }`, grouping notches and trackpad gestures in its instance | Occurrences ([D49]), the owner ([D47]), [D51], a null `modifier` ([D59]) | 6 |

### Components

`scripts/components/`, the browser sides of `FormaDialog`, `FormaFrame`, `FormaSplitter`, `FormaVirtualize` and `FormaWindow`.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `Dialog.js` | 77 | `FormaDialog` | **Once executions**, built in, which Forma's host layer carries out in one task, so they keep their order: `dialog.showModal()`, answering whether it opened it: false for a dialog open modally already, which `FormaDialog` counts as open, since today's `open` returns without reopening it, while a dialog open but not modal fails it. Then `dialog:focusFirst({ part = part })` of `FormaFocus`, `part` bound to the selector `focusWithin` gives, moving the focus to the first element Tab reaches within it, as `TabOrder` orders them, since Tab's order is no test of `tabindex` DOMQL's `focusable` could join ([D96]); it answers whether focus landed, null where the part or a control is missing. `CloseAsync` is `dialog.close()`, answering false for a dialog closed already, which `RequestCloseAsync` reads as a close that never ran, closing its own state, harmlessly. The scrim close is an opt-in continuous execution of `FormaPresentation`, `dialog:scrimClose()`, run only where `FormaDialog` closes on a scrim click. It is a click route on the input router that compares the click's original target with the dialog element, as the design's [native dialogs](dom-watcher-engine-design.md#native-dialogs) state. A click on the dialog's own box closes it inside the click, as today; a click on a descendant, empty space in a content wrapper included, leaves it open. Blazor's `@onclose` follows every close, as today | [D14], [D34], [D36], [D64]. A reaction ([D71]) doesn't fit the scrim close: it runs for every click its subscription hears | 10 |
| `Frame.js` | 342 | `FormaFrame`, inside `FormaChrome` and `FormaWindow` | **Continuous execution** `frame:placement({ box = "self", dragRegion = …, handle = …, step = 10, bounds = null })` of `FormaPlacement`, offering `frame:placementChanges() { drag = it.drag, left = it.left, top = it.top, width = it.width, height = it.height }`, with the commands `measure` and `commit({ drag, left, top, width, height })`, since a `commit` names its drag. C# subscribes to `placementChanges` before it runs `placement`, since the behavior reports as it starts, centering an unplaced box, and an occurrence no subscription hears is lost (`DOMQL5017`). Its arrow keys become routes. Its size limits stay in the module. It holds its geometry and its `is-dragging` class through owned writes while it moves the frame. The render of `FormaFrame`, or of the `FormaWindow` presenting it, takes the accepted geometry over through `commit` (F3, [above](#owned-writes-beside-blazors-render)) | Occurrences ([D49]), commands ([D48]), the owner ([D47]), [D51], a null `bounds` ([D59]); owned writes beside Blazor's render ([D70]), handed over by `commit`; `env.observe` if it chooses ([D53]) | 10 |
| `Splitter.js` | 75 | `FormaSplitter` | **Continuous execution** `bar:splitterDrag({ axis = "inline", action = "splitter.resize" })` of `FormaGestures`, offering `bar:splitterDrags() { phase = it.phase, travel = it.travel, canceled = it.canceled }`; its once-a-frame travel is the behavior's own, before it delivers | Occurrences in order ([D49], [D69]), the owner ([D47]), [D51]; its frame through `env.frame` ([D53]) or `FrameScheduler` | 9 |
| `Virtualize.js` | 208 | `FormaVirtualize`: the data list, grid, cards and tree, and `FormaTimeBox`'s options | **Watch** of `start:virtualizer({ spacerAttribute = spacer }) { offset = it.offset, viewport = it.viewport, itemSize = it.itemSize }`, a maintained member of a nullable type, `VirtualizerMeasurement?` (NEXO4071), that `FormaVirtualizer` keeps with its own observation type. The watch's first snapshot waits for its first sample, and the snapshot is null while the start spacer is out of the document. Today's script reports nothing there, so C# skips a null snapshot. The observation type holds the measurement history and finds and observes the scroll container. `reveal` and `scrollTo` are once executions of the module, which find the drawn item or the offset's change and scroll instantly, answering at once, as the built-in `scrollIntoView` and `scrollBy` do for an instant scroll. The C# render, scroll and reveal loop stays C# | `env.observe` ([D53]); instant scrolls answering at once ([D35], [D67]) | 8 |
| `Window.js` | 169 | `FormaWindow` | **Continuous execution** `surface:topLayerPresentation({ presented = presented, modal = modal, scrim = scrim })` of `FormaPresentation`, which hears a press inside the window itself and delivers it as an occurrence, so no C# handler takes an event raised in a field it holds, with the commands `raise` and `refocus`, and `present` an update of `presented`. An update replaces every binding, so a change of `modal` or `scrim`, for which `FormaWindow` attaches again today, reaches the same `update`, and the host runs the behavior anew where `update` refuses it. In the module: the scrim shown first, a raise deferred while pressed, focus memory in its instance. The command moving focus back into the window is named `refocus`, apart from DOMQL's `focus()`, which moves focus to an element | Commands ([D48]), the owner ([D47]), [D51], a null `scrim` ([D59]); `topmost` kept per document ([D57]) | 10 |

### Operations

`scripts/operations/`, stateless calls C# makes. Most become built-in executions, converted at step 11 as consumers use them.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `Asset.js` | 134 | The composer's plugin asset loader, `EmojiSpriteLoader`; `Embedding.js` | **Once executions** of `FormaAssets`, module members called qualified, `FormaAssets.loadModule(url)`, `FormaAssets.loadStyle(url)` and `FormaAssets.loadSprite(url)`, answering a `Boolean`, with the per-document cache in the module; `loadScript` serves `Embedding.js` alone | Its cache by `env.document` ([D57]) | 11 |
| `CheckBox.js` | 8 | `FormaCheckBox` | **Once execution**, built in: `checkbox.markIndeterminate()` or `checkbox.clearIndeterminate()`, as the box's state asks, each answering whether it changed it; C# takes either answer as applied, since `false` means it was so already | [D39] | 11 |
| `Clipboard.js` | 11 | `FormaShareMenu`, `FormaRichTextSource` | **Once execution**, built in: `Domql.window.writeToClipboard(text)`, answering `"written"`, `"refused"`, `"unsupported"` or `"failed"`. It is a reaction to the press that asks for it, `copy.on("click") { at = it.timeStamp }`, `text` bound as the subscription starts, so it writes with the press's activation and its answer reaches C# after the click. `FormaRichTextSource`'s copy is a composer toolbar command that C# executes through `RichTextSourceViewPlugin`, of a source that follows the document. Its reaction needs the toolbar button's element, and a subscription made again whenever the source changes, since a reaction's bindings are fixed as its subscription starts; otherwise it stays a once execution after the round trip, as today. `Domql.window.hasClipboard` tells availability | [D40]; a reaction ([D71]) | 11 |
| `DocumentOrder.js` | 23 | `FormaDropDownMenu`, on every arrow key | **Query** `menu { let ordered = it.all("[role^=menuitem]"), places = 0.sequence(items.count).orderBy(ordered.indexOf(items[it])) }`, `menu` bound to the popover's `[role=menu]` element, which `FormaDropDownMenu` takes no reference to today, and `items` as the items' list: each item's place in `items`, in document order, an item outside the menu or bound as null last, in the order given, where today's script places a connected item outside the menu by its document position, a case no menu meets, and only a missing or detached one last. A watch of it would remove the crossing per key | `all` and `indexOf`, where `follows` is declined ([D22]) | 11 |
| `Download.js` | 22 | `FormaRichTextPreview`, `FormaRichTextSource` | **Once execution**, built in: `Domql.window.download({ name = "document.html", content = source, type = "text/html" })`, answering nothing, since the browser reports nothing. A host's native save stays C#'s, through `INativeOperations` | [D40] | 11 |
| `Embedding.js` | 20 | `FormaVideoPlayer` | **Once execution** `Domql.window:activateEmbed(scriptUrl)` of `FormaEmbeds`, answering a `Boolean`, outside the generic contracts | — | 11 |
| `Playback.js` | 13 | `FormaVideoPlayer` | **Once executions**, built in: `video.mute()`, then `video.play()`, in one task. `play()` answers `"playing"`, `"blocked"`, `"aborted"` or `"unsupported"`, which today's `.catch(() => {})` loses. Most browsers play a muted element without the user's activation, though headless Chromium blocks it until the user acts | [D40], [D64] | 11 |
| `Pointer.js` | 15 | `DragGesture.js` alone, since `Pointer.cs` was removed | **Forma module's implementation**: the gesture's own capture and release, which it can inline | —; as a query, `capturePointer` and `releasePointer` ([D37]) | 9 |
| `Print.js` | 112 | `FormaShareMenu`, `FormaRichTextPreview` | **Once executions**: built in, `Domql.window.print()` for the page; of `FormaPrint`, `Domql.window:printDocument({ html = content, title = title, className = "prose" })` for markup, which should sanitize or state that its caller does. `Domql.document:composeDocument({ html = content, title = title, className = "prose" })` and `Domql.document:composePage({ title = title })` answer a standalone document as text, the page's style rules inlined, which `Print.cs` hands a host's native operations to save. That text reaches C# through the [browser transfer](browser-transfer-design.md), since a page can pass the hub's limit | [D40] | 11 |
| `Share.js` | 17 | `FormaShareMenu` | **Once execution**, built in: `Domql.window.requestShare({ title = name, url = address })`, answering `"shared"`, `"canceled"`, `"refused"`, `"invalid"`, `"unsupported"` or `"failed"`, so a cancel is told from a refusal. It is a reaction to the share item's press, so it has the activation a share needs. Availability is `Domql.window.canShare`. A host's native share stays C#'s, and where the host shares natively no reaction is made, since one menu item serves both | [D40]; a reaction ([D71]) | 11 |
| `TextEntry.js` | 13 | `FormaPlainTextEditor`, `FormaPagination` | **Once execution**, built in: `entry.select({ start = 0, end = 4 })`. The entry's text reaches C# as a field's edits, so nothing takes it: `FormaPagination` reads the entry as the edits hold it. `FormaPagination` selects the entry once it has rendered the page it went to, so a reaction to Go's press would select the old entry. The editor's selection is the watch `TextSelectionObserver.mjs` becomes, above, which the editor reads afresh for each command | [D39] | 11 |

### The JavaScript interop foundation

`src/Framework/Forma.Framework.JavaScript.Interop/wwwroot/scripts/`, what other scripts share, with no C# wrapper.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `Controls.js` | 12 | `InputRouter`, `FocusSurface`, `RovingFocus`, `ReorderInteraction`, `SelectionInteraction` | **Stays as it is**, for synchronous use in handlers (F2). In queries, `it.matches(…)` with its selectors, and `focusable` where a script asks whether an element can take focus; Tab's reach is `FormaFocus`'s `TabOrder`, since no test of `tabindex` gives the browser's order ([D96]). `focusable` differs from `Controls.Focusable`, which counts a hidden input, a disabled control holding a `tabindex`, and a box that isn't displayed; `textEditable` asks less than `Controls.Interactive` | In queries, [D14], [D96] | None |
| `Direction.js` | 10 | `AnchoredPopover`, `Scroll`, `RovingFocus`, `ScrollContainer` | **Stays as it is**. In queries, `it.style["direction"]` is the computed direction it reads; `it.direction` is what `dir` decides, which a stylesheet's `direction` doesn't change | In queries, [D13] | None |
| `FrameScheduler.js` | 34 | `DomElement`, `ScrollStateObserver`, `AnchoredPopover`, `DragGesture`, `Splitter`, `BlockAlignment`, `SelectionFollower` | **Stays as it is** for the plain classes that work without DOMQL (F2), the drag gesture's among them. Module code schedules through `env.frame(callback, { phase })`, a frame's writes before its reads, and a frame watch's handlers run inside the frame | For module code, [D53], [D63] | None |
| `Owner.js` | 15 | The reports of ten scripts | **Host layer**: each report becomes a watch's change or a behavior's occurrence, which the host layer carries to C# in order, and `tell` retires with its last caller | Occurrences ([D49]), acknowledged ([D50]), in order across handles ([D69]) | With its callers |
| `Transfer.js` | 46 | Every script answering or reporting a value a reader can make large: the edits, the editor, the data view's range, the menu's document order, a print document | **Host layer**: the transfer itself, which the host layer applies to each snapshot, occurrence and answer it carries to C#, inline within its budget and as a stream beyond it, as [the browser transfer and the edit boundary](#the-browser-transfer-and-the-edit-boundary) set out | — | With its callers |
| `Sanitizer.js` | 130 | The rich-text editor's Word paste | **Stays as it is**: a pure transform of markup | — | None |
| `Scrolling.js` | 36 | `DragGesture`, `InputRouter` | **Stays as it is**, for their synchronous tests. In queries, `scroll` and `scrollContainer({ axis = "block" })`, which finds the nearest box the user can scroll whether or not its content overflows, starting at the element's flat-tree parent, where `nearestScroller` also asks that it overflow and tests the element itself first, as `DragGesture` asks it of its own element | In queries, [D9], [D10] | None |

### Culture

`src/Framework/Forma.Framework.Culture.Browser.Interop/wwwroot/`. `CultureCoordinator.js` replaced `DocumentCultureSynchronizer.js` and `CultureCookie.js` since the earlier survey, and gave each web view of a Blazor Hybrid application a culture of its own in `9ada4608`.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `scripts/CultureCoordinator.js` | 676 | Every .NET runtime on the page, through `CultureCoordinatorRegistration`: a circuit, a WebAssembly runtime, a web view; the culture switch | **Stays outside DOMQL**, its own bridge to each runtime as today, since a runtime's registration precedes its first render, below | — | None |
| `formaui-net.Framework.Culture.Browser.Interop.lib.module.js`, outside `scripts/` | 15 | Blazor | **Stays as it is**: Blazor's initializer, starting the coordinator's document-lifetime work before any request. It loads no DOMQL | — | None |

**Why it stays outside.** A circuit's registration completes before the circuit renders, through `CultureCircuitHandler`, and a WebAssembly runtime registers before it renders, so whatever the registration loads is on the path to the first interactive render. A Blazor Web App of its own measured that path with the registration loading DOMQL and a module shaped as `FormaCulture`, published, in Chromium, cold and warm:

| Render mode | First interactive render without DOMQL → with it, cold, at 1×, 4× and 6× CPU throttling | Warm, 1× |
| --- | --- | --- |
| Server | 72 → 144 ms, 318 → 633 ms, 670 → 1,268 ms | 63 → 105 ms |
| WebAssembly | about 8% more, +46 ms at 1× | +17 ms |

- **Loading DOMQL roughly doubles a circuit's start.** Importing the 456 KB bundle, 110 KB under Brotli, is about three quarters of it; registering the module, the first query, the subscription and `run` the rest.
- **Neither preparing it early nor preloading it gives the time back.** Started in Blazor's initializer, the registration left the critical path, 66 ms down to 9 ms at 1×, but Blazor's own start ran later by about as much, since both run on the page's one thread; at 6× the first render came 81 ms later than without it. `<link rel="modulepreload">`, with the import map's `integrity`, saved 7 to 76 ms on Server and nothing beyond noise on WebAssembly.
- **The module form would gain little for that.** The coordinator reads almost nothing of DOMQL's vocabulary, so the module form gave one transport and one owner model. DOMQL loads instead with the first component that uses it, after the first render.

Its cookies, Blazor's enhanced navigation events, `location` and `history`, its writes to `<html lang dir>`, and a web view's own path stay its own, as today.

**The module form, not adopted while loading DOMQL costs a first render this much,** would have been:

| Today | Becomes |
| --- | --- |
| A runtime's registration | A continuous execution, `Domql.document:cultureRegistration({ runtime = "server", start = start })`, its identity the owner ([D47]). `start` carries what a standalone page's runtime starts the coordinator from, or null, bound with its declared type, `FormaCulture.Start?` ([D82]), its `marker` and `policy` as `Map`s, since a declared dictionary takes no plain object (NEXO2045). A runtime that registers again subscribes, runs the registration again for the same owner, and then disposes the handle it replaces: DOMQL keeps both instances running until it does, and its commands reach the latest ([D48]) |
| The accepted pairs it hands to C# | An event source, `Domql.document:acceptedCultures() { culture = it.culture, uiCulture = it.uiCulture, documentKey = it.documentKey, number = it.number }`, delivered in order and acknowledged once C# applied it ([D50]). The registration delivers the accepted pair as it starts, which a subscription made before `run` hears |
| `request`, `reportLanding`, `follow` | Commands reaching the running registration for their owner, failing with `DOMQL5016` where none runs ([D48]): `Domql.document:requestCulture({ culture = "he-IL", uiCulture = "he-IL", direction = "rtl" })`, answering `"accepted"` or `"canceled"` once the requester has applied the pair; `Domql.document:reportCultureLanding({ token = token, order = order })`; `Domql.document:followCulture({ culture = culture, uiCulture = uiCulture, direction = direction })` |
| The accepted pair | A member of its extension of `Document`, `Domql.document:acceptedCulture`, followed through an observation type of the module's own. Where that type is maintained, the member is declared `FormaCulture.AcceptedPair?`, since DOMQL `1.0.0-beta.2` refuses a member reading maintained values whose result can't be null (NEXO4071) |

### Input

`src/Framework/Forma.Framework.Input.Interop/wwwroot/scripts/`. The input router is the page's single authority over who owns an input, as the [input router design](input-router-design.md) states. It stays its own algorithm, usable alone, and `InputRouterDomqlModule` exposes it as a separate JavaScript module, so using the router alone loads no DOMQL.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `InputRouter.js` | 1,205 | 18 components, through `InputRegistration` or an `InputScope`; ten scripts, through its own `register` | **Forma module's implementation** behind `FormaInput`, below; its plain API stays for its JavaScript callers, whose `when` and `handle` are functions | Occurrences ([D49]) handled inside the dispatch ([D51]), in order across owners ([D69]); the owner ([D47]); null options ([D59]); state per document ([D57]); `inputState` reading the state a registration offers ([D52]); routes and overrides bound with declared types ([D82]) | 6; its first consumer at 5 |
| `InputInspector.js` | 95 | `FormaInputInspector`, on the Showcase's inspector page | **Subscriptions** to the router module's decisions, below: one to keys and presses, `types = ["keydown", "pointerdown"]`, unthrottled, and one to wheels, `types = ["wheel"]`, throttled to the latest each 100 ms, as today's script throttles wheels alone. A single throttled subscription would drop keys and presses within a wheel's interval | `localName` and `classes` ([D16]); a throttle ([D73]) | 6 |

The router module's surface:

| Request | Kind | Query |
| --- | --- | --- |
| A registration | Continuous execution | `scope:inputRoutes({ routes = routes, modal = modal, escape = escape, closeRequest = closeRequest, pressAway = pressAway, boundary = boundary, parent = parent, within = within, active = active })`, its options bound, so `update` carries a change of `active`. `within` is declared `List<Element?>`, since a list holding a null binds as one, which `List<Element>` refuses (NEXO3042), and today's router tolerates a null among them |
| The host's policy | Once execution | `Domql.document:configureInput({ escape = "dismiss", closeRequest = null, pressAway = "dismiss", bindings = bindings })` |
| The host's routes, today's `registerHost` | Continuous execution | `Domql.document:hostInputRoutes({ routes = routes, preemptiveRoutes = preemptiveRoutes })`, its actions heard through `Domql.document:inputActions() { action = it.action, input = it.input { family = it.family, type = it.type, key = it.key, code = it.code, … } }`. An `Action` holds the input's target element, so a subscription projects it (NEXO3043). The module registers them on the root element, as `registerHost` does, so the structural walk reaches them last, and its sources stay on the document, where the subscriptions are made ([D49]) |
| Matched actions | Event source | `scope:inputActions() { action = it.action, input = it.input { family = it.family, type = it.type, key = it.key, code = it.code, button = it.button, … } }`, carrying every field C#'s `InputInfo` reads, `family` first |
| Dismissals | Event source | `scope:inputDismissals() { action = it.action, reason = it.reason }` |
| Decisions, for the inspector | Event source | `Domql.document:inputDecisions({ types = types }) { input = it.input { family = it.family, type = it.type, key = it.key, … }, action = it.action, precedence = it.precedence, source = it.source, tie = it.tie, reason = it.reason, preventDefault = it.preventDefault, propagation = it.propagation, owner = it.owner { rid = it["data-rid"], tag = it.localName, id = it["id"], classes = it.classes }, stack = it.stack { element = it.element { … }, layer = it.layer, modal = it.modal, parent = it.parent { … }, activation = it.activation, reason = it.reason } }`. C#'s `InputDecision.Owner` and `InputLayer.Element` become structured descriptions where they are selector strings today |
| A scope's state | Member | `scope:inputState { active = it.active, foremost = it.foremost, layer = it.layer }` |
| The active stack | Member of its extension of `Document` | `Domql.document:inputStack { layer = it.layer, modal = it.modal }` |

- **Data, not functions.**
  - **Declared types.** The host layer makes each query with its bindings' types, `{ types: { routes: 'List<FormaInput.Route>' } }` and `{ types: { bindings: 'Dictionary<String, FormaInput.BindingOverride>?' } }` ([D82]), and every update keeps them. Data typed from itself is of no module's type, so it reaches no such parameter (NEXO3042).
  - **The routes.** A route's binding is one structured type holding every family's fields, each nullable, with a `family` field. The declared type captures the shapes C# serializes for each family: a field its family lacks is null, and one the type lacks is dropped. Typed from itself, a list mixing those shapes doesn't bind at all (NEXO2021).
  - **The overrides.** The host binds them as a `Map` keyed by action, which binds as a dictionary. A plain object is no dictionary (NEXO2045), and written in place, a dictionary's values construct no structure (NEXO3042).
  - **No functions.** A route has no `when`. A host call could keep a function now ([D61]), but the routes C# gives are data, and the JavaScript callers keep the router's own API.
- **The module's own types.** An occurrence is the module's `Action { action, input }`, its `Input` captured inside the router's listener, so `input.target` lets a query say which item was pressed. `Input` is one structured type holding every family's fields, null where its family has none, since Nexo has no union types.
- **Plain DOMQL** is every lifecycle, projection and delivery: `run`, `update`, `dispose`, `executeAsync`, `subscribe`, `watch`.
- **The router's algorithm** is every decision: family matching, specificity and ties, the native-control claims, the structural walk over `composedPath()`, ambient precedence, press-away and its pass-through, preemptive routes, click claiming, close requests, the host's binding overrides, validation, its document listeners, `data-forma-layer` stamping, and committing a focused field's pending edit before it routes an action.
- **What the router needs of Forma** to serve the module: an action callback beside its dismissal callback, a hook when the order of its scopes changes, a description of its bindings without their `when` functions, and a description of an input that carries its target, which today's `describeInput` leaves out.

### The composer

`src/Blazor/Forma.Blazor.Composer/wwwroot/scripts/`, product scripts. The rich-text editor's algorithms stay the composer's, contributed as `FormaRichText`. Each read of the selection feeds a change to a `Range` within the same command, and `SelectionMemory` keeps live ranges. So the module keeps its native selection, ranges and listeners, though DOMQL now serves the document selection and its `Range` ([D20]), editing events ([D31]) and selection placement ([D43]).

What the composer needs of DOMQL is the behavior contract:

- occurrences delivered inside the dispatch ([D49], [D51]);
- commands ([D48]) and the owner ([D47]);
- the state a behavior offers ([D52]);
- a null option passed as it stands ([D59]);
- DOMQL's observations and frames for module code ([D53]);
- reactions, which run its editor commands inside the key's dispatch, removing their second crossing, for the owner its `edit` runs for ([D71], [D48]);
- queries from module code, for the clipboard and the owned writes of the gutter and the bar ([D54]), and owned writes beside Blazor's render ([D70]).

DOMQL's [behaviors in a browser](https://github.com/FormaUI/DOMQL/blob/main/tests/dom/Behaviors.browser.js) run an editor built this way in Chromium, a module keeping a contenteditable's caret, with a command, a shortcut and a flush on blur:

- **A command's selection.** A command that changes what the editor offers is reported once by an `immediate` watch, before the command's answer reaches its caller. The `selectionchange` Chromium queues after the command reports nothing more, and a `notify()` that changes nothing reports nothing. A reaction's handler hears the action, with the reaction's answer, before the watch reports.
- **The blur before the press.** The editor's blur delivers its occurrence to the handler inside the press that moves focus to a button, before `pointerup`, `mouseup` and `click`, and inside the Tab that moves it, before the Enter that presses the button. Sent at once, it reaches .NET before the button's `@onclick`, Server and WebAssembly; what it holds is applied before the action reads it only through the edit boundary, [above](#the-browser-transfer-and-the-edit-boundary).
- **An input method's composition.** It completes in the editing host as Chromium composes it, beside a watch of the editor's state, subscriptions to its events and a class an owned write keeps, written again in the middle of the composition. Its `keydown` reads `key` `Process` and `isComposing` true, and each `input` reads `insertCompositionText` and `isComposing` true, so the router's test of a composing key holds.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `RichTextEditor.js` | 555 | `FormaRichTextEditor`, with its presets and the composer's plugins and dialogs | **Continuous execution** `canvas:richText.edit({ debounceDelay = 400 })`. Commands reaching it: `canvas:richText.execute({ command = "bold", value = null }) { selection = it.selection }`, answering the selection the command leaves, never the document, which reaches C# as an edit ahead of the answer; `canvas:richText.commit()`, its pause cut short; and `canvas:richText.beginLinkDialog({ anchor = false })`, answering `{ text, href, name, opensInNewTab, count }` through the transfer, since a selection can cover the whole document. C#'s own document reaches the canvas through the edits' `write`, the canvas being the source the editor provides for them. Each rejects with `DOMQL5016` once the editor has ended, where `exec` answers null today. A canvas the host may bind as null is written `canvas?:richText`, since `canvas:richText` of a null binding is refused at resolution (NEXO3063). Sources `canvas:richText.changes() { revision = it.revision, start = it.start, removed = it.removed, inserted = it.inserted }`, the edits of its serialized HTML at its own pace, as the [browser transfer](browser-transfer-design.md) has an editor send them, and `canvas:richText.activations() { kind = it.kind, … }`, an activation delivered inside the double-click's dispatch once the module has canceled it, each reaching C# through the transfer. Its `commit()` is the flush the edit boundary runs, and its blur delivers the edit at once, as today's blur commits. A watch of `canvas:richText.selection`, a member reading the state `edit` offers, of a nullable type, since it reads null where no `edit` runs for its owner: the instance calls `notify()` only when the block, the styles or caret-or-range change, so the watch reports as today's key does. `execute` and `setHtml` call `notify()` as well, since DOMQL's [state](https://github.com/FormaUI/DOMQL/blob/main/spec/behaviors.md#behaviors-state) asks an instance to say whenever what it offers may have changed, and a watch reports only a value that changed. `setHtml` also forgets the key, so the next snapshot is reported where it differs from the last, which is what today's reset of the key forces. C# takes the selection from the watch alone. `execute`'s answer still carries `selection`, which C# reads only to announce the formatting the command turned on or off, against the styles it held as it ran the command, or as a reaction's action reached it. The answer and the watch's report arrive in either order: a command C# runs is reported before its answer returns, and a reaction's action reaches C# before the report. Applying an answer's selection could land after a later report, so C# applies none. It is watched under the `immediate` schedule, as today's listener reports at once, where a `frame` watch reports a frame later and not while the document is hidden. Its keyboard commands are reactions to its scope's actions. A route's action is a composer command's name, as `format.bold`, `align.left` or `paragraph.h1`, which C# maps to an editor command and its value, as `bold`, `justifyLeft` or `formatBlock` with `"h1"`. So the reaction looks the action up in a dictionary of the editor's commands, bound as a `Map` or written as a dictionary literal, since a plain object binds as an object, which has no indexer (NEXO3012): `let target = commands[event.action]`, then `canvas:richText.execute({ command = target?.command, value = target?.value })`. An action the dictionary lacks, as the composer's undo and redo, the link dialog's Ctrl+K, its shortcuts dialog, or a host plugin's own command, has a null `command`, which skips the reaction. C# then runs no editor command for an action the reaction carried out, and applies its answer as it applies `exec`'s. A paste is an `execute` whose answer settles once the clipboard is read, natively or through `Domql.window.readFromClipboard({ types = ["text/html", "text/plain"] })`, and its content inserted; a paste from a menu is a reaction to its press, heard through a source on the toolbar's paste item, and the item's C# click runs no paste of its own | Commands ([D48]), occurrences ([D49]), [D51], state ([D52]), the owner ([D47]), a null `value` ([D59]); reactions ([D71]); the clipboard through `env.domql` ([D54]) | Composer |
| `RichTextEditor/Canvas.mjs` | 297 | The editor | **Forma module's implementation**: the editable region, its ranges and blocks. `closestBlock` and `selectedBlocks` write, wrapping loose content, so no member reads through them | — | Composer |
| `RichTextEditor/SelectionMemory.mjs` | 67 | The editor, `ContentWriter` | **Forma module's implementation**: a live `Range` kept across focus changes, the running instance's state | The editor's commands reach it as their `instance` ([D48]) | Composer |
| `RichTextEditor/SelectionInspector.mjs` | 146 | The editor | **Forma module's implementation** of the state the `edit` behavior offers, which the member `canvas:richText.selection` reads and `execute` answers. The instance takes a snapshot in its own `click`, `keyup` and `selectionchange` listeners, as today, and calls `notify()` only when its key changes, so a watch reports the offsets of the change that raised it. A member reading the selection itself would report every caret move, and read a computed alignment no observation follows | State a behavior offers ([D52]); no reporting on parts, since the instance decides when its state changes | Composer |
| `RichTextEditor/InlineFormatter.mjs` | 359 | The editor | **Forma module's implementation** | — | Composer |
| `RichTextEditor/BlockFormatter.mjs` | 146 | The editor | **Forma module's implementation**; its reveal scrolls and places the caret in one command | — | Composer |
| `RichTextEditor/ListFormatter.mjs` | 279 | The editor | **Forma module's implementation** | — | Composer |
| `RichTextEditor/ContentWriter.mjs` | 128 | The editor | **Forma module's implementation**, inserting at the remembered range | — | Composer |
| `RichTextEditor/Links.mjs` | 89 | The editor | **Forma module's implementation**, reading the frozen copy of the selection a dialog keeps | — | Composer |
| `RichTextEditor/TableEditor.mjs` | 168 | The editor | **Forma module's implementation** | — | Composer |
| `RichTextEditor/MediaMarkup.mjs` | 111 | The editor | **Forma module's implementation**; reading an activated element back could become the activation's projection | —; as a projection, `text` ([D15]), the attribute indexer and delivery inside the double-click's dispatch ([D51]) | Composer |
| `RichTextEditor/Markup.mjs` | 35 | The editor's modules | **Forma module's implementation**: pure helpers | — | Composer |
| `RichTextEditor/Fragments.mjs` | 10 | `InlineFormatter` | **Forma module's implementation**: a pure helper | — | Composer |
| `BlockAlignment.js` | 83 | `FormaRichTextGutter` | **Continuous execution** `gutter:blockAlignment({ source = canvas, rowSelector = ".forma-composer-rich-text-gutter-row", blockAttribute = "data-block" })`, gaining an `update`. Its implementation can hold the source's `resize`, `mutation` and `scroll` `event` observations and the gutter's `mutation` through `env.observe`, and lay the rows out in `env.frame`, in place of its own observers and `FrameScheduler`. Each row's `hidden`, which `FormaRichTextGutter` renders, is held through `ownAttribute({ name = "hidden", value, keep = true })` run with `env.domql`, or no longer rendered | `env.observe` and `env.frame`, if it chooses ([D53]); owned writes ([D70]) through `env.domql` ([D54]). As a watch, it would follow what its members miss through a local ([D19]), its selectors observed for what they read ([D26]), and lay each row out on the element `watch.sourceOf` answers ([D72]) | Composer |
| `SelectionFollower.js` | 272 | `FormaRichTextSelectionBar` | **Continuous execution** `bar:followSelection({ source = canvas, placement = "below", handle = …, activeClass = … })` of `FormaPlacement`, composing the gesture, `TabOrder`, `FocusSurface` and a router registration, run after `configureInput`, as the bar configures the router before attaching today. Its active class, on the `class` the bar renders, is held through `ownClass({ name, present, keep = true })` run with `env.domql`. Its option is named `source`, as `blockAlignment`'s is, since DOMQL's owner is the operation's. A null `source` hides the bar, as an owner that resolves to nothing does today; following any selection, with no source given, has no caller | A null `source` passed as it stands ([D59]); `env.observe` and `env.frame` if it chooses ([D53]); owned writes ([D70]) through `env.domql` ([D54]) | Composer |

### The data view, the explorer and the Showcase

Product scripts of `Forma.Blazor.DataView`, `Forma.Blazor.Explorer` and `Forma.Blazor.Showcase`.

| Script | Lines | Used by | Becomes | Rests on | Step |
| --- | --- | --- | --- | --- | --- |
| `ColumnFit.js` | 80 | `FormaDataGrid` | **Watch** `table { availableWidth = (try it.parent as Element)?.clientSize?.width, columns = it:dataGrid.wantedWidths({ attribute = "data-column", measuringClass = "forma-data-grid-measuring" }) { key = it.key, width = it.width } }`, accepting partial observation. `wantedWidths` is a maintained member answering `List<ColumnWidth>?`, nullable as DOMQL `1.0.0-beta.2` requires of a member reading maintained values (NEXO4071), whose observation type, the data view module's, observes the boxes and the content through `env.observe`. It measures with `env.measureUnder(table, { class: measuringClass }, read)` as it starts and as it samples, which DOMQL's own observers never hear. The host layer passes on no snapshot whose `availableWidth` or `columns` is null, as today's script reports nothing for a table out of the document; C#'s `ColumnFitReport` holds neither as null. Its fallback is a continuous execution whose state a watch reads | A transient measurement ([D55]); `env.observe` ([D53]); its fallback, state a behavior offers ([D52]) | Data view |
| `GridColumns.js` | 60 | `FormaDataCards` | **Watch** `ruler { columns = it.grid?.columns.count, cardSize = cards.style["--forma-data-cards-size"] }`, `cards` bound to `FormaDataCards`' root, whose rendered `style` carries `--forma-data-cards-size`, accepting partial observation; reading the card size follows a change the script misses. C# clamps the count to 1, since `grid` answers no track for a template of `none`. The host layer passes on no null count, which `grid` answers while the ruler has no layout box, as today's script reports nothing for a ruler out of the document | — | Data view |
| `HeightReservation.js` | 60 | `FormaDataView` | **Watch** of the table's, its header's and its tallest item row's heights, accepting partial observation. C# turns them into the region's `min-block-size` by today's rule: a full page's height held outright, a short page's projected only while nothing is held. Below | Selectors observed for what they read ([D26]), so the selection's `tabindex` writes evaluate nothing | Data view |
| `SelectionInteraction.js` | 645 | `FormaDataView`, over every view | **Continuous execution** `region:dataView.selection({ itemSelector = "[data-item]", itemKeyAttribute = "data-item", controlSelector = "[data-item-control]", selects = selects, moves = moves, positionAttribute = "data-item-position", seeks = seeks })`, its modes bound, so `update({ region, selects, moves, seeks })` replaces today's detach and attach on a mode change: an update replaces the bindings, never the literals a text writes. It offers one source, `region:dataView.selections() { kind = it.kind, token = it.token, tokens = it.tokens, from = it.from, to = it.to, additive = it.additive, request = it.request, last = it.last, mode = it.mode }`, a seek carrying its anchor's position in `from`, null unless it extends, as `HandleSeekAsync` takes it, keeping its five reports in order, delivered inside the press's or the key's dispatch: a select, a range by tokens, a range by positions, a seek and an activation. A range by tokens carries every token it spans, so it reaches C# through the [browser transfer](browser-transfer-design.md). The commands `region:dataView.focusItem({ request = request, position = position })` and `region:dataView.cancelSeek()` reach it, and reject with `DOMQL5016` once it has ended, where today's call answers `false` | Occurrences ([D49]) inside the dispatch ([D51]), commands ([D48]), the owner ([D47]); `env.observe` and `env.frame` if it chooses ([D53]) | Data view |
| `Breakpoint.js` | 57 | `FormaExplorerCommandArea` | **Watch** `rows.style["--forma-explorer-header-layout"]`, accepting partial observation. `style` answers null for an empty value, which C# reads as the wide tier, as it reads today's empty string | — | Explorer |
| `ReadingPlace.js` | 188 | The Showcase's sidebar | **Continuous execution** of the Showcase's own module, `pages:readingPlace({ links = ".culture-switcher a", sections = ".demo-section", current = ":scope > a.active" })`, with `pages:reveal()` a command. Its note is written inside the click before a full navigation unloads the page, and its hold reacts to every change for two seconds, so both stay inside, with their session storage. The sidebar runs it today through a `JSEnvironment` of its own, with no Forma context, which it needs for a session and an owner the host layer can run it for ([findings](#the-explorer-and-the-showcase)) | Commands ([D48]), the owner ([D47]) | Showcase |

The data view's height reservation, accepting partial observation. `size` measures each border box, fractional as `rect` is, without evaluating again as the page scrolls:

```domql
region {
    let items = it.all("table > tbody > tr.forma-data-grid-row"),
    table = it.first("table")?.size?.height,
    header = it.first("table > thead")?.size?.height,
    count = items.count,
    tallest = items.max(it.size?.height)
}
```

Selecting the item rows by their class leaves out the loading, empty and group-heading rows, which today's text filter counts.

## The order of the work

DOMQL's own [order of the work](https://github.com/FormaUI/DOMQL/blob/main/docs/features-status.md#planned-capabilities) holds one item, the C# half. Nothing in DOMQL blocks a step of Forma's, so what remains is Forma's work, step by step:

| Forma's step | Forma's work | Rests on |
| --- | --- | --- |
| 5, the request model | The host layer over DOMQL: each module's loader, and every watch, execution and subscription carried to C#, one delivery in flight for each watch and a bounded queue of occurrences. Then the freeze pane, the sentinel, focus, the text count and the file viewer's centering on it | [D46], [D47], [D51], [D59], [D60], [D66], [D68], [D69], [D74]; the freeze pane's [D9], [D11], [D13], [D26], [D82]; the text count's [D70] |
| 6, input routing | `FormaInput` over the input router, its routes and overrides bound with declared types; the wheel's turns; the inspector's two subscriptions, its wheels throttled | [D49], [D51], [D52], [D57], [D59], [D69], [D73], [D82] |
| 7, anchored presentation | `FormaPlacement`'s anchored popover: its commands, and its `left` and `top` kept beside `FormaPopover`'s render, with the cost of updating them each frame measured against today's direct writes, the Android web view included | [D48], [D59], [D70] |
| 8, virtualization | `FormaVirtualizer`: its maintained member, `reveal` and `scrollTo` | [D53], [D67] |
| 9, gestures, files and reordering | `FormaGestures`, `FormaDragDrop` and `FormaFiles`, their classes kept beside the render; the splitter's drags | [D49], [D51], [D70] |
| 10, dialogs, frames and windows | The dialog's executions in one task, and its opt-in scrim close; `FormaPresentation`'s windows; the frame's geometry handed to the render of `FormaFrame` or its presenter through `commit`, naming its drag, from the render carrying the accepted placement | [D14], [D34], [D36], [D48], [D64], [D70] |
| 11, the remaining actions | The operations, built in or of `FormaAssets`, `FormaPrint` and `FormaEmbeds`; the copy and the share as reactions; the autofill mark | [D39], [D40], [D57], [D70], [D71] |
| The composer | `FormaRichText`, its editor commands as reactions, looked up by the route's action; the gutter's alignment; the selection bar's follower, their rendered targets held | [D48], [D49], [D52], [D53], [D54], [D70], [D71] |
| The data view | `FormaDataView`'s selection and wanted widths; the cards' columns and the height reservation as watches, with C# clamping the cards' count to 1 and rendering the region's `min-block-size`, which the script writes today | [D26], [D48], [D49], [D52], [D55] |
| The explorer | The header tier as a watch | — |
| Culture | None: the coordinator keeps its own bridge, outside DOMQL, as [culture](#culture) sets out | — |
| The Showcase | The reading place's module, run through a Forma context the sidebar gains | [D48] |

Beside the steps:

- **The C# half,** A1, and A5's queue wait for their design ([D46], [D69]). Forma's host layer covers both from step 5. Once the C# half is designed, the host layer takes its typed queries and records and keeps its own transport (F1), as A1's [correction](#corrections-to-domqls-definitions) asks.
- **Forma's findings,** below, are Forma's to settle, each at the latest with the step that converts its script.

## Findings in Forma along the way

Reading the scripts turned these up. None was reproduced, and each is Forma's to confirm and settle.

### Settled since the earlier survey

- **`FormaSplitter` captured the pointer after a round trip.** `ebc4f662` moved the drag into `Splitter.js`, which captures in the gesture's press handler, and removed `Pointer.cs`.
- **A released scope kept a stale `data-forma-layer`.** `9a949f09` takes the layer from a scope that leaves the active ones.
- **`GetSelectionTextAsync` and `SelectionFollower.UpdateAsync` had no callers.** `bd7121c3` removed them, with `GetHtmlAsync`.
- **`CultureCookie.cs` documented a write the script couldn't observe.** The script and its project were retired in `1ce8cafe`.

### Swallowed rejections and dropped results

- **`.catch(() => {})` stands** in `Owner.js`'s `tell`, and so in the reports of its ten importers; in `InputInspector.js`; in `Playback.js`'s `play()`; and in the rich-text editor's `#tell`, `pasteText` and `pasteWord`.
- **`CultureCoordinator.js` swallows two rejections** with `.then(() => {}, () => {})`, at lines 347 and 391, and logs nothing.
- **`Asset.js`'s `attempt` drops a failure's cause,** so C# logs only that the asset didn't load.
- **A canceled share reads as success.** `Share.js` folds cancel, refusal and invalid data into `false`, and `Share.cs` calls `TryInvokeVoidAsync`, answering true whenever the call completed.
- **A failed copy shows nothing.** Both callers of `Clipboard.WriteAsync` drop its result.
- **Blocked autoplay is never retried.** `Playback.MuteAsync` answers true whenever the call ran, so `FormaVideoPlayer` marks the video muted and playing even when playback was blocked.
- **The rich-text editor's C# side** discards failures in fire-and-forget tasks, `_ = InvokeAsync(…)` and `_ = Interaction.ExecuteCommandAsync(…)`, and ignores `SetHtmlAsync`'s `false`.

### Input acted on outside the input router

- **Still so:** `DragGesture.js` without an `action`, as `ReorderInteraction.js`, `Frame.js` and `SelectionFollower.js` use it; `Frame.js`'s own `keydown`; `Dialog.js`'s scrim `click`.
- **New:** `DropZone.js` cancels `dragenter` in a raw listener; `SelectionFollower.js` nudges with the arrow keys in its own `keydown`; `RichTextEditor.js` cancels `dblclick` itself, though the router routes `dblclick`; `FormaBreadcrumb` cancels `dragover` on each crumb through Blazor's `@ondragover:preventDefault`, beside the explorer sidebar's routed drop zone, and its C# write of `DropEffect` never reaches the browser.

### The input router

- **`typing` bindings are missing from `BindingFields`,** so a typing binding has specificity 0 and counts as the same binding as a bare `keydown` one. No caller pairs them yet.
- **It reads the deprecated `navigator.platform`** for the primary modifier, where DOMQL's `Domql.window.shortcutModifier` reads `navigator.userAgentData` first ([D23]).
- **C#'s bindings never equal JavaScript's.** C# sends `primary: false` where a script's binding leaves it out, so a host override's `remove` never removes a module's default binding, and a replaced binding loses its default's `when`, so a remapped `TabOrder` route consumes Tab even where it has no destination.
- **Registering again resets the activation order.** `InputRegistration` registers again when `parent` or `within` changes, so an active scope jumps to the front.
- **Layers lag a moved element.** They depend on containment, which the router doesn't observe.
- **Every refresh rewrites every layer stamp,** unchanged ones included, which costs an evaluation of each DOMQL watch whose selectors test `data-forma-layer`, since a selector is observed for what it reads ([D26]).
- **The inspector** reports only the first resolution of a pass-through press, keeps crossing to C# while paused, and its `attach` ignores its target.
- **The focused field's commit,** added in `52c5047c`, reads `document.activeElement` and the `input` event's target at the document, so it misses a field inside a shadow root.

### Focus and scrolling

- **`RovingFocus` roves nothing inside a popover.** `#roved()` drops a control whose `closest('[popover]')` isn't null, which looks past the surface, so a `FormaWindow`'s title bar, a toolbar inside a manual popover, gets no roving.
- **`FocusStateObserver` stays within for good after the focused element is removed,** since only a press inside sets what a later leave needs, so `FormaDateTimePicker` never commits its half entry.
- **`RovingFocus` fixes its arrows' direction at construction,** so a later change of `<html dir>` leaves Left and Right reversed.
- **`DomObservation.RefreshAsync` discards `refresh`'s `false`,** so an observation the browser released when its element left stays active in C#.
- **`ScrollContainer`'s page step answers `true` when nothing moves,** against `StepAsync`'s documented meaning, and a test holds that in place.
- **`ScrollInteraction` treats page-mode wheel deltas as pixels.**
- **`TabOrder` consumes Tab when focus can't land,** as on a control a fieldset disables, and searches the page twice at a surface's ends.
- **`FocusInteraction` ignores `wrap` when `roving` is set,** though C# accepts both.
- **`FormaComposer` drops the focus interaction's handle,** so only its context releases it.
- **`DomScroll.StepAsync` and the engine's `run` path have no component caller.**
- **The freeze pane's corner shrinks under a block scroll.** `ScrollContainer` takes the corner as the smallest block end among the frozen parts, so the body's frozen cells sliding beneath the sticky header shorten it, 40 to 25 at a scroll of 45 pixels, and then make it negative, −330 at 400. The arrows sized by `--forma-freeze-pane-corner-extent` shrink and then lose their size, and C# is sent a new state at each step. The plan's query measures the first frozen part, the header's cell.

### Behaviors and components

- **`DropZone.js` shows its active class for any drag,** checking neither `accepts` nor its targets, so a file drop zone shows it for a text drag it won't take.
- **`ReorderInteraction.js`'s keyboard moves race the render.** Two moves before a render send positions read from the same stale order, so the second moves the wrong item.
- **`Frame.js`'s inline geometry competes with the render** of `FormaFrame` and `FormaWindow`, which the plan's own acceptance refuses; `Frame.FitAsync` has no caller.
- **Surfaces can sit under the scrollbar.** `AnchoredPopover.js`, `Frame.js`, `Virtualize.js` and `SelectionFollower.js` clamp to `innerWidth` and `innerHeight`, which include it; `Domql.window.size` leaves it out.
- **`AnchoredPopover.js` never observes its anchor's size,** so an anchor that moves without a scroll or a window resize leaves the popover behind.
- **`FormaSplitter` takes its direction from the culture,** not from the bar, so inside a `dir` island it drags and steps backwards.
- **`Window.js` keeps `topmost` module-wide, and raising a window that holds focus drops it.** Moving the focused element's ancestor with `appendChild`, `insertBefore` or `append` moves focus to the body, in Chromium firing `blur` and `focusout` within the call and in Firefox firing neither. `moveBefore`, which both have, keeps focus and fires nothing.
- **Latent:** `DragGesture.js` has no `lostpointercapture` listener; `Dialog.js` loses its scrim listener if opened twice; `FilePreview.js` keeps old URLs when its input is missing.

### The data view

- **The column fit never attaches again after a view switch.** The table is drawn anew, while the old instance keeps its detached table.
- **A key pressed in a field inside an item starts from no item,** so ArrowDown selects the page's first item; the test covering it doesn't focus the field.
- **A host's remap of `selection.select` or `selection.activate` throws,** since the remapped binding loses the `when` that kept the press on a marker.
- **The height reservation measures only the grid,** against the [data view design](data-view-design.md), and keeps the grid's height held after a switch to the list or the cards. Its held height can come from the loading row, and it has no script test.
- **A swallowed seek stays stuck,** and a restore scheduled for a frame can run after disposal.

### The explorer and the Showcase

- **A drag the explorer never sees end stays its drag.** `ExplorerItemManager` records the dragged item at Blazor's `@ondragstart` and forgets it only on a drop it hears. The explorer handles no `dragend`, and neither the sidebar's drop zone, whose `TreeDropZoneOptions` set no `Accepts`, nor the breadcrumb tests what is dragged. So after a canceled drag, a file or text later dropped on a folder row or a crumb moves the earlier item: a canceled drag fires only `dragend`, which nothing handles; `FormaBreadcrumb` cancels `dragover` for any drag while it has an `OnItemDrop`; and its drop, like the sidebar's `HandleDropAsync`, calls `DropOnItemAsync`, which moves the item `BeginDrag` recorded.
- **The Showcase's sidebar has no Forma context.** It isn't a Forma component, and runs `ReadingPlace` through a `JSEnvironment` of its own with a fresh `Guid` (`ShowcaseSidebar.razor`), so it has no session or runtime key through which the host layer would run the reading place and its `reveal` command for an owner.

### The composer

- **A native paste is unsanitized in the canvas until C# next writes a document of its own,** since C# sanitizes what it commits without writing it back; the same holds for every `exec` result.
- **A native undo from a menu isn't canceled,** and reaches C# as an ordinary change.
- **`exec` of a paste answers the content before the paste.**
- **Keyboard commands make two crossings** and race typing.
- **An inline format command can delete media paragraphs.** The sweep in `InlineFormatter.#reinsert` queries the whole canvas and removes every empty `p`, `div` or `span` holding no `br`, `img` or `hr`, which a `<p>` holding a `<video>` or an `<audio>` is.
- **A read-only canvas still routes its keys.** The router's native claims match only an editable canvas, and `FormaRichTextEditor` registers its scope whatever the state, so only C# refuses a command while the composer is read-only or disabled. A reaction run inside the key would format the canvas, which C# then won't commit, unless `edit` takes the state as an option updated with it, or `execute` checks `isContentEditable`.
- **The composer's toolbar commands have no element a reaction hears.** Paste, Paste from Word and the source view's Copy are toolbar commands whose clicks reach C# through the plugin command model, `ClipboardPlugin` and `RichTextSourceViewPlugin`. A reaction to their press needs a source on the item's element, and C# running no command of its own, or each runs twice.
- **`replaceContent` skips the resets a write from C# makes;** the paste callbacks don't check for disposal; `stripMarkers` removes markers the author typed; `Canvas.closestBlock()` and `selectedBlocks()` write though named as reads; `dispose` drops a debounced change still pending.
- **The selection bar follows a bar that's gone once its row empties and fills again** (latent). `FormaRichTextSelectionBar` attaches once, `_isAttached`, but draws its bar only while the selection row has items. A change of plugins that empties the row and fills it again draws a new bar, while the follower keeps the old one, as the column fit keeps its old table. No Showcase page changes its plugins.

### Culture, operations and helpers

- **A culture switch that doesn't land can block the coordinator's queue for good,** since each request waits for the one before it. A Blazor Web App of its own, `blazor.web.js` 10.0.14, showed when:
  - **A full-load switch** answers a promise that never settles, so a navigation that leaves the document in place blocks every later request. `location.assign` answered by a 204 fires `navigate` and `beforeunload` and nothing more. A declined `beforeunload` fires the Navigation API's `navigateerror`, which the coordinator doesn't hear.
  - **An in-place switch** settles where the pages are islands: every enhanced attempt ends in `enhancednavigationend`, landed or not, and no location-changing handler is consulted for `Blazor.navigateTo`. Under an interactive router no `enhanced*` event fires at all, so a switch a `NavigationLock` cancels never settles.
  - **A `navigateTo` answered by a 204** falls back to a full load that doesn't happen: `enhancednavigationend` cancels the switch, and the address gains a trailing `?`.
  - Settling a full-load switch on `navigateerror`, and an in-place one under an interactive router on its runtime's own navigation, would close each.
- **`Asset.js` resolves a module's URL against its own address,** while scripts and stylesheets resolve against the document's base.
- **`FormaCheckBox` remembers what it wrote,** not what the element holds, so a click that clears `indeterminate`, or a new element, leaves a `true` parameter unapplied.
- **`Controls.Interactive` misses `contenteditable=""`, `plaintext-only` and other casings,** the router has a second, different test for editable content, and `Controls.Focusable` misses a control a fieldset disables, and counts `tabindex` below -1 and a disabled control holding a `tabindex`.
- **`Print.js` leaves focus on `body`** once its frame is removed, doesn't sanitize, leaving the boundary to its caller, and writes `lang` and `dir` into the frame unescaped.
- **`Sanitizer.js` duplicates C#'s `RichContentMarkupPolicy`** by hand.
- **`Scrolling.nearestScroller` looks vertically alone,** stops at a shadow root and never answers the document's own scroller, so the drag gesture's edge scrolling can't scroll the page.
- **`CharacterCount.mjs` finds its output through the global `document`,** so an output inside a shadow root isn't found.
- **`GridColumns.js` misses a change of the cards' size,** which resizes nothing it observes.

### Forma's plan

- **Its capability mapping is behind the code.** It lists `Pointer.js` as a pointer capability, and lacks `Splitter.js`, `DocumentOrder.js`, `CharacterCount.mjs`, `CultureCoordinator.js` and `InputInspector.js`.
- **Its design's DOMQL examples** predate today's notation, as the [introduction](#replacing-formas-javascript-with-domql) says.

[D1]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d1
[D2]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d2
[D3]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d3
[D8]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d8
[D9]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d9
[D10]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d10
[D11]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d11
[D12]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d12
[D13]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d13
[D14]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d14
[D15]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d15
[D16]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d16
[D17]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d17
[D18]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d18
[D19]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d19
[D20]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d20
[D21]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d21
[D22]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d22
[D23]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d23
[D24]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d24
[D25]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d25
[D26]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d26
[D27]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d27
[D28]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d28
[D29]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d29
[D30]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d30
[D31]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d31
[D32]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d32
[D33]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d33
[D34]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d34
[D35]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d35
[D36]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d36
[D37]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d37
[D38]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d38
[D39]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d39
[D40]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d40
[D41]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d41
[D42]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d42
[D43]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d43
[D44]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d44
[D46]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d46
[D47]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d47
[D48]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d48
[D49]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d49
[D50]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d50
[D51]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d51
[D52]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d52
[D53]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d53
[D54]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d54
[D55]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d55
[D56]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d56
[D57]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d57
[D58]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d58
[D59]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d59
[D60]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d60
[D61]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d61
[D62]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d62
[D63]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d63
[D64]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d64
[D65]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d65
[D66]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d66
[D67]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d67
[D68]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d68
[D69]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d69
[D70]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d70
[D71]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d71
[D72]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d72
[D73]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d73
[D74]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d74
[D82]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d82
[D84]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d84
[D83]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d83
[D87]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d87
[D88]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d88
[D96]: https://github.com/FormaUI/DOMQL/blob/main/docs/decisions.md#d96
