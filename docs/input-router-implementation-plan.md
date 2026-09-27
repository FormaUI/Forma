# Forma Input Router Implementation Plan

The plan for building the router that [the input router design](input-router-design.md) states, and the record of what has landed.

## Status

Step 1 of the implementation plan is built: the router script, the records, the bridge and
its registration in `AddFormaUI`, host routes, and the script tests. `FormaInputScope`
declares a scope from markup and `FormaInputInspector` shows the router's decisions live;
the Showcase's `FormaInputInspector` page uses both, and its visible-browser walk covers the native
table, nesting, dismissal, the modal barrier, host routes, shadow roots, wheel, and pointer
input against real events. Resolution with a hundred registered scopes, ten of them active,
costs a few hundredths of a millisecond, so parent lookup scans the scopes as they are.
`FormaPopover` is the first library component on the router: it registers its root and
activates while open, with its trigger counted as inside it, so Escape and a press away
dismiss it through the router rather than through a document listener and a backdrop of
its own, and `FormaBreadcrumb`'s segment menus are `FormaDropDownMenu`s over it, so its own
key handler and backdrop are gone. `FormaDialog` is a modal scope while open, so Escape and a
platform close request go to whatever is foremost, and the dialog closes only when that is
itself. `FormaComposer`'s and `FormaToolbar`'s shortcuts are routes: the
composer registers its
catalog on its root and each editor registers it on its editable element, where the native
table would otherwise claim the keys, and `FormaToolbar` registers its actions' shortcuts on its
target. `FormaWindow` registers its root while attached, modal when the window is, and
active while it is the workspace's current window and focus is inside it, so Escape closes
the focused window alone and a press away sends it back when its options say so. The router
service and the scope handle, `InputRouter` and `InputScope`, are public rather than
internal, since the library's components register their roots through them from another
assembly. The router stamps each active element with its layer as `data-forma-layer`; no
stylesheet reads it. A profile carries no `focus` field, in the script or in `InputProfile`:
`Focus.js` declares its keys as routes on the surface it is attached to, `focus.next`,
`focus.previous`, `focus.first` and `focus.last`: Tab and Shift+Tab on a wrapping or arranged
surface, matching only where the surface has a control to send focus to, so a Tab past the end
of an arranged surface with nothing after it is left to the browser; the arrows along the axis,
Home and End on a roving one, matching only while one of its roved controls holds focus, with
the arrow along the reading direction resolved when it attaches. A key inside a nested surface
that routes it is that surface's and never reaches the one around it, which is what
`defaultPrevented` used to arrange; a Tab with a modifier other than Shift, or an arrow in a
text field, is left alone by the router's vocabulary and native table rather than by checks of
its own. `Wheel` declares each turn it answers as a route on the surface, under the action the
component names, `freeze-pane.scroll`, `pagination.page` and `file-viewer.zoom`, with the key it
is gated on as the binding's modifier; a turn over a popover opened from the surface stops at the
popover's active scope, and the native table gained any element that scrolls on its own, which
takes the wheel along the axis it scrolls, so a panel inside the surface keeps its own scrolling
and the behavior's checks for a popover, a fixed element or a scroller between the two are gone.
A track answers only a turn that is mostly vertical, so a trackpad's slight diagonal now moves
it where a strict zero sideways delta used to be required. `DragGesture` declares the press that
begins a gesture as a route on its container when the script composing it names an action, in
place of its own press listener, matching the primary button and the pointer type it answers
through the pointer vocabulary and only a press on something it can drag; `PanGesture` names
the container's action, `file-viewer.pan` for the viewer's image frame, so a press inside a
popover over the frame is the popover's. The reorder and the selection bar's handle still start
from the gesture's own listener. Active drags and the selection bar are not on the router:
`DragGesture` cancels a drag from its own Escape listener, and `SelectionFollower` returns focus
to the editor from its own.
Of the owners in step 3, the two shortcut sets, `FormaSelect` and `FormaDropDownMenu` have
moved: each routes its trigger's arrows on the trigger, and its list's arrows, Home and End
through `FormaPopover`, which carries its content's routes on the registration it already makes
while open, so the router prevents the page from scrolling under the list and `NavigationGuard`
is gone. `FormaSelect` still closes on Tab through the popover's key callback, since its popover
does not keep focus and so routes no Tab. `FormaTabs`, which had
no keys of its own before, routes the arrows, Home and End on its tab list alone, so an arrow
inside a panel stays the panel's; the arrow along the reading direction is next. Activation is
manual, since a panel can be as costly as an editor switch: the arrows move focus and the tab
order with it, and Enter or Space selects. `FormaEmojiPicker`'s category strip routes the same
keys on the strip alone, activating as it goes since a category is cheap to show; the strip is a
`FormaInputScope`, so its scope lives and dies with it as the options show and hide it. The two
strips share their key table and wrap arithmetic. `FormaFileViewer` routes the arrows on its
root as next and previous sibling, in reading order, and only while it has siblings to step
between, so a lone image keeps the arrows for scrolling its frame; the native table gained the
browser's media controls, so the arrows on a focused video seek rather than step.
`FormaSplitter` routes grow, shrink, Home and End on its bar, the arrows along the axis it moves
on, and swaps them for the orientation it is given after registering. `FormaSortableList`'s keys
are the first declared from script: `ReorderInteraction` registers its moves as routes on the
list beside the drag it keeps, matching only a key raised on an enabled handle with somewhere to
go, so a key elsewhere in an item, or at the edge, is left to whatever owns it; a modified arrow
no longer moves an item, since the router's keys take no modifier unless stated. The list has the
host's policy pushed before the script registers, and the browser interop assembly now builds on
the input one. The registration a component keeps on its own root is one thing, `InputRegistration`,
which `FormaInputScope`, the select, the menu, the tabs, the viewer and the splitter hold and sync
after each render in place of a copy each of the same reference, scope, in-flight and disposal plumbing; it owns the
reference the router calls back through, so the components' handlers are private.

## Input owners before the router

### Dismissal

Escape and press-away behavior are decided in several unrelated ways today:

- Native `<dialog>` cancellation.
- A document listener owned by `FormaPopover` (retired; the popover is a scope on the router).
- `FormaBreadcrumb`'s key handler and backdrop span (retired; its segment menus are `FormaDropDownMenu`s).
- `DragGesture` canceling an active drag.
- `FormaComposer`'s selection bar returning focus to the editor.

### Navigation keys

Arrow, Home, and End are handled independently by `FormaSelect`, `FormaDropDownMenu`,
`FormaEmojiPicker` tabs, `FormaFileViewer` sibling navigation, `FormaSplitter`,
`FormaDataGrid` through `SelectionInteraction`, `FormaSortableList` through
`ReorderInteraction`, and the selection bar's nudge handle; `NavigationGuard`
prevents page scrolling for two of them.

### Tab

`Focus.js` wraps or orders focus within a surface; native dialogs trap focus;
`FormaSelect` closes when Tab is pressed.

### Shortcuts and modifiers

`FormaComposer` registers its shortcut catalog as routes on its own scope and on each editor's
editable element, and `FormaToolbar` registers the shortcuts its actions carry as routes on its
`Target`; a command is its own action, so a host remaps either by the command's name.
`FormaDataGrid` handles Ctrl/Shift-click and Shift+Arrow selection;
`ImplicitSubmitGuard` controls implicit submission; `FormaSearchBox` and
`FormaPagination` assign behavior to Enter.

### Pointer gestures

`DragGesture` supports sortable reordering and the selection-bar handle; `PanGesture`
supports the command-bar track; `FormaSplitter` owns its pointer interaction; explorer
HTML5 drag and drop spans four views and the sidebar; `DropZone` supports file intake;
`SelectionInteraction` listens for capture-phase mousedown on `FormaDataGrid`.

### Wheel

`Wheel` is used by `FormaCommandBar`, `FormaToolbar`, and `FormaFileViewer`, optionally
requiring a modifier.

## Conflicts before the router

They share one shape: an outer component listens on its root or on `document` and acts on
input originating within an inner component — and a popup is inner even though the browser
draws it in the top layer, because it remains a DOM descendant of whatever opened it.

| Conflict | What happens today |
| --- | --- |
| Select inside a data-grid cell | `FormaDataGrid`'s capture-phase listener moves row selection before the select receives ArrowDown. Only text-entry targets are excluded. |
| Emoji picker over the composer | Resolved: `FormaComposer`'s shortcuts are routes, and a popup over it is an active scope that Ctrl+B pressed inside it stops at. |
| Popup opened from a toolbar | Wheel input bubbling from the popup scrolls the toolbar track behind it. |
| Drag started inside a popup | Escape reaches the popup's document listener before the drag's element listener. |
| Nested drop targets | A folder node inside the sidebar's drop area lets both targets receive the drop unless the inner one stops propagation. |
| Nested popups | Whichever popup registered its document listener first answers Escape. |
| Tab handling | `Focus.js` respects `defaultPrevented` from a nested surface — a local solution, not a cross-component model. |

## Migration

| Conflict | Migration |
| --- | --- |
| Select in a grid cell receives ArrowDown | Both register scopes; `FormaDataGrid`'s capture handler acts only as owner |
| Emoji-picker search receives Ctrl+B | Done: composer bindings are routes on `FormaComposer`'s and the editor's scopes and do not act inside a nested scope |
| Popup content receives wheel over a toolbar | The wheel acts only as owner |
| Drag inside a popup receives Escape | The drag activates above the popup |
| Folder node receives a drop inside the sidebar | The sidebar does not act on a drop owned by the nested scope |
| Inner popup receives Escape | Done: router ordering replaces per-popup listeners |
| Popup in a dialog receives Escape | Done: routed before native cancellation |
| Dialog opened over a popup receives Escape | Done: modal barrier |
| Host `<input>` in a grid receives arrows | Native table, built; `FormaDataGrid`'s own text-entry exclusion stands until `SelectionInteraction` is on the router |
| Tab list receives arrows | New routes; the missing interaction |

## Steps

One architectural change, so one ownership rule is introduced and verified across the
existing components, landing as several coherent commits.

1. **Foundation.** `InputRouter.js` with scope and route registration, the native table,
   structural resolution over composed paths, active ordering with the precedence rules,
   per-class listener strategies, `resolve` and its views. The .NET records and the scoped
   bridge. Vitest for nested resolution through a top-layer popover, the native table, the
   modal barrier, containment, activation order, top-only dismissal, and release.
2. **Dismissal.** `FormaPopover` (done), `FormaDialog` with both entry points (done), active
   drags and the selection bar while focused (open), `FormaBreadcrumb` rebuilt on `FormaPopover` (done,
   as a `FormaDropDownMenu` per segment), `FormaWindow` (done). The per-popup document listener, both backdrops,
   and the backdrop z-indexes go (done).
3. **Owners onto routes.** `SelectionInteraction`, both shortcut sets (done: `FormaComposer`'s
   and `FormaToolbar`'s shortcuts are routes, and `KeyBindings` is retired), the three
   wheel tracks of `Wheel` (done), `PanGesture` (done), `DropZone` and `FormaExplorer`'s drop targets,
   `Focus.js` (done), and the components that own navigation keys — `FormaSelect`,
   `FormaDropDownMenu`, `FormaTabs`, `FormaEmojiPicker`, `FormaFileViewer`, `FormaSplitter`
   and `FormaSortableList` (all done). `NavigationGuard` is retired in favour of the
   router's `preventDefault` (done); the per-component text-entry exclusions are retired in
   favour of the native table (open).
4. **Verification** across combinations, in a visible browser: select in a grid cell; popup
   on a toolbar; emoji picker over `FormaComposer`; drag started in a popup; nested popups;
   popup in a dialog and dialog over a popup; nested explorer drops; a host `<input>` in a
   grid; Tab through nested focus-managed surfaces; Escape through successive active
   scopes; RTL navigation unchanged.
