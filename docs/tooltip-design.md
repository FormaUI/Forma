# Forma Tooltip Design

## Scope

`FormaTooltip`, a short text that describes a control, shown beside it on hover, focus and tap; `IDisableable`, the
contract every control that can be disabled implements, whose reason for being disabled shows through the tooltip; and
the reason a collection's disabled item gives the same way.

## Purpose

A control a reader can't use, for a reason they can't see from where they are, must say why where they meet it: by a
mouse, a finger or the keyboard. The browser's `disabled` attribute takes a control out of the tab order and the
pointer's reach, so a reason attached to it is never met. One tooltip, and one contract every disableable control
keeps, give every control the same way to say why.

## The tooltip

- **It describes its trigger.** The tooltip is `role="tooltip"`, and the trigger names it in `aria-describedby`, so a
  screen reader hears the text as the control's description whether or not it's shown.
- **Shown on hover, focus and tap.** A pointer resting on the trigger shows it after a short delay; keyboard focus shows
  it at once; under a coarse pointer, a tap on the trigger shows it, and a tap elsewhere hides it.
- **Dismissible, hoverable and persistent,** as WCAG 1.4.13 asks: Escape hides it without moving the focus, the pointer
  can move from the trigger onto the tooltip without it hiding, and it stays until the trigger loses both hover and
  focus, or Escape.
- **Text alone.** A tooltip holds plain text and nothing to press. A surface with controls in it is a popover.
- **Placed in the top layer.** It hangs from its trigger through the popover's anchored placement, so no scrolling or
  clipping ancestor cuts it off: above the trigger, flipped below where there is no room, clamped into the viewport,
  and mirrored right to left.
- **On every page.** Its showing is a script behavior the interop package's initializer attaches on a statically
  rendered page too, as the share menu's commands come alive there; with no script, the description still reaches
  assistive technology through `aria-describedby`. The initializer begins importing the script as it loads, Blazor
  starts once the import has settled, loaded or failed, and the initializer installs the script once Blazor has started,
  in a Web App and in a web view, so the first tap on a started page finds it and a host whose file server refuses a
  module still starts Blazor.
- **Drawn from the tokens.** An inverse surface, the small type, the small radius and the spacing scale, wrapping at a
  readable width, with a border of its own in forced colors.

## Disabled with a reason

- **One contract.** `IDisableable` has two members: `IsDisabled`, whether the component is disabled, and
  `DisabledReason`, why. Every control that takes a `Disabled` parameter implements it, explicitly, answering from
  its `Disabled` parameter and a `DisabledReason` parameter beside it; a record that carries `IsDisabled`, as a toolbar
  item does, carries `DisabledReason` too.
- **One helper draws it.** `DisabledReasons` holds what every implementer draws: whether it shows its reason, its
  tooltip's id, `{elementId}-disabled-reason`, and its description with that id added. The input base offers
  `ShowsDisabledReason` and the tooltip itself.
- **A region's reason is its content's.** The frame, the chrome and the window take `Disabled` to make the whole region
  inert, and what a reader reaches in one is its content, whose own controls say why; so a region takes `Disabled`
  alone.
- **A collection's item gives its reason as a control does.** The data view takes `ItemDisabledReason`, a function of
  the item beside `IsItemDisabled`. A disabled item with a reason names its tooltip in its element's description and
  in its selection box's, and shows it on hover, focus and tap; a grid's row holds the tooltip in its first column's
  cell, since a row holds only cells. A catalog item carries a `DisabledReason`, which the explorer hands on.
- **A reason keeps the control reachable.** A component disabled with a reason stays in the tab order and the pointer's
  reach: it writes `aria-disabled="true"` in place of the `disabled` attribute, does nothing when activated, pressed or
  typed into, names its tooltip in `aria-describedby`, and shows the reason as its tooltip on hover, focus and tap.
- **A component disabled without a reason** writes the `disabled` attribute, as before.
- **A reason without `Disabled` shows nothing,** since it says why a control is disabled.
- **A button showing its reason drops a host's `title`,** so the reason is the one tooltip it shows.
- **A field keeps its value.** An input disabled with a reason holds its value as a read-only one does, so a form posted
  without script carries it, where a `disabled` input's value isn't posted.
