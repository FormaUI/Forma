# Forma Data View Implementation Plan

The plan for building the data view that [the data view design](data-view-design.md) states: the surveys it rests on, the phases it lands through, their state, and where the build departed from the design. Section names refer to the design.

## What the plan rests on

### What Coi shipped

Every tenant admin list is one `GridView` under a card: pages, products, orders,
abandoned orders, leads, comments, coupons, users, invoices and design sets, and a
product picker in a modal. Each binds to a repository data source that sorts and pages on
the server, twenty rows to a page, and each is laid out the same way: a spacer column at
either edge, a checkbox column where rows can be acted on together, the record's name as
a link to its editor, a few bound cells, and a run of icon-only action cells at the end,
view, edit, duplicate, approve, delete. The cells that recur across pages are a
thumbnail, a date in a short format, a currency amount, a stock count drawn red at zero,
a status badge built as a tinted pill in code-behind, and a lock or a home glyph for a
row's state.

A row is clickable anywhere: a click on any cell runs the row's main trigger, which
navigates to the editor, opens a modal, or toggles the row's checkbox. Checking a row
tints it and swaps the toolbar above the grid for the bulk actions; the header checkbox
checks the rows on the page. Two row tints mark state on their own, a green bold row for a
new order or lead and a red row for an expired coupon, and a row's editor button appears
on hover beside its name. Sorting is a postback on a few columns, marked in the header by
one neutral glyph that never shows the direction or which column is active. Paging is
the framework's numeric pager, page numbers alone, and the page a reader was on travels
back in the query string when they return from an editor. The header is not sticky, the
table scrolls sideways on a phone with no column ever hidden, the empty state is one
sentence, and loading is a spinner in the toolbar or a bar across the top on a phone.
Actions carry no text and no accessible name; their labels are tooltips drawn on hover.

The picker in the modal is the same grid with its header off, its rows dense and its
click-through disabled. The image library lists every upload as a tile with its name, the
cards presentation with nothing to sort or group it by. The news ticker's list is a grid
whose rows are reordered by dragging, which is a different component's job. The
super-admin lists are an older pattern, an edit form above a grid with move-up and
move-down buttons.

### Parity with the framework's grid

The grid over the engine replaces the framework's grid, so everything a caller could do with
that grid they can do with these two, under the library's names. The inventory, feature by
feature, so nothing is dropped by omission; the engine's members are the data view's, the
column's are the grid's.

| The framework's grid | The data view and its grid |
|---|---|
| `Items` and `ItemsProvider`, one or the other | The same two ways in, one supplied |
| `IAsyncQueryExecutor`, so a queryable backed by a store is counted and read asynchronously | `IDataViewQueryExecutor`, a service the host registers for its store, with an in-memory default the library carries |
| `GridItemsProviderRequest`: start, count, sort column, direction, cancellation, `ApplySorting` and `GetSortByProperties` | `DataViewRequest` with the same members, so a provider applies the sort to its own queryable or reads the sorted properties for a query it writes itself |
| `GridItemsProviderResult`: items and total | `DataViewPage`: items and an optional total, a slice shorter than asked for ending a set it cannot count |
| `PaginationState` and `Paginator`, with a summary template | `PageSize`, a bindable `Page`, the reported total, and `FormaPagination` with its options |
| `Virtualize`, `ItemSize`, `OverscanCount` | `Virtualize`, `ItemHeight`, `OverscanCount` |
| `AnchorMode`, `InitialItemIndex`, `ItemComparer`, `ScrollToItemAsync` | The same anchoring set: where the viewport holds when items arrive during virtualization, the item to open on, the comparer that tells a prepend from an append, and scrolling to an item on request |
| `ItemKey` | `ItemKey` |
| `RowClass`, `OnRowClick` | `ItemClass`, `OnItemClick` on the engine, which the grid draws on its rows |
| `Class`, `AdditionalAttributes` | The host surface every component has |
| `Theme` | The design tokens; a theme restyles the views by redefining them |
| `QueryParameterNameOptions`: the page, sort and direction parameter names | `AddressOptions`: the prefix and the names, with the active view beside them |
| `RefreshDataAsync`, `SortByColumnAsync`, `ShowColumnOptionsAsync`, `HideColumnOptionsAsync` | `RefreshDataAsync` and `SortByAsync` on the engine; `SortByColumnAsync`, `ShowColumnOptionsAsync` and `HideColumnOptionsAsync` on the grid |
| `ColumnBase`, public and derivable, with `CellContent`, `HeaderContent` and `IsSortableByDefault` | `FormaColumnBase`, public and derivable, with the same overrides |
| `Title`, `Class`, `Align` | `Title`, `Class`, `Align`, logical |
| `Sortable`, `SortBy`, `InitialSortDirection`, `IsDefaultSortColumn` | The same four, and a `SortName` apart from the title that the address carries |
| `GridSort` with `ByAscending`, `ByDescending`, `ThenAscending`, `ThenDescending` | `DataViewSort` with the same compound keys, which every view sorts by |
| `HeaderTemplate` | `HeaderTemplate` |
| `ColumnOptions`, an options surface a header button opens | `ColumnOptions`, opened from the header as the library's popover |
| `PlaceholderTemplate` for a virtualized cell not yet loaded | `PlaceholderTemplate` |
| `PropertyColumn` with `Property`, `Format`, `SortBy` | `FormaPropertyColumn` with the same, plus its editor |
| `TemplateColumn` with `ChildContent`, `SortBy` | `FormaTemplateColumn` with the same |

What the framework's grid could not do and this design adds is the rest of it: the views
beside the grid, selection, editing, priority, frozen columns, grouping, the empty and busy
states, and the fit.

## Phases

Each phase ends buildable, tested and reviewable, with a showcase page showing every state the phase adds.

1. **The project, the engine and the grid.** ✅ The engine with its contract pair and view registration, the source with its query executor, the request and page records, the busy and empty states, refresh and the page giving way, the sort and the page, the address, the footer, the scroll region with the height reservation, and the grid as the first view with its columns, header and rows; the framework's grid left the product, the Showcase and the test projects. Departures: the table is a part of the grid, `FormaDataGridTable`, declared after the columns so it renders once they have registered, as are its row and column header, the three sharing one stylesheet scope; and a data view that also sets `Status` wraps its view in `ChildContent`, as Razor requires once a second fragment is named. Gate: every scenario the grid's Showcase page showed renders as it did, and every existing test passes against the owned markup.
2. **Selection.** ✅ Checked and pointer selection as the engine's, the four select-all modes with the menu as the default, the selection methods, selection over a provider, and the selection kept by key across paging, sorting and fresh copies of the items. Departure: the master control reaches the page in the menu and pager modes, so clearing it clears the page alone, which the engine exposes as `ClearPageAsync` beside the methods the design names. Gate: the selection scenarios in each mode, a provider with checked selection, and a selection kept across a page change.
3. **Virtualization.** ✅ Virtualization over a set and over a provider in the view's units, with the placeholder, the anchoring set, the initial item, scrolling to an item, the item count announced, and what the master control reaches where nothing pages. Departure: a view registers with the viewport it keeps, a contract the design did not name, through which the engine scrolls and asks a virtualizing view to fetch its slices again. Gate: a hundred thousand rows scroll in a bounded box, open on a given row and hold their viewport when rows are prepended.
4. **Grouping.** ✅ Grouping with its headings, folding and the fold kept across views. Gate: a grouped grid folds and unfolds from the keyboard.
5. **Fitting a width.** ✅ Column priority, the width measurement, the columns menu, frozen columns through the freeze pane, the dense rows and the headerless grid. Departures: the pane is the one box the rows scroll in both ways, since a sticky header inside a pane that scrolls sideways alone would stick to nothing, so the freeze pane gained the option; and the header row always renders, collapsed when the header is off, since the widths are measured from its cells. Gate: a narrow scenario drops its lowest columns and lists them, a frozen scenario holds its name column while scrolling, and the picker scenario shows the headerless dense grid.
6. **Inline editing.** ✅ The edit session and the property column's editors, the library's text box, checkbox and date picker where it has one for the member's type. Departure: a number, a date with a time and a time keep the framework's inputs in the text box's look; see the open decisions. Gate: the editing scenario runs inside a form.
7. **The list and the cards, with the view switch.** ✅ The two views with their item templates and densities, the cards' packing, and the view switch bound to the active view and written to the address. Gate: one set shown three ways on the engine's Showcase page, switched from the view switch and reproduced from a link.
8. **The hierarchy and the tree.** ✅ The engine's hierarchy with its current node and children loaded on demand, and the tree with its disclosure, keyboard pattern and type-ahead. Departures: a tree that chooses the current node registers as a companion, drawn beside the active view and left out of the switch; the roots have members of their own on the contracts rather than standing for a null node; a sidebar's choice is the current node alone, so it leaves the selection of the view beside it untouched, and it opens a node's ancestors to reveal a node the view beside it moved into; and in a view with a selection a double press, or Enter on the item's marker, activates the item. Gate: a folder hierarchy opened in the tree while the grid shows the current node's children.
9. **The sort menu.** ✅ The sort menu over the engine's sort. Departures: the menu reaches the data view through a sorter contract of its own, as the view switch does through the switcher's; and the grid opens by its default sort column only when the address named no sort. Gate: the sort chosen from the menu reproduced from a link.
10. **Accessibility, touch, direction, efficiency, tests and showcase.** Seven steps in this order, each a commit of its own:
    1. **A stable sort identity.** ✅ A column's sort goes by a sort name apart from its title. Gate: a sorted link opened in another culture opens with the same sort in the same direction.
    2. **The Showcase scenarios.** ✅ Every state the design names is shown on a page. Gate: a state-by-state list against the design, each state shown on a page:

       | State | Page and section |
       | --- | --- |
       | Nothing to show, in the library's sentence and in the caller's content, over a grid, a list and a tree | The data view's States tab, Nothing to Show |
       | Loading before a source's first answer, and busy over the items while it answers again | States, A Slow Source; the data grid's Virtualized Over a Provider |
       | The page giving way when its items go, and the paginator hiding once everything fits | States, The Page Giving Way |
       | A selection refused at its maximum and at its minimum, reported to the page | States, A Bounded Selection |
       | The captions in place of the master box | The data grid's Selection Limits and Captions |
       | The four select-all modes | The data grid's Select-All Modes |
       | Item selection over rows, list items and cards | The data grid's Row Selection; the data view's List and Cards tabs |
       | Grouped rows, list items and cards | The data grid's Grouped Rows; the List and Cards tabs |
       | Frozen columns, fitting a width, the headerless dense picker and inline editing | The data grid's Frozen Columns, Fitting a Width, A Picker and Inline Editing Inside a Form |
       | Virtualized over a set and over a provider, opening on an item, and items arriving at the top | The data grid's Virtualized, Virtualized Over a Provider, Opening on a Row and Rows Arriving at the Top |
       | The densities of the list and the cards | The List tab's A Dense List; the Cards tab's Thumbnails |
       | The hierarchy, and a node busy while its children load | The Tree tab; States, A Tree Asking for Its Children |
       | The view switch and the sort menu | The data view's main tab, One Set, Three Views |

       The fixtures found two faults, both fixed: a box pressed past a selection bound toggled its own look while the selection stayed as it was, and a new source collapsed a tree's opened nodes.
    3. **Accessibility, touch and direction.** ✅, with its screen reader check open. A browser pass over every view in English, Hebrew and Spanish, under a fine pointer and a coarse one, fixing what it found, in the data view and in the library's own components. Gate: no target under its floor, every view's names, roles and states exposed, and the count and busy transitions announced. The last part is open: the slow source's first load, a move to its second page and a refresh each saying loading and then the count, the tree's first load the same, taking away the last page's only item and putting everything back each saying the new count, and a fourth box pressed in the bounded selection saying the refusal, in English and in Hebrew, become the data view's scenarios in [the screen reader checks plan](screen-reader-checks-implementation-plan.md), which runs them under NVDA.
    4. **Virtualization.** ✅ Each of the tree, the list and the cards was measured without virtualization over a held set with checked selection, timing the first render and a box pressed until its item painted selected:

       | Items | List: first render, press | Cards: first render, press | Tree: first render, press | Grid: first render, press |
       | --- | --- | --- | --- | --- |
       | 500 | 243 ms, 46 ms | 244 ms, 35 ms | 293 ms, 55 ms | 152 ms, 16 ms |
       | 1,000 | 388 ms, 66 ms | 328 ms, 71 ms | 453 ms, 81 ms | 216 ms, 28 ms |
       | 2,000 | 824 ms, 135 ms | 621 ms, 140 ms | 797 ms, 160 ms | 267 ms, 52 ms |
       | 5,000 | 2,183 ms, 446 ms | 1,932 ms, 381 ms | 1,978 ms, 433 ms | 568 ms, 132 ms |
       | 10,000 | 6,450 ms, 911 ms | 5,067 ms, 723 ms | 4,278 ms, 836 ms | 1,203 ms, 291 ms |

       The list, the cards and the tree cross the web's 200-millisecond threshold for an interaction between two and five thousand items, and the grid between five and ten thousand, so every view virtualizes, with grouping under it. Landed: the list one item to a row, the cards in rows of as many as the pack lays out, the tree over its visible nodes, grouping in every view, and Home and End reaching a virtualizing view's ends. The views then move onto `FormaVirtualize`, so the item holding the focus stays rendered; [the virtualization plan](virtualization-implementation-plan.md) carries those steps. Gate: every view virtualizing at the engine's scale of a hundred thousand items.
    5. **Tests that wait on the clock.** ✅ The data view's component tests wait on the clock 237 times, 229 for a state to render and 8 for an assertion to hold. Under load, seven runs of the project in forty failed on waits standing in for a step the test controlled, and what those runs named was fixed, a fault in the tree's own rendering among it; the rest were sorted here, each wait recording whether its state already held when it was first reached, across thirty runs under load. Seventeen needed time, every one for the same reason: a load that replaced another while that load's wait to announce itself as loading was still running, a new source, a new grouping, a sort read from the address or a page pressed, asked its source only after cancelling the last load had run that wait's callback on the thread pool, which no test can hold. Departure: rather than give the component a contract a test could hold, the wait no longer listens to the load's cancellation, since it already checks on ending that its load is still the one answering, so a new source is asked in the render that gives it, which a component test holds. With that, every wait found its state already reached in every run, and all of them are kept. Gate: each wait kept found its state reached before it was checked, under load; and the project, repeated sixty times while two other test projects load the machine, failed no run.
    6. **Efficiency.** Open, and taken once the views have moved onto `FormaVirtualize` in [the virtualization plan](virtualization-implementation-plan.md), since that move changes how each view draws its items. A browser profile of the many-rows and virtualized scenarios, measuring the first render apart from the interactions after it, a box pressed, an arrow moved, select-all and a sort, recorded as a table the way step 4 records its own; then each view's item drawn by a component of its own that renders again only when its own state changes, selected, focused, editing or the item itself; then the profile again, its table beside the first. Gate: each kind of observer attached once per data view and never per item, and no interop call per item, each held by a component test; and a selection change re-rendering only the item views it affects and the selection controls that depend on it.
    7. **A move the reader began and did not make.** Open. The selection behavior takes the focus back onto an item a render drew afresh, unless the reader moved it: a press anywhere or a Tab marks the move, and the mark lasts until the focus next lands in the list. A press or a Tab that a host or a component of its own handles while keeping the focus where it was leaves the mark standing, so a render that draws the focused item afresh in that window drops the focus to the page and nothing takes it back. The mark is kept from the input on, since a focus that leaves while the input is still being handled must not be taken back, and reconciled once the input has ended: a focus still on the element it held when the input began clears the mark. Where the input ends differs by kind. A Tab moves the focus within its own keydown, a mouse press at the press itself, and a touch tap only at the compatibility press the browser raises after the finger lifts, so a press is reconciled once it has ended, at its click, at its release where it raises no click, as a secondary button's does, or at its cancellation, never at the press. Gate: script tests in which a Tab, a press and a secondary press are each handled without moving the focus and a render then draws the item afresh, the focus going back onto it, beside the tests that Tab out of the page and a press elsewhere leave the focus where they sent it; and in a browser, a tap on a non-focusable part of the page under touch emulation leaves the focus where the tap sent it.
11. **The explorer.** Open. The explorer's content area becomes a data view with its item template and its three views, its view modes keeping their names and its own views, group headings and selection retired, and its folder-tree sidebar the same data view's tree over the folders. Gate: the explorer's tests and its browser checks pass unchanged in behavior.
12. **The gallery.** Open. The gallery's browsing becomes a data view in the cards view at its compact density, grouped by date, landing with the gallery itself whenever the roadmap reaches it.
13. **The tree's return point.** Open. A plain tree's tab stop follows the node last focused in it, falling back to the current node and then the first, while a sidebar keeps returning to the current node, as the design's tree section states; a collapse or a removal while the focus is outside the tree moves only the return point. It is a step of its own, apart from the tree's move onto the virtualizer. Gate, in a browser: Tab out of a plain tree and back lands on the node last focused, a first entry lands on the current node or the first, a sidebar returns to the current node, a node removed or collapsed while the focus is elsewhere leaves the page's focus where it was, and none of it changes the selection.

## Open decisions

- **A date picker over a date that cannot be empty.** The picker's value is a nullable date, and the framework identifies a field only through a member of the input's own type, so a date member that cannot be empty is identified through a stand-in and the picker draws no validation state for it. The way through is the library's to decide: a picker bound to a date that cannot be empty, or one generic over both.
- **Moving among the items under checked selection alone.** With boxes and no item selection, the selection behavior routes no moves, so each box is a Tab stop and a virtualizing view gives the keys no way past the boxes drawn. Whether the boxes take the arrows, Home and End, as the markers do under item selection, is a design decision of its own.
- **Inputs the library lacks.** A number, a date with a time and a time are edited through the framework's inputs in the text box's look. The roadmap's number input replaces the first when it lands; the other two wait for a decision on whether the library wants them.
- **Asking again for an equal query.** A new queryable is a new source, asked again, so a query to a store built in a page's markup asks the store on every render of the page. Skipping the question for a query equal to the last would take comparing expression trees with their captured values evaluated, as a store's query cache keys them, and a comparison that missed a real change would show stale items; nothing the data view serves needs it yet.
