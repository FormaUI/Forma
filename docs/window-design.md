# Forma Window Management

## Status

Design for extending `FormaWindow` with coordinated window management: identity,
origin, parent–child windows, activation, stacking, minimization, modality, bounds,
and a persistent handle for every window in `FormaWindowArea`. Authoritative for
`FormaWindow`, `FormaWindowArea`, the `Forma.Framework.Windows` model, and every
component's participation in a workspace.

`FormaWindow` is built: a raised frame with chrome that docks in a layout or floats in
the top layer, moved by its title bar, resized at its edges and from the keyboard,
maximized to the viewport, with a placement a host binds and resize limits in its
options. Step 1 of the plan is built: `Forma.Framework.Windows` holds `IWindow`,
`IManagedWindow`, `WindowOrigin`, `WindowSelector` with its standing selections,
`WindowOperationResult`, and `WindowManager` with its registry, parent validation,
stacking order, activation history, modal ordering, inherited hiding, and the
ask-everyone-first close, registered as a scoped service by `AddFormaWindowServices`.
Its tests drive the manager through a fake window. Nothing in `FormaWindow` uses it
yet; that is step 2. The implementation plan at the end records what lands as each
step is taken.

## Purpose

A window on its own knows how to present itself. It does not know which window is the
current one, which windows another window opened, what closing it should take with it,
what a press elsewhere should do to it, or how a reader gets back to it once it is
minimized or sent behind something else. Those are relationships between windows, and
a relationship needs an owner that sees every window at once.

> Which window is current, which windows belong together, and how does a reader reach
> any window regardless of its state?

## Decision summary

- **Identity is a generated `Guid`.** `Id` is stable for the window's lifetime and is the
  only identity. Title and icon are presentation; windows with identical titles and
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
  `IWindow`, the way it already implements the router's owner contract.
- **The manager is a scoped service; a nested workspace cascades its own.** One manager
  per circuit is the default workspace. A host that needs a second workspace cascades
  another manager to the windows and window area inside it.
- **Stacking is re-shown, not z-indexed.** A floating window is a manual popover, and the
  top layer stacks in show order; z-index has no effect there. The manager holds the
  order and re-asserts it in the browser by re-showing popovers bottom to top.
- **A modal blocks its parent's subtree; a modal root blocks the workspace.** Blocking is
  `inert` on the blocked element plus a backdrop beneath the modal in the top layer.
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
  match, ambiguous, or declined, with the windows affected. An invalid selector or a
  window from another workspace throws.
- **Names follow the component's own conventions.** `Modal`, `Disabled`, `Active` with
  `ActiveChanged`, `Minimized` with `MinimizedChanged`, alongside the existing
  `Maximized` with `MaximizedChanged`. The area that holds every window's handle is
  `FormaWindowArea`, a sibling of `FormaCommandArea` in intent and in name.

## The existing component

`FormaWindow` keeps its public surface. Everything below extends it.

| Concern | Existing surface | What the design does with it |
| --- | --- | --- |
| Presentation | `Title` (required), `Icon` | The handle in the window area shows both and follows later changes. |
| Docking | `Mode` / `ModeChanged` | Independent of minimization, activation, and stacking. |
| Position and size | `Placement` / `PlacementChanged` | Reused for movement, bounds, and restoration. |
| Maximization | `Maximized` / `MaximizedChanged` | Preserved across minimization; restoring from one never clears the other. |
| Capabilities | `Options` | Extended with the settings consumers pass through unchanged. |
| Host commands | `Commands` / `OnCommand` | Kept beside dock, maximize, minimize, and close. |
| Closing | `OnClose` | Still raised once, only when the window actually closes. |
| Content | `Header`, `Body`, `Footer`, attributes | Unchanged; the body is never removed to hide it. |
| Browser | `Window.js` attached while floating | Extended for stacking, bounds, and remembered focus. |
| Input | A non-modal scope while floating | Extended for modality and press-away. |

The private `_titleId` identifies the title element for `aria-labelledby`. It is not
the window's identity.

## The model

### Identity and origin

```csharp
window.Id                  // This window's Guid, generated once, read-only.
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
of their windows has its own `Id`.

| Window | `OriginId` | `OriginName` | `InstanceId` | `Id` |
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
ancestor, or raised to the top of the floating windows, asks whether it may close, and
tells it to close once every affected window has accepted. `FormaWindow` implements
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
window area inside that cascade belongs to it.

### API

```csharp
manager.RegisterAsync(window)      // for the component's lifetime
manager.RefreshAsync(window)       // after its title, icon, parent, or state changed
manager.UnregisterAsync(window)    // when the host disposes it

manager.Windows
manager.GetWindowById(id)
manager.TryGetWindowById(id, out window)
manager.TryFindWindows(selector, out windows)

manager.ActivateAsync(selector)
manager.BringToFrontAsync(selector)
manager.SendToBackAsync(selector)
manager.MinimizeAsync(selector)
manager.RestoreAsync(selector)
manager.CloseAsync(selector)
```

A refresh re-validates the declared parent, brings the descendants' hiding into line,
moves a window that became modal in front of what it blocks, and lets a current window
that is no longer eligible give way. `GetWindowById` throws when absent;
`TryGetWindowById` reports false with null; `TryFindWindows` reports false with an empty
collection. Selectors replace separate
title, origin, instance, child, and descendant queries, and separate single and bulk
operation overloads.

A window offers the same operations. Without a selector, they act on the window itself;
with one, the selector is scoped to the window's own subtree.

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
| Identity | `Id` | Read-only, generated. |
| Origin | `Origin` | Parameter; resolved when absent. |
| Parent | `Parent` | Parameter, validated by the manager. |
| Activation | `Active` / `ActiveChanged` | Bound state, coordinated by the manager. |
| Minimization | `Minimized` / `MinimizedChanged` | Bound state. |
| Modality | `Modal` | Parameter; a composing dialog derives it from itself. |
| Availability | `Disabled` | Parameter, as elsewhere in the library. |
| Closing | `OnClosing` with cancelable args, then `OnClose` | Callbacks. |
| Stacking | `BringToFrontAsync()`, `SendToBackAsync()` | Methods. |
| Lifecycle | `ActivateAsync()`, `MinimizeAsync()`, `RestoreAsync()`, `CloseAsync()` | Methods, each taking an optional subtree selector. |
| Press away | `Options.PressAway` | `Ignore` or `SendToBack`. |
| Bounds | `Options.Bounds` | The viewport, or a container element. |

`Options.PressAway` and `Options.Bounds` are settings a composing consumer forwards
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
element if it still exists and can take focus; otherwise the window's initial focus
target. Modal redirection is applied before the final target is chosen.

### Stacking

Bring-to-front and send-to-back change order and nothing else. A group brought forward
keeps its internal order and lands ahead of the rest. A visible modal never goes behind
the context it blocks, and press-away cannot send it there. The manager owns the order;
the browser reflects it by re-showing the floating popovers from back to front.

Stacking applies to floating presentation. A docked window has a place in its parent's
layout; activating it focuses it and scrolls it into view without undocking it.

### Minimization

Minimizing hides the window and keeps its handle. Its component instances and content
state survive. Docked, it hides in its layout; floating, its popover is hidden and the
element stays.

Minimizing a parent hides and deactivates every descendant, at every depth, as inherited
suppression: each descendant keeps its own `Minimized`, `Mode`, `Placement`,
`Maximized`, and `Disabled`. A descendant is never visible or active under a minimized
ancestor, and one registered under an already-minimized parent starts hidden. Hidden
windows release their input scopes and modal blocking until visible again.

Restoring a parent lifts its suppression only. Descendants minimized on their own stay
minimized; those under another minimized ancestor stay hidden. Restoration activates the
selected eligible window, not every descendant. Choosing the handle of a descendant hidden
by minimized ancestors restores the chain above it, then restores and activates it,
subject to modality.

Restoring from minimization returns the preceding `Mode`, `Placement`, and `Maximized`.
A maximized window minimized and restored is still maximized. A floating placement is
reused and clamped to the current bounds. `RestoreAsync` restores from minimization; the
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
element is `inert` and a backdrop stands beneath the modal in the top layer, which is
the native dialog's own model. Choosing a handle whose window has a visible modal
descendant brings that modal forward and activates it; the parent stays blocked until the
modal finishes. Nested modals resolve through the router's precedence, so a handle cannot
reach past a modal child. A modal hidden by ancestor minimization releases its blocking
until it can be shown again.

### Bounds

`Options.Bounds` is the viewport or a container element. The script measures the region
and clamps movement, resizing, and restoration to it, reapplying on viewport and container
resize. Maximized fills the selected region. Enough of the title bar and its commands
always stays reachable to move or restore the window; when the region is smaller than the
minimum size, the minimum wins and the chrome stays reachable. Every adjustment flows
through `Placement` / `PlacementChanged`, so host and browser agree.

## The window area

`FormaWindowArea` holds one persistent handle per registered window, keyed by `Id`, for as
long as the window exists: active or inactive, front or back, minimized, hidden under a
minimized ancestor, docked or floating, enabled or disabled. Identical titles and icons
produce separate handles; nothing is deduplicated. A handle shows the window's `Title` and
`Icon` and follows changes to them. The handle is the reader's way to a window; it is not
the resize handle the window's `HandleClass` names.

Choosing a handle restores the window if minimized, brings it to the front, and activates
it. A window sent back comes forward and activates in place. Handles group by `OriginId`
or `InstanceId`; grouping is presentation and never changes parentage.

The area stands alone or inside `FormaLauncherbar`, laid out as a row, a column, or a grid.
It follows the manager whose windows it presents.

## Integration with input routing

The floating profile today dismisses on Escape and platform close requests and ignores a
press away. The design adds two things the router already models: a `Modal` scope while
`Modal` is set and visible, and a pass-through press away, which the router reports as a
dismissal with the press-away reason and the window answers by sending itself back when
`Options.PressAway` says so, never by closing. Neither routes through `OnClose`. The scope
registers for the component's lifetime while floating, activates with manager activation
rather than raw focus events alone, and is released while the window is hidden.

## Non-goals

- Application launching. `FormaLauncherbar` owns it and may host the window area.
- Persisting windows across circuits. The host binds `Placement`, `Mode`, `Maximized`,
  and `Minimized` and decides what to keep.
- Moving `FormaDialog` out of `Feedback`. It stays classified by its purpose, prompting for
  a decision; composing a window does not change that.

## Implementation plan

1. **`Forma.Framework.Windows`.** `IWindow`, `WindowOrigin`, `WindowSelector` and its
   markers, matching, `WindowManager` with registry, parent validation, stacking order,
   activation history, and `WindowOperationResult`, with unit tests for every rule above.
2. **`FormaWindow` on the manager.** Registration for the component's lifetime, `Origin`
   and `Parent`, `Active` and `Minimized` with their callbacks, `OnClosing` and recursive
   close, stacking by re-showing, remembered focus, the minimize command in the chrome.
3. **Modality, press-away, bounds.** The modal scope and backdrop, `Options.PressAway`,
   `Options.Bounds` in the script, maximization within a container.
4. **`FormaWindowArea`.** Handles, grouping, layouts, and its place in a launcher bar.
5. **Showcase.** A page covering the demo list below, walked in a visible browser.

## Demo

- The same operation by `Window` reference and by origin or instance criteria.
- Multi-match activation reporting ambiguity; an empty operation selector rejected;
  `WindowSelector.All` reaching the workspace; a selection holding a parent and a
  descendant processing each once.
- Two instances of one origin: selecting, grouping, and stacking by origin versus by
  instance; renaming the origin changing nothing; closing one root leaving the other.
- A descendant with unsaved work declining a parent's close, and a close selecting
  several roots aborting as a whole.
- Focus fallback and remembered focus; content preserved across minimization; two
  workspaces; redirection to a modal child; reclamping after a bounds resize.
- Windows with identical titles and icons holding separate handles and identities.
- Parent, child, and grandchild: recursive hiding on minimization, independently
  minimized descendants staying minimized on restoration, recursive closure, title
  queries returning every match, descendant queries walking the tree.
- Press-away sending a window back, and `FormaWindowArea` as the way back to every
  window, its handle present before and after.
