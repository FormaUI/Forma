# Forma Icon Buttons Design

## Scope

The icon-button set: `FormaCloseButton`, `FormaEyeButton`, `FormaAddButton`,
`FormaDeleteButton`, `FormaDuplicateButton`, `FormaEditButton`, `FormaPublishButton`,
`FormaRefreshButton`, `FormaShareButton`, `FormaUpdateButton` and `FormaViewButton`, and
the menu triggers that follow the same rule, `FormaOverflowMenu` and `FormaSortMenu`.

## Purpose

A row's actions are one vocabulary of glyphs with one target size and one set of states,
so a reader learns each once and no page draws its own.

## Decision summary

- **Each is a thin wrapper over `FormaButton`.** A ghost icon-only button supplying one
  glyph and its accessible name, owning no colours and shipping no stylesheet; every visual
  decision belongs to the button. Retinting for a saturated surface goes through the
  button's ghost colour knobs rather than a fork.
- **The accessible name is required, not defaulted.** A wrong name is worse than an
  obvious compile error, and the call sites genuinely differ.
- **The glyph names the action's meaning, and avoids the glyph a neighbour owns.** Delete
  is a trash can, not a cross: a cross reads as dismiss, and borrowing it for an
  irreversible action is how a reader deletes what they meant to close. Duplicate is two
  overlapping sheets, not a second pencil, so making another can never be misread as
  changing this one. Share is three connected nodes, not the arrow leaving a box, which is
  the external-link glyph a row may already carry for "open on the public site". Update
  is the check mark, the commit half of an inline edit opposite the cross that discards it.
- **A stateful button draws its state and is named for what a press does.** The eye is
  struck through while what it guards is hidden, and publish reuses that strike-through for
  unpublish, so one control reads as two states rather than two icons. The view button
  draws the mode it switches to and names that mode, since an icon-only toggle that draws
  its current state cannot say which it is.
- **Labelled where the action is primary.** Add is the one of the set that is not always
  icon-only: a primary add is usually labelled in place, so it takes optional child content
  and drops `IconOnly` when given it. Publish is labelled in an editor footer and a glyph
  alone in a dense row.
- **The button owns no policy.** Confirmation before a delete stays the caller's through
  `IConfirmDialogService`, since a button that confirms on its own cannot be composed into
  a bulk action that confirms once. What duplication means, whether an edit navigates or
  begins in place, and what sharing means, which needs a product decision about
  `navigator.share()` and its fallback, are all the caller's. Scheduling stays out of the
  publish button: draft, scheduled and published are three states and a button offers
  two, so "Schedule…" belongs in a menu beside it.
- **Refresh forwards `Busy`.** A slow refresh spins and announces `aria-busy` rather than
  looking dead; it is the one of the set where busy is the point.
- **The menu triggers follow the same rule.** The overflow menu is the ellipsis,
  horizontal in a toolbar and vertical in a grid row, where a column is narrow and a row
  is tall; the sort menu is two opposed arrows, and is a menu rather than a button because
  sorting offers a choice rather than performing one action. A grid that already has
  sortable column headers keeps using them, since two controls over one sort state is a
  synchronization problem with no upside.
