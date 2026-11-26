# Forma Data View Implementation Plan

The steps that build [the data view design's narrow width](data-view-design.md#at-a-narrow-width), in order, each
landing with its tests, its browser checks and its gate.

## Steps

1. ✅ **The grid's narrow rows.** `FormaDataGrid` takes `ThumbnailTemplate`, `ItemTemplate` and `ActionsTemplate`, and
   draws narrow rows from them and from `ItemName` once its visible columns no longer fit side by side, each at the
   width its content takes on one line; it draws its columns again once its width holds the columns it measured as it turned narrow.
   The name keeps to one line and the detail to two at most, each cut short with an ellipsis. bUnit tests of both
   forms and of the switch, a browser check at a phone's width in both directions, and a Showcase example.
2. ✅ **The list and the cards at a narrow width.** Below `36rem` of their own width, a container query lays the list's
   rows in one column, their name and detail held as the grid's are, and the list takes `ActionsTemplate`; the cards
   keep their tiles and their actions bar.
3. ✅ **Selection on demand.** A narrow view draws no standing boxes. `Selecting`, with `SelectingChanged`, says whether
   its items draw their boxes: a long press on an item starts it with that item checked, a box Tab reaches out of
   sight and Space checks does from the keyboard, and it ends once nothing is checked; started by a command, it lasts until a command stops it. A row's box stands at its
   start, pushing its content toward its end, and a card's on its thumbnail. The explorer's selection mode sets it.
4. ✅ **The view's menu.** A narrow view's head holds the button of three dots in place of the actions column, opening
   Select and the host's `SelectionCommands`, `DataViewCommand<TItem>` records, each enabled by the selection, its
   `AppliesTo` and `TakesMany` and the host's `IsDisabled`, a disabled one saying why; `OnSelectionCommand` runs one
   with the selected items.
5. ✅ **A row's own actions by a swipe and by the focus.** A swipe toward a narrow row's inline start, a route on the
   input router, slides it aside to show its actions, and a swipe back, a press elsewhere or Escape slides it home; a
   row holding the keyboard's focus shows its actions' button at its end, and Shift+F10 and the Menu key open them as
   they do. Script tests of the gesture and browser checks under touch, on Android among them, in both directions.
6. **Every runtime.** Browser checks of the narrow rows, the menu, the swipe and the long press with their commands,
   in every render mode and in a web view, on Android, at a phone's width and a tablet's, in both directions; the
   data view's README, the changelog and the features status.
