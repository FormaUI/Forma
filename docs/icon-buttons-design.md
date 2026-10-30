# Forma Icon Buttons Design

## Scope

The icon-button set: `FormaCloseButton`, `FormaEyeButton`, `FormaAddButton`,
`FormaDeleteButton`, `FormaDuplicateButton`, `FormaEditButton`, `FormaPublishButton`,
`FormaRefreshButton`, `FormaShareButton` and `FormaUpdateButton`, and the menu trigger
that follows the same rule, `FormaOverflowMenu`.

## Purpose

A row's actions are one vocabulary of glyphs with one target size and one set of states,
so a reader learns each once and no page draws its own.

## Decision summary

- **Each is a thin wrapper over `FormaButton`.** A ghost icon-only button supplying one
  glyph and its accessible name, owning no colors and shipping no stylesheet; every visual
  decision belongs to the button. Retinting for a saturated surface goes through the
  button's ghost color knobs rather than a fork.
- **The accessible name is required.** The call sites genuinely differ, and a missing name
  is a compile error where a default would be a wrong name read aloud.
- **The glyph names the action's meaning, and avoids the glyph a neighbor owns.** Delete
  is a trash can, not a cross: a cross reads as dismiss, and borrowing it for an
  irreversible action is how a reader deletes what they meant to close. Duplicate is two
  overlapping sheets, not a second pencil, so making another can never be misread as
  changing this one. Share is three connected nodes, not the arrow leaving a box, which is
  the external-link glyph a row may already carry for "open on the public site". Update is
  the check mark, the commit half of an inline edit opposite the cross that discards it.
- **A stateful button draws its state and is named for what a press does.** The eye is
  struck through while what it guards is hidden, and publish reuses that strike-through
  for unpublish, so one control reads as two states rather than two icons.
- **Labeled where the action is primary.** Add is the one of the set that is not always
  icon-only: a primary add is usually labeled in place, so it takes an optional label and
  shows it beside the glyph. Publish is labeled in an editor footer and a glyph alone in a
  dense row.
- **The button owns no policy.** Confirmation before a delete stays the caller's through
  the confirm dialog service, so a delete can join a bulk action that confirms once for
  every item it holds. What duplication means and whether an edit navigates or begins in
  place are the caller's, and the share button opens `FormaShareMenu`, whose
  [design](share-design.md) decides what sharing reaches. Scheduling stays out of the publish
  button: draft, scheduled and published are three states and a button offers two, so
  "Schedule…" belongs in a menu beside it.
- **Refresh shows it is busy.** A slow refresh spins and announces that it is busy, so it
  reads as working; it is the one of the set where busy is the point.
- **The overflow menu follows the same rule.** It is the ellipsis, horizontal in a toolbar
  and vertical in a grid row, where a column is narrow and a row is tall.

## Open decisions

- **Does `FormaDeleteButton` pin the danger variant?** Right for a lone destructive button,
  and loud repeated down every row of a grid. The alternative leaves it ghost like the rest
  of the set and puts the emphasis on the confirm step instead.
