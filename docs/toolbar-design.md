# Forma Toolbar Design

## Scope

`FormaToolbar` and the item model in `Forma.Framework.Toolbar` it renders, the toggle
button every action is drawn with, and the three toolbars composed over it:
`FormaSearchToolbar`, `FormaSelectionToolbar` and `FormaStatusStrip`.

## Purpose

A row of controls is described as data and rendered by one component, so every toolbar in
the library has the same targets, the same states and the same keyboard, and a caller adds
a command by adding an item rather than markup.

> What can stand on a toolbar, how is each kind drawn, and what does the toolbar hold no
> opinion on?

## The item model

`FormaToolbarItem` is abstract, carrying `Id`, `Title`, `Tooltip`, `Icon` and
`IsDisabled`; the subtype decides the rendering. `FormaToolbarGroup` adds `Items`, so one
`Items` list mixes bare items and clusters and a caller with nothing to group skips the
ceremony; groups do not nest, and the guard says so. `FormaToolbarAction` adds `Command`,
the shortcut's display and ARIA forms, and `RequiresState`, the state the host must report
before the action can run; it is specialized by `FormaToolbarCommand`, which fires once,
and `FormaToolbarToggle`, which adds `IsActive` and an alternate title and icon shown while
on. `FormaToolbarMenu` adds `Options`, at least one, each a `FormaToolbarOption` with an
`Id`, a `Title`, a `Command`, `IsActive`, and an optional icon, preview style or swatch.
`FormaToolbarReadout` adds `Value` and `IsAnnounced`. `FormaToolbarCustomItem` adds
`Component` and `Parameters`.

## Decision summary

- **Every action is a `FormaToggleButton`.** A ghost `FormaButton` that adds only what a
  toolbar command needs, a glyph, a label shown or read aloud, and a toggle's pressed
  state, so hover, disabled and focus come from the one button and the toolbar keeps no
  button styling of its own. A toggle's on state is marked `aria-pressed` and drawn with a
  fill as well as color. `ShowLabels` puts an icon and its label in the same button,
  padded for its text; an icon-only button is drawn square, and only then carries an
  `aria-label`. An action with no icon shows its label regardless. A tooltip that would
  only repeat a visible label is dropped.
- **Each kind renders its own way.** A group becomes a `role="group"` cluster named by its
  title and separated from its neighbors by a rule; a bare item joins the row in a
  `display: contents` wrapper without inheriting a separator; a menu becomes a
  `FormaDropDownMenu` showing the active option's title or a color swatch, the first
  option standing in while none is active; a readout
  becomes a span that gains `role="status"` and a polite live region when `IsAnnounced`;
  a custom item becomes a `DynamicComponent`. The root carries `role="toolbar"` and the
  label whichever way it renders.
- **`Scrollable` decides the root.** Set, the items go on a `FormaCommandBar` rather than
  reinventing its scrolling track and overflow arrows, with `CommandBarOptions` deciding
  how the track scrolls; unset, the toolbar renders its own
  row and the items wrap, so a toolbar that cannot overflow carries none of the bar's
  observers or interop. The collapse sits once, on whichever element is the root:
  forwarded to the command bar when scrolling, owned through the toolbar's own
  `FormaVisibility` when not.
- **Shortcuts are routes.** An action's ARIA shortcut, such as "Control+Shift+B", is
  parsed into a route on the input router, registered on `Target`'s scope as
  `toolbar.{command}` once `Target` is set; the toolbar never listens document-wide, so
  shortcuts stay inert until then, and a host remaps them by action like any other.
  Control and Meta both parse as the platform's shortcut key. A matched shortcut raises
  `OnCommand` with the action's command as a click does, and is refused when the toolbar's
  `Disabled` or the action's `IsDisabled` says so, or when the state it requires is absent.
  `Hidden` releases the routes even while `Target` stays set.
- **Keys are identities.** Each item and option is keyed by its `Id` and each group by its
  own, so a host adding or removing one never tears down a neighbor's open menu.
- **The toolbar holds no opinion on what its items are.** No plugin registry, no
  multi-row layout engine, no undo or redo special case; those are `FormaComposer`'s. Titles
  are caller-supplied and rendered as given, so the host localizes them and the component
  owns no strings. `IsDisabled` lets one item stand down without disabling the whole
  toolbar, and `ActiveStates`, the states the host reports by id, turns a toggle or an
  option on and holds back an action whose `RequiresState` is not among them.

## The composed toolbars

- **`FormaSearchToolbar`** pairs `FormaSearchBox` with a toolbar of list actions rendered
  with `ShowLabels`, so each action reads as a named button. The search box is handed its
  options whole and owns the debounce. The search field is not optional, since a search
  toolbar with only actions is a toolbar; `OnSearch` is required and so is the
  placeholder, the field's only accessible name.
- **`FormaSelectionToolbar`** pairs with `FormaDataGrid`'s checked selection, taking only the
  count, or a `Caption` standing in for the count text, so it stays non-generic, renders
  its actions with `ShowLabels`, and keeps `FormaCloseButton` beside them as the clear
  control while `OnClear` is handled. It collapses through
  `grid-template-rows` rather than a positioned cross-fade, so no wrapper positioning is
  imposed on the caller, and `inert` sits on the controls alone, leaving the caption's
  live region in the accessibility tree so the first count change is announced.
- **`FormaStatusStrip`** is a toolbar of readouts and commands whose announcer is the
  toolbar's own. `EmptyMode` decides whether an empty `Items` renders nothing, the
  default, or keeps the strip's chrome as an empty bar; either way no nested toolbar is
  rendered while there is nothing to show. `Hidden` is independent of `EmptyMode`: it
  collapses the strip even while it holds items, keeping it mounted. `Disabled` forwards to
  the inner toolbar; `Hidden` and `Animated` do not, since the strip's own `FormaVisibility`
  already collapses everything inside it and a second collapse would run two transitions
  over the same content.
