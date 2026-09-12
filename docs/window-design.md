# Forma Window Design

## Scope

Coordinated window management for `FormaWindow`: identity,
origin, parent–child windows, activation, stacking, minimization, modality, bounds,
and a persistent handle for every window in `FormaRestArea`. Authoritative for
`FormaWindow`, `FormaRestArea`, the `Forma.Framework.Windows` model, and every
component's participation in a workspace.

## Purpose

A window on its own knows how to present itself. It does not know which window is the
current one, which windows another window opened, what closing it should take with it,
what a press elsewhere should do to it, or how a reader gets back to it once it is
minimized or sent behind something else. Those are relationships between windows, and
a relationship needs an owner that sees every window at once.

> Which window is current, which windows belong together, and how does a reader reach
> any window regardless of its state?

## Decision summary

- **Identity is the component's `RuntimeId`.** It is stable for the window's lifetime and
  is the only identity. Title and icon are presentation; windows with identical titles and
  icons coexist with separate identities and separate handles.
- **Origin is separate from identity.** `WindowOrigin` names the application or feature
  instance a window belongs to: a stable `OriginId`, a display `OriginName`, and a
  runtime `InstanceId` generated once per instance. Every managed window resolves an
  effective origin, explicitly or by inheritance.
- **Parentage is a direct reference.** `Parent` establishes the recursive lifecycle
  relationship; origin never does. Closing and minimizing cascade down the tree only.
- **One selector for every query and operation.** `WindowSelector` describes one or many
  windows by reference, origin, instance, title, or parent, and the same selector drives
  discovery, activation, stacking, minimization, restoration, and closing.
- **The model lives in `Forma.Framework.Windows`.** `IWindow`, `WindowOrigin`,
  `WindowSelector`, `WindowManager`, its results, the parent tree, the stacking order,
  and the activation history have no Blazor dependency. `FormaWindow` implements
  `IManagedWindow`.
- **The manager is a scoped service; a nested workspace cascades its own.** One manager
  per circuit is the default workspace. A host that needs a second workspace cascades
  another manager to the windows and rest area inside it.
- **Stacking is re-shown, not z-indexed.** A floating window is a manual popover, and the
  top layer stacks in show order; z-index has no effect there. The manager holds the
  order and re-asserts it in the browser by re-showing popovers bottom to top.
- **A modal blocks its parent's subtree; a modal root blocks the workspace.** Blocking is
  `inert` on the blocked windows, and a modal root stands on a scrim in the top layer: the
  translucent dark layer that dims what lies beneath, named as the theatre and Material
  Design name it, so that "backdrop" stays the word for the surface component.
- **Window-level selectors reach down only.** `window.CloseAsync(selector)` means "close
  these among mine": the selector is scoped to the window's own subtree. Siblings are
  the manager's business.
- **Minimizing hides; it never removes.** Component instances, entered values, selection,
  and scroll survive minimization and ancestor hiding. Hidden content is outside focus
  navigation and input routing until it is visible again.
- **Closing asks the whole subtree first.** `OnClosing` carries cancelable args and is
  checked across every affected window before any `OnClose` fires; one decline aborts
  the whole operation.
- **Results are records; bugs throw.** A `WindowOperationResult` reports applied, no
  match, ambiguous, ineligible, or declined, with the windows affected. An invalid selector or a
  window from another workspace throws.
- **A frame owns its box; a chrome dresses it; a window floats it.** `FormaFrame` is
  the raised backdrop with a box of its own, moved, resized and maximized, that knows no
  workspace; `FormaChrome` is a frame whose drag region is a title bar; `FormaWindow`
  is a chrome floated into the top layer and the only kind of workspace member, whose
  frame drives the window's box. A window is never embedded, so it has no mode; a frame
  is never closed or minimized, so it has no membership.
- **Names follow the component's own conventions.** `Modal`, `Disabled`, `Active` with
  `ActiveChanged`, `Minimized` with `MinimizedChanged`, alongside `Maximized` with
  `MaximizedChanged`. The area that holds every window's handle is
  `FormaRestArea`, a sibling of `FormaCommandArea` in intent and in name.

## The window's surface

`FormaWindow`'s public surface, and what the workspace does with it.

| Concern | Surface | In the workspace |
| --- | --- | --- |
| Presentation | `Title` (required), `Icon` | The handle in the rest area shows both and follows later changes. |
| Position and size | `Placement` / `PlacementChanged` | Movement, bounds, and restoration all flow through it. |
| Maximization | `Maximized` / `MaximizedChanged` | Preserved across minimization; restoring from one never clears the other. |
| Capabilities | `Options`, `FrameOptions` | The window's own from its title bar; the frame's for the box. |
| Host commands | `Commands` / `OnCommand` | Beside lock, minimize, maximize, and close. |
| Closing | `OnClose` | Raised once, only when the window actually closes. |
| Content | `Header`, `Body`, `Footer`, attributes | The body is never removed to hide it. |
| Browser | `Window.js` for the top layer, `Frame.js` for the box | The window's script stacks and remembers focus; the frame's holds the box within its bounds. |
| Input | A scope, modal while `Modal` is set | Modality and press-away reach it through the router. |

The window is named by `aria-label` from its title; the chrome's private `TitleId`,
which identifies the title element for a region's `aria-labelledby`, is not the window's
identity.

## The model

### Identity and origin

```csharp
window.RuntimeId           // This window's Guid, the component's RuntimeId, read-only.
window.Parent              // Its parent window, or null for a root.
window.Origin.OriginId     // Stable identity of the application or feature.
window.Origin.OriginName   // Display name, such as "Explorer".
window.Origin.InstanceId   // One running instance's Guid.
window.Title               // Presentation.
window.Icon                // Presentation.
```

```csharp
public sealed record WindowOrigin(string OriginId, string OriginName, Guid InstanceId);
```

| Field | Meaning | Matching |
| --- | --- | --- |
| `OriginId` | Which application or feature. | Selects every instance. |
| `OriginName` | How it is shown. | Never a criterion; changing it changes nothing else. |
| `InstanceId` | Which running instance. | Selects that instance's windows. |

`InstanceId` is generated once per application instance, not per window or render. Two
Explorers share `OriginId` and `OriginName` with distinct `InstanceId` values, and each
of their windows has its own `RuntimeId`.

| Window | `OriginId` | `OriginName` | `InstanceId` | `RuntimeId` |
| --- | --- | --- | --- | --- |
| First Explorer, main | `explorer` | Explorer | A | W1 |
| First Explorer, child | `explorer` | Explorer | A | W2 |
| Second Explorer, main | `explorer` | Explorer | B | W3 |

An origin is resolved in this order: given on the window; inherited from the parent
window; taken from a cascaded application-instance origin; otherwise the workspace's
own origin, which the manager supplies. A window always has one.

### Parentage

`Parent` is a direct reference the window declares and the manager validates, at
registration and again whenever the window refreshes: no self-parenting, no cycles, no
window from another workspace. The manager keeps the tree it validated, so a declaration
it rejected never takes effect. Roots have no parent. Origin never implies parentage: two windows sharing an instance are not
parent and child, closing one root leaves the other open, and a descendant follows its
parent's lifecycle whatever its origin.

### The selector

```csharp
public interface IWindowSelector { }

public sealed record WindowSelector : IWindowSelector
{
    public IWindow? Window { get; init; }

    public string? OriginId { get; init; }
    public Guid? InstanceId { get; init; }
    public string? Title { get; init; }

    public IWindow? Parent { get; init; }
    public bool Recursive { get; init; }

    public bool HasCriteria { get; }

    public static IWindowSelector All { get; }
    public static IWindowSelector Current { get; }
    public static IWindowSelector Frontmost { get; }
    public static IWindowSelector Backmost { get; }
    public static IWindowSelector Previous { get; }
}
```

The five statics are markers with their own implementations inside the model. `All` is
explicit so it cannot be mistaken for a selector nobody filled in.

Matching rules:

- Every supplied criterion must match; a null property imposes nothing.
- `Window` selects that window. With `Title` as well, that window's title must match;
  it does not rename anything.
- `OriginId` selects every instance; `InstanceId` selects one. `OriginName` is never a
  criterion.
- `Parent` limits selection to direct children; with `Recursive`, to all descendants.
  The parent itself is excluded. `Recursive` is not a criterion on its own: without
  `Parent` it neither narrows a search nor counts toward an operation's required
  criterion.
- Without `Parent`, selection covers every registered window in the workspace.
- Title equality is exact and ordinal.
- Searches include inactive, minimized, disabled, and ancestor-hidden windows.
  Selection never overrides eligibility, modality, or lifecycle rules.
- A search with an empty selector matches everything. An operation requires at least
  one criterion or `WindowSelector.All`; an empty operation selector is rejected and
  changes nothing.
- `All` stands alone; combining its marker with criteria is invalid.

```csharp
new WindowSelector { Window = window }
new WindowSelector { Window = window, Title = "Properties" }
new WindowSelector { OriginId = "explorer" }
new WindowSelector { InstanceId = instanceId }
new WindowSelector { OriginId = "explorer", InstanceId = instanceId, Title = "Properties" }
new WindowSelector { Parent = window }
new WindowSelector { Parent = window, Recursive = true }
new WindowSelector { Parent = window, Recursive = true, Title = "Properties" }
WindowSelector.All
```

### Results

```csharp
public enum WindowOperationOutcome { Applied, NoMatch, Ambiguous, Ineligible, Declined }

public sealed record WindowOperationResult(WindowOperationOutcome Outcome, IReadOnlyList<IWindow> Windows);
```

No match, ambiguity, a match with no eligible window, and a declined close are ordinary
answers a caller branches on. An invalid selector, a foreign window reference, or an
unregistered window is a bug at the call site and throws. `Windows` holds what the
operation changed when it applied, and who declined when it was declined.

### The managed window

`IWindow` is what the workspace and a handle read. `IManagedWindow` adds what the
manager drives: it tells the window when it becomes active, minimized, hidden by an
ancestor, blocked by a modal, or raised to the top of the floating windows, asks whether
it may close, and tells it to close once every affected window has accepted. `FormaWindow` implements
both; a test drives the manager with a fake.

## The manager

### Workspace

Each `WindowManager` owns one workspace: its registry, parent tree, stacking order, and
activation history. Queries and operations reach only that workspace, and parents and
descendants live in the same one. Origin and instance group windows within a workspace;
neither defines one. Keyboard focus is coordinated across workspaces: independent
activation histories do not mean two windows hold focus at once.

The manager is registered scoped, so a circuit has one workspace with nothing to write.
A host that wants a nested workspace cascades another manager, and every window and
rest area inside that cascade belongs to it.

### API

```csharp
manager.RegisterAsync(window)      // for the component's lifetime
manager.RefreshAsync(window)       // after its title, icon, parent, or state changed
manager.UnregisterAsync(window)    // when the host disposes it

manager.Origin                     // the origin a window without one resolves to
manager.Current                    // the active window, or null
manager.Changed                    // raised after anything a handle or a query reads changed

manager.Windows                    // in the order they registered
manager.StackedWindows             // from the back of the stacking order to the front
manager.GetWindowByRuntimeId(runtimeId)
manager.TryGetWindowByRuntimeId(runtimeId, out window)
manager.TryFindWindows(selector, out windows)
manager.GetParent(window)
manager.GetChildren(window)        // direct children, back to front
manager.GetInstanceNumber(window)  // among its origin's instances, from 1, kept for life
manager.IsHidden(window)           // under a minimized ancestor
manager.IsBlocked(window)          // behind a visible modal
manager.IsEligible(window)         // enabled, visible, and not blocked
manager.Within(window, selector)   // the selector scoped to the window's own subtree

manager.ActivateAsync(selector)
manager.BringToFrontAsync(selector)
manager.SendToBackAsync(selector)
manager.MinimizeAsync(selector)
manager.RestoreAsync(selector)
manager.CloseAsync(selector)
```

A refresh re-validates the declared parent, brings the descendants' hiding into line,
moves a window that became modal in front of what it blocks, and lets a current window
that is no longer eligible give way. `GetWindowByRuntimeId` throws when absent;
`TryGetWindowByRuntimeId` reports false with null; `TryFindWindows` reports false with an empty
collection. Selectors replace separate
title, origin, instance, and descendant queries, and separate single and bulk
operation overloads.

A window offers the same operations. Without a selector, they act on the window itself;
with one, the selector is scoped to the window's own subtree through `Within`.

```csharp
window.ActivateAsync()
window.MinimizeAsync()
window.CloseAsync(new WindowSelector { Title = "Properties" })   // among this window's descendants
```

### Cardinality

Activation requires exactly one selected window: zero reports no match, more than one
reports ambiguity and activates nothing. With one, ancestor restoration and modal
redirection still decide which eligible window finally receives focus.

Every other operation applies to every selected window that is eligible for it. A
selection that overlaps a parent and its descendants processes each affected window
once. A close checks every affected window, descendants included, before closing any; one
decline aborts the whole operation.

## The window

### Surface

| Concern | Surface | Category |
| --- | --- | --- |
| Identity | `RuntimeId` | Read-only, generated. |
| Origin | `Origin` | Parameter; resolved when absent. |
| Parent | `Parent` | Parameter, validated by the manager. |
| Activation | `Active` / `ActiveChanged` | Bound state, coordinated by the manager. |
| Minimization | `Minimized` / `MinimizedChanged` | Bound state. |
| Lock | `Locked` / `LockedChanged`, `Options.Lockable` | Bound state; the option adds the command. Locked, a floating window is neither moved, resized nor maximized; everything else stays. |
| Modality | `Modal` | Parameter; a composing dialog derives it from itself. |
| Availability | `Disabled` | Parameter, as elsewhere in the library. |
| Closing | `OnClosing` with cancelable args, then `OnClose` | Callbacks. |
| Stacking | `BringToFrontAsync()`, `SendToBackAsync()` | Methods. |
| Lifecycle | `ActivateAsync()`, `MinimizeAsync()`, `RestoreAsync()`, `CloseAsync()` | Methods, each taking an optional subtree selector. |
| Press away | `Options.PressAway` | `Ignore` or `SendToBack`. |
| Bounds | `FrameOptions.Bounds` | The viewport, or a container element. |

`Options.PressAway` and `FrameOptions.Bounds` are settings a composing consumer forwards
unchanged, which is what puts them on the record. `Modal` is a parameter because a
dialog that composes a window decides it for itself.

### Activation and focus

Activation and stacking are separate. Bringing a window forward does not activate it or
move focus; `ActivateAsync` brings it forward and focuses it. The manager's activation
agrees with the router's scope activation and with browser focus at all times.

When the active window closes, minimizes, or is disabled, its eligible parent becomes
active; failing that, the most recently active eligible window in the workspace. Closed,
disabled, minimized, ancestor-hidden, and modally blocked windows are not eligible. With
no eligible window, nothing is active and focus returns to the workspace.

Each window remembers its last focused element. Activating it again restores that
element if it is still inside the window; otherwise the window's first control, else the
window itself. Modal redirection is applied before the final target is chosen.

### Stacking

Bring-to-front and send-to-back change order and nothing else. A group brought forward
keeps its internal order and lands ahead of the rest. A visible modal never goes behind
the context it blocks, and press-away cannot send it there. The manager owns the order;
the browser reflects it by re-showing the popovers from back to front.

### Minimization

Minimizing hides the window and keeps its handle. Its component instances and content
state survive: its popover is hidden and the element stays.

Minimizing a parent hides and deactivates every descendant, at every depth, as inherited
suppression: each descendant keeps its own `Minimized`, `Placement`, `Maximized`, and
`Disabled`. A descendant is never visible or active under a minimized
ancestor, and one registered under an already-minimized parent starts hidden. Hidden
windows release their input scopes and modal blocking until visible again.

Restoring a parent lifts its suppression only. Descendants minimized on their own stay
minimized; those under another minimized ancestor stay hidden. Restoration activates a
lone restored window that is eligible, not every descendant. Choosing the handle of a descendant hidden
by minimized ancestors restores the chain above it, then restores and activates it,
subject to modality.

Restoring from minimization returns the preceding `Placement` and `Maximized`. A
maximized window minimized and restored is still maximized. The placement is reused and
clamped to the current bounds. `RestoreAsync` restores from minimization; the
maximize toggle's "Restore" clears `Maximized`; neither clears the other.

### Closing

Every close, from chrome, from the router, or from the manager, takes one path: check,
then close descendants before parents, then raise each `OnClose` once and unregister.
`OnClosing` is checked across the whole subtree first; a window with unsaved work
declines by setting `Cancel`, and one decline leaves every window open. Overlapping
requests and hierarchy changes while checks are pending cannot let a newly related window
skip the check. `OnClose` alone is not proof of removal; the manager coordinates the host
callback, the closure, and disposal so a closed parent cannot leave a live descendant.

Sending back and minimizing never raise `OnClose`.

### Modality

A modal child blocks its parent's subtree; a modal root blocks the workspace. The blocked
windows are `inert`, and a scrim stands beneath a modal root in the top layer, which is
the native dialog's own model. Choosing a handle whose window has a visible modal
descendant brings that modal forward and activates it; the parent stays blocked until the
modal finishes. Nested modals resolve to the frontmost visible one, so a handle cannot
reach past a modal child. A modal hidden by ancestor minimization releases its blocking
until it can be shown again.

### Bounds

`FrameOptions.Bounds` is the viewport or a container element. The script measures the region
and clamps movement, resizing, and restoration to it, reapplying on viewport and container
resize. Maximized fills the selected region. Enough of the title bar and its commands
always stays reachable to move or restore the window; when the region is smaller than the
minimum size, the minimum wins and the chrome stays reachable. Every adjustment flows
through `Placement` / `PlacementChanged`, so host and browser agree.

## The rest area

`FormaRestArea` holds one persistent handle per registered window, keyed by `RuntimeId`, for as
long as the window exists: active or inactive, front or back, minimized, hidden under a
minimized ancestor, enabled or disabled. Identical titles and icons
produce separate handles; nothing is deduplicated. A handle shows the window's `Title` and
`Icon` and follows changes to them. The handle is the reader's way to a window; it is not
the resize handle the frame's `HandleClass` names.

Choosing a handle restores the window if minimized, brings it to the front, and activates
it. A window sent back comes forward and activates in place. Handles group by `OriginId`
or `InstanceId`; grouping is presentation and never changes parentage. A group is named
for its origin; the workspace's own origin, which windows without one resolve to, is
named by the library in the reader's language. Grouped by instance, the first instance of an origin keeps the plain name
and every later one carries the number the workspace gave it when it first registered
("Explorer", "Explorer 2", "Explorer 3"), so a screen reader tells them apart and a name,
once heard, never changes: closing an instance leaves the others' names as they were, and
a number is reissued only past the limit, and never to one an open instance holds.

The area stands alone or inside `FormaLaunchBar`, laid out as a row, a column, or a grid.
It follows the manager whose windows it presents.

## Frame, chrome and window

A window does not embed. What stands in a layout with a box of its own is a frame; what
carries a title bar is a chrome; what floats in the top layer as a member of the
workspace is a window. They are separate components, each composing the one beneath it.

| Component | Folder | Responsibility |
| --- | --- | --- |
| `FormaBackdrop` | `Layout/` | Surface appearance: one of four variants, flat, raised, inset or canvas. |
| `FormaFrame` | `Layout/` | Position, size, dragging, resizing, maximization and the resize handles. |
| `FormaChrome` | `Layout/` | The title bar, icon and commands, and the arrangement of header, body and footer. |
| `FormaWindow` | `Windows/` | Window identity, ownership, activation, stacking, modality and lifecycle. |

### `FormaFrame`

A frame is a raised backdrop with a box of its own. Without a placement it stands in its
layout like any backdrop and offers no interaction. Given a placement, or maximized, it
stands in the viewport within its bounds: the reader moves it by the drag region the
host designates, resizes it at eight handles over its edges and corners, and steps or
grows it with the arrow keys on the focused drag region, Shift resizing. Maximized, it
fills its bounds; locked, it stays where it is. It can be hidden, keeping what it holds,
and disabled, going `inert`. It has no title, no commands and no workspace: it is never
activated, minimized, closed, stacked or blocked, because those are what a window is.

```razor
<FormaFrame @bind-Placement="_placement">
    <DragRegion>…</DragRegion>
    <ChildContent>…</ChildContent>
</FormaFrame>
```

Parameters: `Label`, `DragRegion`, `ChildContent`, `Placement` with `PlacementChanged`,
`Maximized`, `Locked`, `Options` (`Movable`, `Resizable`, the resize limits, `Bounds`),
`Hidden`, `Disabled`, and captured attributes. `Frame.js` is its browser side:
the drag, the handles, the keys, and the fit within its bounds whenever the frame or its
bounds are laid out again, reporting where the box came to rest once.

The box the frame writes is its own element, or the parent that presents it. Which one is
the presenter's to say, not the frame's or a chrome's: a parent that presents a frame
cascades `FrameBox.Parent` to it, and the frame cascades `FrameBox.Self` on to what it
holds, so a frame nested inside stands on its own. Presented, the frame stays in the
parent's layout and fills it, renders its drag region and handles, and moves and resizes
the parent's element, reporting the parent's box through `PlacementChanged` without
keeping a placement of its own. The host stays the one authority over position, size and maximization; the frame
does the interacting and follows the host's geometry.

### `FormaChrome`

A chrome is a frame whose drag region is a title bar: the icon, the title and the host's
commands, then the optional header band, the body and the optional footer band inside
the frame. It is a `region` named by its title unless the host gives it another role, and
it forwards the frame's placement, maximization, lock and options, so a placed chrome is
a titled panel the reader drags by its title bar. A chrome knows no workspace
either.

### `FormaWindow`

A window composes a chrome and owns everything else: membership in the workspace with
the identity, origin and parent it declares, activation, minimization, hiding under a
minimized ancestor, blocking, closing with `OnClosing` asked first, press-away, modality
and the scrim, stacking in the top layer, `Window.js`, and the input scope. Its box is
the popover element it renders, and it cascades `FrameBox.Parent` so its chrome's frame
drives that box: the
title bar drags the window, the handles resize it, the frame's script holds it within
its bounds, and the placement comes back through `PlacementChanged` to the window, which
binds it for the host. `Maximized` and `Locked` go down the same path, so the window's
state is the only state. Its parameters are the chrome's, less `Hidden`, plus `Origin`,
`Parent`, `Placement` with `PlacementChanged`, `Maximized` with `MaximizedChanged`,
`Locked` with `LockedChanged`, `Minimized` with `MinimizedChanged`, `Active` with
`ActiveChanged`, `Modal`, `OnClosing`, `OnClose`, `Options`, a `WindowOptions`
(`Closable`, `Minimizable`, `Maximizable`, `Lockable`, `PressAway`), and `FrameOptions`.

The window hands its chrome one command list: the host's commands, then lock, minimize,
maximize and close. Commands the window recognizes it answers itself; the rest reach the
host's `OnCommand`. The window's root, not the frame, carries the dialog role and the
active and maximized states, and the window renders the scrim beside its root; the chrome
stays a `presentation` inside it.

### Names and classes

- `IWindow`, `IManagedWindow`, `WindowManager`, `WindowSelector` and `FormaRestArea`
  are named exactly: the workspace holds windows and nothing
  else. A window's parent is a window, and the rest area lists windows.
- The box's classes and handles are the frame's: `forma-frame`, `forma-frame-backdrop`,
  `forma-frame-content`, `forma-frame-drag-region` and `forma-frame-handle`. The
  chrome's classes and tokens are the chrome's: `forma-chrome`, `forma-chrome-titlebar`,
  `forma-chrome-title`, `forma-chrome-icon`, `forma-chrome-commands`,
  `forma-chrome-header`, `forma-chrome-body`, `forma-chrome-footer`, and
  `--chrome-titlebar-bg-color`, `--chrome-header-bg-color`, `--chrome-body-bg-color`,
  `--chrome-footer-bg-color`. The window keeps `forma-window`, its state classes and
  its scrim.

## Integration with the renderer

The manager is Framework code and awaits off the renderer, as every `Forma.Framework.*`
service does. `FormaWindow` therefore carries each request the manager makes onto its
dispatcher before touching state or raising a callback, and never refreshes the manager
from inside such a request: a change the manager made is one the manager already knows.

## Integration with input routing

A window's profile dismisses on Escape and platform close requests and ignores a press
away, and adds two things the router models: a `Modal` scope while
`Modal` is set and visible, and a pass-through press away, which the router reports as a
dismissal with the press-away reason and the window answers by sending itself back when
`Options.PressAway` says so, never by closing. Neither routes through `OnClose`. The scope
registers for the component's lifetime, activates with manager activation
rather than raw focus events alone, and is released while the window is hidden.

## Non-goals

- Application launching. `FormaLaunchBar` owns it and may host the rest area.
- Persisting windows across circuits. The host binds `Placement`, `Maximized`, and
  `Minimized` and decides what to keep.
- Moving `FormaDialog` out of `Feedback`. It stays classified by its purpose, prompting for
  a decision; composing a window does not change that.
- Moving content between a frame and a window. A host chooses a frame, a chrome or a
  window when it renders.
