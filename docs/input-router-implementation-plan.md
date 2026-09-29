# Forma Input Router Implementation Plan

The plan for bringing every input a component acts on under the router that [the input router design](input-router-design.md) states, and the record of what has landed. Section names refer to the design.

## Status

The router arbitrates the keys, presses, wheel, drags and drops of most behaviors: the drag gesture, the drop zone, focus, reorder, the selection keys, the selection follower and the wheel register with it. A few inputs are still acted on outside it, each deciding for itself whether it owns an input the design says only the router decides: the keys a popover, a search box, the pager and the tree's type-ahead read through Blazor's own handlers, the pan gesture's presses, and selection by click.

Phase 1 has landed: the guard is gone, Enter on a data view's marker is the selection behavior's activation route, and the pager's jump is an Enter route on its box. The selection behavior now attaches under checked selection as well as item selection, declaring only the activation there, since under checked selection the boxes select and a press or an arrow has nothing to do; the item comes from the marker the key was pressed on, which the router does not pass to a handler in .NET, so activation is told to the data view by the script rather than routed to a handler of its own.

## Phases

1. **Enter.** ✅ The guard that stops Enter submitting the form around a control leaves the library, its script, wrapper, tests and inventory row with it. Enter becomes a route where a component acts on it: the selection behavior's activation of the item whose marker holds the focus, which replaces the views' own Enter handlers on their markers, and the pager's jump. The scripting guide gains the rule that input a component acts on is a route or an active registration on the router. Gate: Enter on a marker and in the pager's jump input act without submitting a surrounding form, and nothing in the library listens for Enter outside the router.
2. **The remaining keys.** The popover's, the search box's and the pager's keys become routes on their scopes, the tree's type-ahead a route matching any printable character, and the pan gesture an active registration while it pans, as the drag gesture is. Gate: no library component reads a key through its own handler, and Escape during a pan resolves as it does during a drag.
3. **Click selection through the router.** Click becomes a binding family of its own beside the keyboard, pointer, wheel and drop families, dispatched on the click event rather than as shorthand for a pointer's release, so a click raised from the keyboard resolves like any other with no preceding press required. A press resolves its scope and lets a gesture begin without committing a selection; an active registration that claims the gesture, a drag or a pan, marks the click that follows it suppressed, tied to its pointer and the element pressed, and a click raised from the keyboard is never suppressed; the click then dispatches its route once suppression has been applied. A scope that goes away or turns inactive drops the click pending on it rather than letting it fall to the scope beneath. The selection behavior's click becomes a click route and its shift-press, which stops the browser selecting text, a pointer route at press time as it is today; the views' own presses on an item, the press that activates it and the double press that activates it under a selection, become click routes too. Gate: a short drag inside a row selects nothing, a click after it selects normally, a keyboard click is never swallowed, and Enter and a synthesized click never run one action twice.

## Open decisions

None.
