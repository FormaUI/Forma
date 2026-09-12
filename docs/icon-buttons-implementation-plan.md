# Forma Icon Buttons Implementation Plan

The plan for the icon-button set that [the icon buttons design](icon-buttons-design.md)
states, and the record of what has landed.

## Status

`FormaCloseButton`, `FormaEyeButton` and `FormaOverflowMenu` are built. The first two
were retrofitted onto the components that had drawn their own: the close button replaced
three hand-drawn crosses that had drifted in glyph and focus ring, and the eye button
replaced the password box's hand-rolled ghost button. The rest of the set is on the roadmap.

## Open

- Whether `FormaDeleteButton` pins the danger variant, right for a lone destructive
  button but loud repeated down every grid row, or stays ghost and leaves emphasis to the
  confirm step.
- Whether `FormaViewButton` is a toggle or a two-option segmented control showing both
  modes at once, which is guesswork until the managers exist.
