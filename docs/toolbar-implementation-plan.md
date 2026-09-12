# Forma Toolbar Implementation Plan

The plan for the toolbars that [the toolbar design](toolbar-design.md) states, and the
record of what has landed.

## Status

The item model, `FormaToolbar` with both roots, shortcuts as routes, and the three
composed toolbars are built. `FormaSearchToolbar` was renamed from `FormaToolbar` once
that name was needed for the generic, item-driven toolbar, and rebuilt on it;
`FormaSelectionToolbar` was renamed from `FormaContextToolbar`, since "context" named the
circumstance rather than the subject.

## Open

- `FormaToolbar` sets `role="toolbar"` without the arrow-key roving focus an ARIA
  toolbar owes; the roles its children carry are correct, but keyboard navigation between
  them is plain tab order. The gap is the toolbar's to close, and the composed toolbars
  inherit it.
