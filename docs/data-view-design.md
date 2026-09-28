# Forma Data View Design

## Scope

`FormaDataView<TItem>`, the engine that browses a set of items, and the views that present
what it browses: `FormaDataGrid`, rows with aligned columns and headers; `FormaDataList`,
items arranged down the height; `FormaDataCards`, items with an image, a title and what
else describes them; and `FormaDataTree`, items as nodes under their parents, opened a
node at a time. The engine owns everything a view does not draw: the source, the
selection, the sort, the page, the address, virtualization, grouping and the states. A view
owns its presentation and the settings only that presentation has, so the grid carries
columns and nothing else does.

The data view is a component big enough to be its own project, as the explorer and the
composer are: the engine and its four views ship from `Forma.Blazor.DataView`, one package
beside the product, and borrow the product's namespace as those do, so their types live in
`Forma.Blazor.Components.DataView`. The records, options and contracts they share live
there too, since it is the feature they belong to.

## Vocabulary

One word describes one presentation throughout the library:

- A **grid** is rows with aligned columns and headers, `FormaDataGrid`.
- A **list** is items arranged down the height, `FormaDataList`.
- **Cards** are items with an image, a title and other information, `FormaDataCards`.
  At the cards view's compact density a card is the image and its name alone, which is
  what a consumer that shows thumbnails composes; it is a setting of the cards view and
  not a fourth view.
- A **tree** is items as nodes under their parents, `FormaDataTree`, opened a node at a
  time.
- A **thumbnail** is an item's depiction, the picture a card or a list row draws, never a
  presentation.
- A **view** is one of those presentations over a data view, and the **active view** is
  the one showing.

These are the library's names for its components. A consumer names its own modes for its
readers, so the explorer keeps calling its cards view thumbnails and its grid view
details; the vocabulary binds what a component is, not what a surface calls the mode it
composes it in.

## Purpose

Every surface that browses records is the same thing underneath. An admin list page is a
table of records, a page at a time, with a checkbox to act on several rows and a sort by a
column. The explorer browses catalog items three ways, as tiles, as names and as a table.
The gallery browses media as date-grouped thumbnails. Each needs a source that is a set or
a provider, a selection remembered across pages and bounded by a limit, a sort, a page, a
place in the address, virtualization when the set is large, and something to say when it
is empty or still loading. Built three times, those drift three ways; the data view is
that engine built once, with the presentation left to a view that draws items and nothing
else.

It is the library's own engine rather than a wrapper over the framework's grid. A wrapper
can style what it wraps and add columns beside it, but it cannot change how items are laid
out, which are on screen, how a column gives way when the width runs out, what a
select-all names, or present the same set as cards; the framework's grid owned all of
those, and the library's rules for tokens, direction and fit stopped at its edge.

## Coi parity and what changes

Coi is the floor, not the specification. Its list pages say what the grid must be able to
do, because every one of them is a table its administrators used daily; they do not say
how, and they are not a visual target.

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

### Full parity, nothing dropped

Every cell kind, every row state, the checkbox column with its page-wide master control,
the row click-through, the sort, the page kept in the address, the empty state, the busy
state, the headerless dense picker, the image library's tiles and the toolbar's bulk
actions are in scope. The cell kinds are the caller's templates over the library's
components, the status badge, the icon buttons and the thumbnail; the row tints are the
caller's row class; the bulk actions are the selection toolbar the page composes. Nothing
Coi's administrators could do from a list is lost.

Two behaviors are not reproduced as authored. The hover-revealed editor button and the
tooltip-only action labels leave a keyboard or touch reader with no way to reach or name
an action; a row action is a labeled icon button, present whether or not a pointer hovers.
And the toolbar that crossfades between search and bulk actions is not the grid's; the
page composes the selection toolbar over it, which is the same swap done once for every
list.

### Modernisations

- **The direction of a sort is shown**, and the sorted column is marked, through the glyph
  and `aria-sort`.
- **The header stays put** while the rows scroll.
- **Columns give way by priority** before the table scrolls sideways, and a dropped column
  stays reachable, where Coi could only scroll.
- **Numbers, dates and amounts align to the end** so digits line up, where Coi centered
  them; a caller can center a column.
- **The pager is the library's**, with the page controls a reader expects and the ones a
  caller turns off.
- **Every action has an accessible name**, every control meets the target floor, and the
  keyboard reaches everything a pointer reaches.
- **The view says when it has nothing and when it is waiting**, in its own body, rather
  than leaving a bare header under a spinner elsewhere.
- **One set, three presentations.** A page that shows records as a table can show them as
  cards with a view switch, where Coi had one layout per page.
- **Logical properties throughout**, so the same stylesheet serves both directions rather
  than one mirrored by hand.

## Parity with the framework's grid

The grid view over the engine replaces the framework's grid, so everything a caller could
do with that grid they can do with these two, under the library's names. What follows is
that inventory, feature by feature, so nothing is dropped by omission; the engine's
members are the data view's, the column's are the grid's.

| The framework's grid | The data view and its grid |
|---|---|
| `Items` and `ItemsProvider`, one or the other | The same two ways in, one supplied |
| `IAsyncQueryExecutor`, so a queryable backed by a store is counted and read asynchronously | `IDataViewQueryExecutor`, a service the host registers for its store, with an in-memory default the library carries |
| `GridItemsProviderRequest`: start, count, sort column, direction, cancellation, `ApplySorting` and `GetSortByProperties` | `DataViewRequest` with the same members, so a provider applies the sort to its own queryable or reads the sorted properties for a query it writes itself |
| `GridItemsProviderResult`: items and total | `DataViewPage`: items and an optional total |
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
| `Sortable`, `SortBy`, `InitialSortDirection`, `IsDefaultSortColumn` | The same four |
| `GridSort` with `ByAscending`, `ByDescending`, `ThenAscending`, `ThenDescending` | `DataViewSort` with the same compound keys, which every view sorts by |
| `HeaderTemplate` | `HeaderTemplate` |
| `ColumnOptions`, an options surface a header button opens | `ColumnOptions`, opened from the header as the library's popover |
| `PlaceholderTemplate` for a virtualized cell not yet loaded | `PlaceholderTemplate` |
| `PropertyColumn` with `Property`, `Format`, `SortBy` | `FormaPropertyColumn` with the same, plus its editor |
| `TemplateColumn` with `ChildContent`, `SortBy` | `FormaTemplateColumn` with the same |

What the framework's grid could not do and this design adds is the rest of it: the views
beside the grid, selection, editing, priority, frozen columns, grouping, the empty and busy
states, and the fit.

## Decision summary

- **One engine, several views.** `FormaDataView` browses; a view presents. The engine
  renders no item itself and knows nothing of columns, tiles, rows or nodes; it hands each
  view the items on screen and the state around them, and takes back what the reader did.
- **A hierarchy is the engine's.** A source with parentage, a set with a `ParentKey` or
  a provider asked for a node's children, gives the engine a hierarchy: a **current node**
  whose children the grid, the list and the cards show, and the whole of it for the tree,
  loaded a node at a time as nodes open. The explorer's folder tree and its content area
  are that one source seen two ways.
- **A view reads a view contract and acts through an interaction contract.** The engine
  implements `IDataView<TItem>` and `IDataViewInteraction<TItem>`, the pair the library
  gives every subject and its parts, and cascades them; a view declared inside the engine
  registers on initialization and reads the pair. A host's own view is any component that
  does the same, so a fourth presentation needs nothing from the library.
- **A view's settings sit on the view.** Columns are the grid's, the item template is the
  cards' and the list's, density is each view's own. The engine takes only what every
  presentation shares, so it never grows a parameter for one of them.
- **The active view is state the engine keeps**, bindable by a page and written to the
  address beside the sort and the page, so a reader who switched to cards comes back to
  cards. A data view with one view has nothing to switch; one with several pairs with the
  view button the page composes.
- **Everything the framework's grid did, the grid does**, under the library's names,
  which the inventory above checks feature by feature.
- **Two ways in, exactly one supplied.** `Items` hands over a queryable set the engine
  sorts, counts and pages itself, through a query executor the host can replace for its
  store. `ItemsProvider` answers one request at a time and returns the items with the
  total where the store can count; the engine then holds only the window it shows. The
  split is the explorer's, and its request and page records take the same shape as the
  catalog's.
- **Sort, page and view live in the address** under a prefix the caller names, so two data
  views on one page keep apart and a link reproduces what a reader saw.
- **Selection is the engine's and keyed.** Checked selection and pointer selection are
  the engine's state, remembered by `ItemKey` across pages, sorts and views, bounded by a
  minimum and a maximum, and reported as a list of items. A view draws the state and hands
  gestures back; it never keeps a selection of its own.
- **How the master control reaches the whole set is the page's choice**, `SelectAllMode`:
  a menu of scopes, the page with the whole set offered in the footer, the whole set at one
  press, or nothing because the page selects through the engine's methods. The menu is the
  default.
- **Grouping is the engine's.** A group key folds the items under headings a view draws,
  a folded group staying folded across views and opening on a new grouping, as the
  explorer already does.
- **The engine draws the states and the footer**; a view draws items. Empty, busy and
  the loading placeholder belong to the engine, as do the paginator and the status region.
- **Virtualization is the framework's list virtualization**, driven by the engine and
  measured by the view, since a row and a row of cards are different heights.
- **The grid fits its width before it scrolls**, by column priority, with what left
  reachable through a columns menu, and frozen columns held through the freeze pane. The
  cards view packs before it stacks, keeping several cards across a narrow column.
- **Item actions are the caller's.** An action column, a card's action bar and a list
  row's actions are templates over the library's icon buttons.
- **The data view is not a page.** A search box, filters, a bulk-action bar, a view switch
  and a "new" button stand above it and belong to the page, composed from the toolbar, the
  selection toolbar, the sort menu and the view button.
- **The explorer and the gallery are consumers.** The explorer's content area and its
  folder-tree sidebar become one data view with its item template, its three flat
  presentations and the tree, and the gallery browses
  media through a data view in the cards view at its compact density, grouped by date,
  keeping for itself what is about media rather than browsing.

## The engine

### The source

`Items` is an `IQueryable<TItem>`. The engine applies the sort, counts, takes the page and
enumerates through `IDataViewQueryExecutor`, a service that says whether it handles a
queryable and counts and reads it asynchronously; the library registers an in-memory one
that handles every queryable, and a host registers its store's ahead of it so a query
backed by a database runs there without blocking a circuit.

`ItemsProvider` is a delegate the engine calls with a **request**, the start index, the
count, the sort and a cancellation token, which can apply its sort to a queryable the
provider holds or hand over the sorted properties for a query the provider writes itself,
and that returns a **page**: the items and the total, or no total where the store cannot
count cheaply, in which case the engine drops its pager and pages by the items it
received. A request superseded by a newer one is canceled rather than ignored, and the
newest answer is the one shown. `RefreshDataAsync` asks the source again for the window
being shown, which is how a host reports that the records changed; a page that no longer
exists, because the last items on it were deleted, gives way to the last page that does.

While a provider is answering the engine is busy: the items it had stay in place under a
busy state, and a data view that has never had items shows its loading content instead. A
source that answers with no items shows the empty content, the caller's fragment or the
library's default sentence.

### Selection

Checked selection, `EnableCheckedSelection`, gives every item a box the view draws, and a
master control the view places, in the grid's header cell or above the cards. The master
control is a tri-state box: unchecked with nothing selected on the page, indeterminate with
some, checked with all. What pressing it reaches is `SelectAllMode` in the checked
selection options:

- **`FromMenu`**, the default: the box selects or clears the page, and a menu button beside
  it opens the scopes, the page with its count, the whole set with its count where the
  engine holds the set, and clearing the selection.
- **`FromPager`**: the box selects or clears the page, and once the page is fully selected
  and the set holds more, the footer's status region offers selecting the whole set, named
  by its count.
- **`Direct`**: the box selects or clears the whole set at one press where the engine holds
  the set, and the page where it does not.
- **`None`**: the master control is not drawn, and the page selects through the engine's
  selection methods and its own controls.

A provider data view cannot name its whole set, so every mode reaches the page alone
there, and the menu and the footer offer no more than that.

Pointer selection, `EnableItemSelection`, is explorer-style selection over the items
themselves: a plain click replaces the selection with the item, Ctrl or Cmd toggles it,
Shift selects the range from the anchor, and Alt does nothing; the arrow keys, Home and End
move focus among the items, Shift extending the range and Ctrl or Cmd moving focus alone.
The view says what the arrows mean, up and down alone among rows, all four among cards.
The keys are routes the selection behavior declares, so the host's input policy reaches
the router before them. Both kinds combine.

The selection is a list of items reported through `SelectedItems` and
`SelectedItemsChanged`, remembered by key, and an item not on the page stays selected until
it is cleared. `CheckedSelectionOptions.Maximum` blocks further selection once reached,
muting the remaining items and reporting the blocked attempt through
`OnSelectionRejected`, while a full clear is always allowed; `Minimum` blocks unchecking an
item below the floor, but not a plain-click or range replacement or the master clear, which
reset the selection rather than remove from it. The options' caption swaps the master box
for a static label or an interactive toggle. The selection pairs with the selection
toolbar above the data view, which the page composes.

### Sorting, paging and the address

The engine holds one sort, `DataViewSort`, a first key and any number of further keys, each
ascending or descending, applied to a set or handed to a provider. A view offers it as the
presentation allows: the grid through its column headers, the list and the cards through
the sort menu the page composes over `SortByAsync`. A data view opens sorted by its default
sort, which the grid takes from its default sort column.

With paging on, the engine shows `PageSize` items and the footer's paginator, which stays
hidden while everything fits on one page; the page is bindable through `Page` and
`PageChanged`, and the total the source reported is read through `TotalItemCount` and
announced through `TotalItemCountChanged`, so a page can show a capacity beside the view.
A page change scrolls the items back to their top. Turning paging off shows every item in
one scroll region and returns to the first page, so a page number left behind means
nothing later.

Where the caller gives `AddressOptions` a prefix, the sort, the page and the active view
are written to the address as `{prefix}-sort`, `{prefix}-order`, `{prefix}-page` and
`{prefix}-view`, read back on navigation, and dropped from the address when they return to
their defaults; the names are the options' to change. A data view with no prefix keeps its
state in memory alone. Two data views on one page take two prefixes.

### Hierarchy

A source has parentage when a set names a `ParentKey` beside its `ItemKey`, or when a
provider takes the node whose children it is asked for in the request. `HasChildren` says
whether an item can open; absent, a node opens until its children come back empty. The engine then
holds a current node, `CurrentItem` with `CurrentItemChanged`, null for the roots, and the
flat views show its children, sorted, paged and grouped as any set is; activating a node
that has children in a flat view makes it current, which is how the explorer opens a
folder. The tree shows the hierarchy from the roots, asking the source for a node's
children the first time it opens and keeping them, with a busy state on the node while
they load. The sort applies within a level. A source without parentage is one level,
and the tree has nothing to show for it.

### Grouping

`GroupBy` names a key the items fold under: a group is a heading the view draws over its
members, folded or open, and a folded group stays folded when the reader switches views
and opens again on a new grouping. The engine groups a set itself and asks a provider for
the sorted window a grouping needs. A group heading carries the group's name and count and
is a target, so a reader folds it from the keyboard.

### Virtualization and the scroll region

The items live in a scroll region the engine owns, with what a view pins, the grid's
header, stuck to its top and the footer outside it. The data view caps itself at the height
it is given: against an auto-height ancestor the cap resolves to none and the view takes
the height of its items, and inside a bounded shell the items scroll while the pinned
parts and the footer stay put. A paged data view holds its region at the height of a full
page, so a short last page does not shrink it.

`Virtualize` is for the bounded case with paging off: the engine renders only the items
near the viewport through the framework's list virtualization, in units the view names,
a row for the grid and the list, a row of cards for the cards, with `ItemHeight` the
height of that unit and `OverscanCount` the units drawn beyond each edge. An item whose
window has not loaded draws the view's placeholder. `AnchorMode` says where the viewport
holds when items arrive at an edge, `ItemComparer` tells a prepend from an append so the
items on screen stay put, `InitialItemIndex` opens the data view on an item, and
`ScrollToItemAsync` scrolls to one later. The region announces its real item count, so
assistive technology knows how many items exist rather than how many are drawn.

### The footer and the states

The footer holds the paginator at the inline start and the status region at the inline
end, wrapping when narrow. It is skipped when there is nothing to put in it, and the
paginator alone hides while everything fits on one page. The status region is a live
region, so a count written into it is announced. The empty state and the loading state
fill the region the items would, the empty one showing the caller's `EmptyContent` or the
library's sentence, the loading one the library's busy indicator, and the region is
`aria-busy` while a provider answers.

## The views

A view is declared inside the data view's content and registers with the engine on
initialization; the engine cascades its item type, so a view names no type argument of
its own. A data view with one view shows it; one with several shows the active view,
`View` with `ViewChanged`, naming the view by the key each declares, `grid`, `list`,
`cards`, `tree`, or the host's own. A view reads `IDataView<TItem>`: the items on screen with
their keys, the selection with what each item may do, the sort, the groups, the busy and
empty states, and the options the engine holds. It acts through
`IDataViewInteraction<TItem>`: select, toggle, range, clear, sort, fold a group, activate
an item, and report a measurement virtualization needs. A view draws the item's selected,
disabled and focused states with a channel beside color each, and gives every target the
floor.

### The grid

`FormaDataGrid` draws the items as rows with aligned columns and headers. Its settings are
its own: the columns, `ShowHeader`, `Dense`, and inline editing.

**Columns.** A column is declared inside the grid's `Columns` fragment and registers with
the grid on initialization, in the order written, which is the order rendered. Every
column has a **title**, shown in the header and used as the row's accessible column name;
an **alignment**, start, center or end, logical in both directions, with numbers aligned
to the end by default in a property column whose type is numeric; a **priority**, an
integer where a lower number leaves first when the width runs out and the highest priority
never leaves; a **frozen** flag; and a **sort**, either none or a `DataViewSort` the
engine applies. A column declares its **initial sort direction**, and one column may be
the **default sort column** the grid opens sorted by. A column may carry a **header
template** in place of its title, a **column options** fragment the grid opens from a
button in the header cell as the library's popover, a **placeholder template** drawn in a
virtualized cell whose row has not loaded yet, a **class** for its cells, a **width** on
the scale, a `CssLength`, where the content does not size it well, and a caller hides one
outright with `Visible="false"`, a decision of the page rather than of the width that takes
the column out of the columns menu too.

`FormaPropertyColumn<TItem, TValue>` binds a member. Its title comes from the member's
display attributes or its name, its sort from the member, its text from the value through
a format string against the current culture, and while its row is edited it swaps to an
editor chosen from the value's type, or the caller's edit template. `Editable="false"`
opts it out, and a member with no setter is never editable. `FormaTemplateColumn<TItem>`
renders the caller's fragment for the row and, where given, its own header content, with
no sort unless the caller supplies one, since the grid cannot see through a template. Both
derive from `FormaColumnBase<TItem>`, public so a caller derives a column of their own by
overriding its cell and header content.

The grid adds one column of its own, at the start, whenever a kind of selection is on: the
checkbox column with the master control in its header, or an invisible marker column
that carries the row's focus and selection semantics for pointer selection without
checkboxes.

**Sorting by header.** A sortable column's header is a button. Pressing it cycles
ascending, descending, then off, announced through `aria-sort` and drawn with a glyph
beside the title; one column sorts at a time, and `SortByColumnAsync` sorts by a column on
request, as a header press does.

**Fitting a width.** The grid measures its own inline size and the widths its columns
want, and decides which columns fit in priority order. A column that does not fit is
dropped from the row, its header and cells left unrendered, and listed in the **columns
menu**, a menu in the header that shows every droppable column with a mark against those
shown; choosing a dropped column shows it and lets the pane scroll sideways for it, and
choosing a shown one drops it. A choice a reader makes wins over the priority until the
grid is given a different width, and a column of the highest priority is never dropped and
never listed. Frozen columns are written first and stay at the start of the pane as it
scrolls, through the freeze pane the grid composes; the pane's arrows and its draggable
frozen edge come with it. The grid never sizes against the viewport; a container query
decides its layout, and the only media queries are for the reader.

**Header and rows.** The header row sticks to the top of the scroll region and paints over
the rows in the header tone, its cells on the same gutters as the cells beneath them; the
columns menu, shown only when a column can be dropped, stands at the header's inline end.
`ShowHeader="false"` drops the header for a consumer that names its columns elsewhere. A
row stands on the medium target step, so its actions and inputs sit level with the
buttons beside the grid; `Dense` puts it on the small step for a list whose rows are plain
text. Rows are told apart by a hairline in the border token, and under a hovering pointer a
row takes the hover tone inside the hover media query alone. A selected row takes the
selected tint and its box or marker carries `aria-selected`; a disabled row takes the
disabled text tone and its box is `aria-disabled`; a row that carries focus draws the
focus ring on itself. The caller's `ItemClass` is layered under the grid's own classes.
The table is a `<table>` with header cells scoped to their columns, so a screen reader
reads a cell with its column name.

**Inline editing.** `EnableInlineEditing` with `CloneForEdit` lets one row at a time be
edited. The grid copies the row, cascades the edit session its columns read, validates
the copy over an edit context of its own with the data annotations validator, and raises
`OnRowSave` only once the copy is valid; `CancelEditAsync` discards it and raises
`OnRowCancel` with the untouched row, `SaveEditAsync` commits and reports whether it was
accepted, and `BeginEdit`, `IsEditing` and `EditingItem` complete the surface, driven from
a template column through a reference to the grid. The item is a reference type, since
the editors write through to the copy. The row being edited leaves every selection gesture
for the duration. The editors are the library's inputs, chosen by the value's type: the
text box for text, the checkbox for a Boolean, the date picker for a date, the number
input for a number; anything else needs an edit template.

### The list

`FormaDataList` draws the items down the height, each row the item's thumbnail where the
caller templates one, its name, and the secondary text the caller's `ItemTemplate` adds,
flowing on into
further columns when the height runs out and the width allows, as the explorer's list
does. It has no header; its sort comes from the sort menu the page composes. Its rows
stand on the small target step, with `Dense` tightening them further for names alone. A
selected row takes the selected tint and its check mark; the arrow keys move up and down
the flow, and Home and End reach its ends.

### The cards

`FormaDataCards` draws each item as a card: its thumbnail, its name and the information the
caller's `ItemTemplate` places beneath, with the item's actions in a bar the caller
templates and a check mark while selecting. Cards pack into as many across as the width
allows at the card size the view is given, `CardSize`, packing before they stack, so a
narrow column still holds several. At the compact density, `Dense`, a card is the
thumbnail and its name alone, which is what a consumer shows as thumbnails, and the
thumbnail is the target. Group headings span the row of cards. The arrow keys move in all
four directions across the packed cards,
Home and End reach a row's ends, and a selected card takes the selected tint with its
check mark.

### The tree

`FormaDataTree` draws the hierarchy as nodes under their parents, each row the node's
thumbnail where the caller templates one and its name, indented by its depth, with a disclosure that
opens and closes it and the caller's `ItemTemplate` for what else a node shows. Opening a
node asks the engine for its children, and a node that is loading shows the busy state
in place of its disclosure. The keyboard follows the tree pattern: the up and down arrows
move among the visible nodes, the inline-end arrow opens a closed node or moves to its
first child, the inline-start arrow closes an open node or moves to its parent, Home and
End reach the first and last visible node, and typing a letter jumps to the next node
starting with it. A selected node takes the selected tint and `aria-selected`; the
selection is the engine's, so a node selected in the tree is the same item selected in
the grid. Choosing a node can also make it the current node, `SelectsCurrent`, which is
what a sidebar does, and `NodeFilter` narrows the tree to the items it admits, the
folders alone for a sidebar over a source that holds files too. The tree announces itself
as a tree with each node's level, its
open state and whether it has children, and virtualizes over the visible nodes with a
row as its unit.

### A host's own view

A component that registers with the engine and reads the pair is a view, so a host
presents a set any way it needs, a calendar, a map, a kanban board, without the library
shipping a mode for it. It names itself with a key the view switch and the address use,
draws the items the engine hands it, reports its selection gestures through the
interaction, and tells the engine how tall its virtualization unit is. Everything the
engine does, the source, the selection, the address, the states and the footer, comes with
it.

## The public surface

The data view is read and driven by a host, and extended by one, so its surface is written
to be read: a parameter names the thing it sets, a method starts with the verb it performs,
and what belongs together travels together.

The engine's parameters fall into a few groups. The source: `Items` or `ItemsProvider`,
`ItemKey`, `ItemComparer`. The presentation: the views as content, `View` with
`ViewChanged`, `ItemName`, `ParentKey`, `HasChildren`, `CurrentItem` with
`CurrentItemChanged`, `GroupBy`, `ItemClass`, `EmptyContent`, `LoadingContent`, `Status`. Paging
and the address: `EnablePagination`, `PageSize`, `Page` with `PageChanged`,
`PaginationOptions`, `AddressOptions`. Virtualization: `Virtualize`, `ItemHeight`,
`OverscanCount`, `AnchorMode`, `InitialItemIndex`. Selection: `EnableCheckedSelection`,
`EnableItemSelection`, `SelectedItems` with `SelectedItemsChanged`,
`CheckedSelectionOptions` carrying the bounds, the caption and `SelectAllMode`, and
`OnSelectionRejected`. Items: `OnItemClick`. A setting a page normally passes through
unchanged sits in an options record; one it binds or derives from itself is a parameter.
The grid's own are `Columns`, `ShowHeader`, `Dense`, `EnableInlineEditing`, `CloneForEdit`,
`OnRowSave` and `OnRowCancel`; the list's, the cards' and the tree's are
`ThumbnailTemplate`, `ItemTemplate` and
`Dense`, with the cards' `CardSize` and the tree's `SelectsCurrent` and `NodeFilter`.

The engine's methods are the verbs a page needs: `RefreshDataAsync`, `SortByAsync`,
`ScrollToItemAsync`, `SelectAllAsync`, `SelectPageAsync`, `ClearSelectionAsync`,
`ClearPageAsync`, `SelectAsync` and `DeselectAsync` for given items, `FoldGroupAsync` and
`UnfoldGroupAsync`, `OpenNodeAsync` and `CloseNodeAsync`, with `TotalItemCount` read
beside them. The grid adds
`SortByColumnAsync`, `ShowColumnOptionsAsync`, `HideColumnOptionsAsync`, `BeginEdit`,
`SaveEditAsync` and `CancelEditAsync`, with `EditingItem` and `IsEditing`. The selection
methods are what `SelectAllMode.None` hands a page: the same operations the menu performs,
from whatever control the page draws.

The data view is extended in three ways. A view of the host's own registers with the
engine and reads the contract pair, and a column of the host's own derives from
`FormaColumnBase`. A template replaces one part of what a view draws: an item's template,
a column's header, cell, placeholder and edit template, a column's options surface, a
card's action bar, the empty and loading content, and the status region. And the options
records grow without a new parameter each time a concern gains a setting.

## Efficiency

A browsing surface is the component a reader waits on most, so what it costs is part of
its design, and every phase is held to these rules.

- **Only what is on screen is rendered.** A page renders its page, a virtualized view its
  window, and a tree its open nodes; nothing renders an item the reader cannot see.
- **The source does the work.** Sorting, counting and slicing a queryable run in the
  store through the executor, never after materializing the set, and a provider is asked
  for one window, once, with a superseded request canceled rather than awaited.
- **A render costs what changed.** Every item is keyed, so a re-render diffs rows rather
  than rebuilding them; a selection change touches the rows whose state changed; the
  selection is looked up by key in a set, never by scanning a list; and a column's fit is
  computed when the width changes, not on every render.
- **Measurement is observed, not polled.** One resize observer per data view reports the
  width the fit reads and the heights virtualization needs, and the interop it takes is
  attached once and released on disposal, never a call per row.
- **Script is for what markup cannot do.** Sticky headers, packing, hover and direction
  are stylesheet work; script is for measurement, the selection gestures the keyboard and
  pointer need, and nothing else.
- **Nothing allocates per keystroke or per pointer move.** A gesture resolves to an item
  through the item token table and reports once when it settles.

## Accessibility, keyboard and direction

Every view announces what it is: the grid a table with scoped header cells, its sort
through `aria-sort` and its busy state; the list and the cards a list of items; the tree
a tree with each node's level and open state. Every
item's selection state is announced, every group heading is a button that says whether it
is folded, and every control meets the target floor, 24 pixels square and 44 under a
coarse pointer: the sort buttons, the boxes, the pane's arrows, a card's image at the
compact density, and whatever a template places in an item.

Sorting, selecting, paging, folding, revealing a column, switching views and opening the
columns menu are all reachable from the keyboard: Tab reaches the header buttons, the item
markers and the items' own controls, the arrows move focus among the items while pointer
selection is on, in the directions the view gives them, and the paginator's own keys page.

Every offset, alignment and border is logical, so the same stylesheet serves both
directions; a numeric cell reads as a number in either, cards pack from the inline start,
and a glyph that points mirrors through the icon component. The showcase page shows every
view in every culture the library ships.

## Non-goals

- Cell-level navigation and editing, a spreadsheet's two-dimensional grid. The grid is a
  table of records, and a row is the unit.
- Column reordering and per-column resizing by the reader. The frozen edge is draggable
  because the pane provides it; the rest of the widths are the grid's to decide.
- Sorting by several columns at once. A view sorts by one sort, whose keys may be several.
- Row detail expansion, a master-detail row. A record's detail is a page or a dialog the
  caller opens from an item action.
- Reordering items by dragging. That is the sortable list's job.
- A toolbar, a search box, filters, a view switch or a sort menu. They are the page's,
  composed from the library's components above the data view.
- What is about media rather than browsing: a lightbox, a slideshow, playback. Those stay
  with the file viewer and the gallery.
