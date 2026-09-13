# Forma Toolbar Implementation Plan

The plan for the toolbars that [the toolbar design](toolbar-design.md) states, and the
record of what has landed.

## Status

The item model, `FormaToolbar` with both roots, shortcuts as routes, roving focus, and the
three composed toolbars are built. `FormaSearchToolbar` was renamed from `FormaToolbar` once
that name was needed for the generic, item-driven toolbar, and rebuilt on it;
`FormaSelectionToolbar` was renamed from `FormaContextToolbar`, since "context" named the
circumstance rather than the subject. Roving focus arrived last, as a roving mode of the
shared `Focus` behavior rather than key handling of the toolbar's own.

## Open

Nothing.
