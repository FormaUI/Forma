# Forma Popover Design

## Scope

`FormaPopover`, the one owner of the popover contract every dropdown, menu and picker in
the library hangs from, and the anchored placement behavior it composes.

## Purpose

A control that opens a surface beside itself needs the same four things every time: a
place in the top layer that no ancestor can clip, a position that follows its anchor, focus
that comes back to the right control, and dismissal that agrees with every other open
surface. One component owns them so no consumer can leave one out.

## Decision summary

- **A manual popover in the top layer.** The element stays mounted and shows its content
  only while open, so a scrolling or clipping ancestor cannot cut it off: it works inside
  a grid's scroll region, a dialog body or any scrolling box. Light-dismiss is the input
  router's rather than the platform's, since some iOS versions ignore a tap outside a
  popover.
- **Anchored by script.** The top layer keeps no layout tie to the anchor, so the shared
  anchored-popover behavior measures and applies the position: it hangs from the anchor's
  reading-start or reading-end edge, mirrored in RTL, as the consumer chooses, or by
  default from the edge away from the nearer side of the viewport so the popover extends
  toward the room; it flips above when there is no room below, clamps into the viewport,
  and follows scroll, resize and its own content's growth. The trigger that opens it is
  where focus returns, and the consumer hears once the popover is positioned, for content
  that takes focus or attaches a behavior of its own. One positioning path serves every
  browser.
- **Dismissal through the router.** The popover registers an input scope and activates it
  while open, so Escape and a press away are answered by the router wherever focus is, for
  every consumer, and a dialog hosting the popover keeps its own Escape. The trigger
  counts as inside the popover, so a press on it toggles. A press away passes through: it
  dismisses the popover and then belongs to whatever was pressed, so one press on another
  control closes the popover and acts on the control, and a press on another popover's
  trigger swaps the popovers. A consumer can turn press-away dismissal off. The content's
  own keys, a list's arrows, Home, End and Tab, are routes it declares and answers through
  the popover.
- **Every close takes one path, and focus goes back when it was inside.** However the
  popover closes, a popover that returns focus gives it back to the trigger when focus is
  inside the popover at the close, on Escape, a chosen option or a picked day, and leaves
  focus where it is otherwise, so Tab moving on and a trigger click closing its own
  popover keep focus where the reader put it; a press away leaves focus with what was
  pressed.
- **A popover that keeps focus wraps Tab.** A menu wraps Tab among the popover's controls;
  a list that closes on Tab leaves it unwrapped.
- **Content decides its own look.** The popover draws no box of its own.
- **Beside the dialog.** The dialog is a native `<dialog>` that gets its scrim, focus
  trap, Escape and focus restoration from the browser and makes the page inert; the
  popover is a manual popover that gets the top layer alone from the browser and hangs
  from a control while the page stays live.
