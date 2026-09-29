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

- **A manual popover in the top layer.** The element carries `popover="manual"`, stays
  mounted and shows its content only while open, so a scrolling or clipping ancestor
  cannot cut it off: it works inside a grid's scroll region, a dialog body or any
  `overflow: auto` box. `hidePopover()` on close is what lets the element stay mounted
  between openings. Light-dismiss is the input router's rather than the platform's, since iOS 17 to
  18.2 ignores a tap outside a popover.
- **Anchored by script.** The top layer keeps no layout tie to the anchor, so the shared
  anchored-popover behavior measures and applies the position: it hangs from the edge of
  `Anchor` that `Alignment` names, its reading-start or reading-end edge, mirrored in RTL,
  or by default the edge away from the nearer side of the viewport so the popover extends
  toward the room; it flips above when there is no room below, clamps into the viewport,
  and follows scroll, resize and its own content's growth. The trigger handed to
  `OpenAsync` is where focus returns, and `OnOpened` fires once the popover is positioned,
  for content that takes focus or attaches a behavior of its own. One positioning path
  serves every browser rather than a CSS and JS split.
- **Dismissal through the router.** The popover registers an input scope and activates it
  while open, so Escape and a press away are answered by the router wherever focus is; a
  consumer cannot leave them unwired, and a dialog hosting the popover keeps its own
  Escape. The trigger counts as inside the popover, so a press on it toggles rather than
  dismisses. A press away passes through: it dismisses the popover and then belongs to
  whatever was pressed, so one press on another control closes the popover and acts on the
  control, and a press on another popover's trigger swaps the popovers. `PressAway` turns
  press-away dismissal off. The content's own keys, a list's arrows, Home, End and Tab,
  are routes it declares through `InputRoutes` and answers through `OnInputAction`.
- **One `CloseAsync`, and focus goes back only when it was inside.** With `ReturnsFocus`,
  closing returns focus to the trigger when focus is inside the popover at the time,
  Escape, a chosen option, a picked day, and leaves focus alone otherwise, so Tab moving on
  and a trigger click closing its own popover never have focus pulled back; a press away
  never moves focus.
- **`KeepsFocus` wraps Tab.** A menu wraps Tab among the popover's controls; a list that
  closes on Tab leaves it off.
- **Content decides its own look.** The popover draws no box of its own.
- **Not a dialog.** The dialog is a native `<dialog>` that gets its scrim, focus trap,
  Escape and focus restoration from the browser and makes the page inert; the popover is a
  manual popover that gets only the top layer from the browser and hangs from a control
  while the page stays live.
