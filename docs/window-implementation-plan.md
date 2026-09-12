# Forma Window Implementation Plan

The plan for building window management as [the window design](window-design.md) states it, and the record of what has landed.

## Status

`FormaFrame`, `FormaChrome` and `FormaWindow` are built: a frame is a raised backdrop
with a box of its own, a chrome is a frame whose drag region is a title bar, and a
window is a chrome floated in the top layer, moved by its title bar, resized at its edges
and from the keyboard, maximized to its bounds, with a placement a host binds and resize
limits in its frame options. Step 1 of the plan is built: `Forma.Framework.Windows` holds `IWindow`,
`IManagedWindow`, `WindowOrigin`, `WindowSelector` with its standing selections,
`WindowOperationResult`, and `WindowManager` with its registry, parent validation,
stacking order, activation history, modal ordering, inherited hiding, and the
ask-everyone-first close, registered as a scoped service by `AddFormaWindowServices`.
Its tests drive the manager through a fake window. Step 2 is built: `FormaWindow` is a
managed window of its workspace, registered for its lifetime with the parent and origin
it declares, with `Active`, `Minimized`, `Modal` and `Disabled`, a cancelable `OnClosing`
checked across the subtree before `OnClose`, a minimize command in its chrome, its own
operations scoped to its descendants, stacking asserted by re-showing its popover, and
focus remembered and returned on activation. Step 3 is built: a modal window registers a
modal scope, keeps Tab inside itself, blocks its parent's subtree or the workspace, the
blocked windows going `inert` and a root modal standing on a scrim; a press away sends
a window back when its options say so; a window is held, restored and maximized within
its bounds, the viewport or an element of the page; a window activates itself when it
opens; and a lockable window can be held where it is, neither moved, resized
nor maximized until unlocked. Step 4 is built: `FormaRestArea` holds one handle per
registered window in registration order, pressed while current, marked while minimized
or hidden, grouped by origin or instance, laid out as a row, column or grid, and
following the workspace cascaded to it; its place in a launch bar is still open. Step 5
is built: the Showcase's window page renders every item of the demo list below but the
last, with an operation log that reports each selector's outcome, and was walked in a
visible browser. Step 6 is built: `FormaFrame`
owns the box, `FormaChrome` the title bar and the bands, and `FormaWindow` composes the
chrome and has its frame drive the window's box; a window always floats, and the Showcase
has a frame page and a chrome page beside the window page. `IWindow` keeps `IsFloating`,
which `FormaWindow` answers true and the manager reads when it re-shows the floating
windows. The steps below record what landed at each.

## Steps

1. **`Forma.Framework.Windows`.** `IWindow`, `WindowOrigin`, `WindowSelector` and its
   markers, matching, `WindowManager` with registry, parent validation, stacking order,
   activation history, and `WindowOperationResult`, with unit tests for every rule the design states.
2. **`FormaWindow` on the manager.** Registration for the component's lifetime, `Origin`
   and `Parent`, `Active` and `Minimized` with their callbacks, `OnClosing` and recursive
   close, stacking by re-showing, remembered focus, the minimize command in the chrome.
3. **Modality, press-away, bounds.** The modal scope and scrim, `Options.PressAway`,
   `Options.Bounds` in the script, maximization within a container.
4. **`FormaRestArea`.** Handles, grouping, layouts, and its place in a launch bar.
5. **Showcase.** A page covering the demo list below, walked in a visible browser.
6. **Frame, chrome and window.** `FormaFrame` takes the box out of `FormaWindow` and
   `FormaChrome` the title bar and the bands; the window composes the chrome, keeps the
   workspace membership and the top layer, and has its frame drive the window's box.
   The mode, the embed command and every floating branch go, and the classes, tokens,
   tests and Showcase pages split along the same lines.

## What the split removes

- `WindowMode`, `Mode` and `ModeChanged`, the `Embeddable` option, and the embed and
  float commands with their pin glyph and their strings.
- Every branch in the window that asked whether it was floating: it always is.
- The window's own drag, resize, keyboard and fitting, which are the frame's, and the
  resize options, limits and bounds of `WindowOptions`, which are `FrameOptions`.

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
- Press-away sending a window back, and `FormaRestArea` as the way back to every
  window, its handle present before and after.
- Frames in a stack with a splitter between them, a hidden and a disabled frame, and a
  chrome's title bar carrying the host's commands, on the chrome page. A window's title
  bar carrying the host's commands before lock, minimize, maximize and close is covered
  by the window's tests and is not in the Showcase.
