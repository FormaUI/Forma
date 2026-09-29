# Forma Data View Implementation Plan

The plan for building the data view that [the data view design](data-view-design.md) states, and the record of what has landed. Section names refer to the design.

## Status

Phase 1 has landed: the `Forma.Blazor.DataView` project and its test project, the engine with its contract pair, the query executor, the provider records, the address, the footer, the scroll region, and the grid as its first view with the derivable column base, the property and template columns, the header with its sort buttons and options surfaces, and the rows it renders itself. The framework's grid is gone from the product, the showcase and every test project. The selection, the row tokens, the height reservation and inline editing moved over with it as they were, so every scenario the grid's showcase page shows renders as before, under the engine's name for the selection and the address.

Three things landed differently from the plan's sketch. The table is a part of the grid, `FormaDataGridTable`, declared after the columns so that it renders once they have registered, rather than a deferral helper of its own; the same part pattern gives the grid its row and its column header, and the three parts share one stylesheet scope through the project file, since the grid's rules reach markup its parts render. Basic virtualization landed with the grid rather than waiting for phase 3, because the showcase's virtualized scenario needed it to keep rendering as it did; the anchoring set, the placeholder template and the announced count remain phase 3's. And the views are the data view's content, so a data view that also sets `Status` wraps its view in `ChildContent`, as Razor requires once a second fragment is named.

The files sit flat in the project under one namespace, as the explorer's do, rather than in the per-view folders first sketched, and the showcase pages sit in the `Features` category beside the explorer's and the composer's.

Phase 2 has landed: the four select-all modes with the menu as the default, drawn by the grid's header from the engine's mode; the selection methods on the engine and the interaction contract; selection over a provider, which reaches the page in every mode and never offers the set; and the selection kept by the item key, so a page re-fetched as fresh copies still shows what was selected. The master control reaches the page in the menu and pager modes, so clearing it clears the page alone, a reset the selection floor does not bind, which the engine exposes as `ClearPageAsync` beside the methods the design names. The design's `IsSelected` and `SelectedItems` answer by key, and a change of `ItemKey` rebuilds the selection under the new key.

Phase 3 has landed: the anchoring set on the engine and handed to the framework's list virtualization, the column's placeholder template drawn in a placeholder row, every row a virtualizing grid draws carrying its place in the set, scrolling to an item, and the master control where nothing pages, which reaches the whole set the data view holds and is not drawn over a provider. A view registers with the viewport it keeps, a contract the design did not name, through which the engine scrolls and asks a virtualizing view to fetch its windows again after a reload, which a sort or a refresh over a virtualizing view needed. The anchor mode and the placeholder's context are the framework's own types, since the virtualization is the framework's.

Phase 4 has landed: a grouping the items fold under, naming the key, the order the groups come in and the name a heading shows, carried in the request so a provider sorts its window by the group before the sort; the groups read from the window as runs of one key; the headings the grid draws as row-group headers, each the library's button that folds its group and opens it again; and the fold kept on the engine, so it survives a reload and a change of view, and opens again under a new grouping. Grouping a virtualizing data view is refused, since its windows are flat rows with no place for a heading; see the open decisions.

Phase 5 has landed: a column's priority, frozen flag, visibility and declared width; the fit, measured by a column-fit behavior in the browser interop that watches the width the grid is given and the width each header cell wants, laid out at the table's own content width for one unpainted read; the columns menu at the header's inline end; the frozen region through the freeze pane the grid composes, with the selection column frozen beside the frozen columns, each placed by the widths before it, the pane's arrows stepping between the other columns and its edge resizing the last frozen column; and the dense rows and the headerless grid as before. Three things landed differently from the design's sketch. The pane is the one box the rows scroll in, both ways, since a sticky header inside a pane that scrolls sideways alone would stick to nothing; the freeze pane gained the option, and the grid wraps its pane in a shell that takes the region's height. The header row always renders, collapsed to no height when the header is off, because the widths are measured from its cells. And the fit is measured by script rather than decided by a container query, since a query cannot tell what a column wants.

Phase 6 has landed: the property column draws the library's inputs where the library has one for the member's type, the text box, the checkbox and the date picker, each named by its column since a cell carries no label; a number, a date with a time and a time keep the framework's inputs in the text box's look until the library has inputs of its own for them, which the roadmap's number input begins. The date picker binds a nullable date; a date member is bound through a conversion each way and identifies its field through a stand-in of the picker's own type, so the picker shows no validation state of its own for such a member, though the row's save still validates it; see the open decisions.

Phase 7 has landed: the list and the cards as the second and third views, each with its item and thumbnail templates and its density, the cards with their action bar and card size, both drawing the engine's groups under a shared heading part and the engine's selection through the same markers the grid draws; the engine's active view, bindable and written to the address, with a view registering its key, the name and glyph the switch shows, and its viewport in one record; the item name the engine hands a view without columns; and the view switch, a component a page composes beside the data view or inside it, one toggle button per registered view. Two things landed differently from the design's sketch. The arrows move by where the items stand rather than by directions a view declares, one behavior finding the item above, below or beside the focused one, so a table's rows, a list's columns and packed cards need no layout of their own. And the list and the cards refuse a virtualizing data view rather than showing an empty window; see the open decisions.

Phase 8 has landed: the engine's hierarchy, a parent key over a set or a provider asked for a node's children through the request, whether a node has children, and the current node whose children the views without a tree show, bindable and reached by activating a node with children in any of them; the children the tree asks for kept by node until the source or the sort changes; and the tree, drawing the roots and each opened node's children a level deeper, with its disclosure, its busy state, its keyboard pattern and type-ahead, its filter, and its choice of the current node. Several things landed differently from the design's sketch. A tree that chooses the current node registers as a companion, a view drawn beside the active view and left out of the switch, since a sidebar and the view it drives show at once; the region lays a companion out across from the view beside it. The roots have members of their own on the contracts rather than standing for a null node, so no caller passes null for an item whose type may be a value. A sidebar's choice is the current node alone, not the data view's selection as well: its rows carry no selection marker, the current node is the selected node of a single-select tree, and the rows carry the focus themselves with the current node as the tab stop, so choosing a folder in the sidebar leaves the selection of the view beside it untouched; and when a view beside it moves into a node the sidebar does not show, the sidebar opens the node's ancestors to reveal it, which the data view makes possible by remembering the parent every item was answered under. The tree's keys are routes on the input router, the inline arrows bound by the reading direction; a tree whose rows carry a marker declares the inline arrows alone and leaves the vertical keys to the selection behavior, and one whose rows carry the focus declares the vertical keys and Enter as well, with type-ahead the one key it reads itself. In a view with a selection a press is the selection's, so a double press, or Enter on the item's marker, activates the item, which is how a folder opens from a selecting grid; the selection behavior also leaves a key pressed in a control of the view outside its items, a header's button or a sidebar's row, to that control. And the tree draws every visible node rather than virtualizing over them; see the open decisions.

Phase 9 has landed: the sort menu, a drop-down a page composes beside a data view or inside it, offering the sorts it is given, each a name the address carries, a label, the keys and the direction it opens in, with the sort applied checked and the two directions turning it, so the list and the cards sort as the grid's headers do. It reaches the data view through a sorter contract of its own, as the view switch does through the switcher's, and a sort the address names opens the data view sorted when the menu offers it. The grid now opens by its default sort column only when the address asked for no sort, since a sort named in the address that no column carries may be the menu's; a name neither carries leaves the data view unsorted, where before the grid's default took over. A menu option sharing a column's sort name marks that column's header when chosen.

Phase 10's first step has landed: a column's sort goes by its sort name rather than its title, a property column's being its property's path, so a link opened in another culture keeps its sort and its direction; a property column over a computed property sorts only once its sort is named, and a column asked to sort with no name, or two sortable columns of one sort name, are refused as the grid collects its columns.

Phase 10's second step has landed: the engine's page gained a States tab for what no page showed, and every state the design names is now on a page:

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

The bounded selection showed a box the reader pressed past a bound toggling its own look while the selection stayed as it was; the item boxes now take the master box's pattern, the press deciding and the box never toggling itself, so a refused press leaves the box as it was. The fixtures also showed a new source, a queryable built afresh on a page's render, collapsing a tree: the engine now keeps the nodes the reader opened and shows their children until the new source answers for them.

## What the rewrite starts from

The wrapper carries a good deal the design keeps: the two kinds of selection with their bounds, captions and rejection reports, the item token table the selection behavior reads, the height reservation that holds a paged view at a full page, the footer with the library's paginator and the status region, inline editing with its edit session, and the property column with its editor choice, title resolution and draft-targeted validation. Those move rather than start over, the selection and the footer into the engine and the editing into the grid. The explorer carries the other half: its three views over one content area, the group headings that fold, the sort menu and the view switch, the frozen name column through the freeze pane, and the selection behavior over items. Those are the pattern the engine generalizes, and the explorer adopts the engine once it exists. What has to be built new is the engine's contract pair and view registration, the table the grid renders itself, the query executor, the provider contract, virtualization with its anchoring, the address with the active view, and the list and cards views.

The grid's tests cover selection, bounds, paging, the footer, the scroll region, the provider, refresh, item tokens and inline editing, and they stay as the contract each step has to keep passing, rewritten only where the markup they find changes. The explorer's tests do the same for its adoption. The tests that pin the framework grid's markup, which the stylesheet reached through, go with the stylesheet rules that needed them.

## File layout

The layout as built; an entry marked planned is one the design still calls for and no phase has landed yet.

```text
src/Forma.Blazor.DataView/                     # namespace Forma.Blazor.Components.DataView
├── FormaDataView.razor                        # the engine
├── FormaDataView.razor.cs
├── FormaDataView.razor.css
├── IDataView.cs                               # what a view reads
├── IDataViewInteraction.cs                    # what a view invokes
├── DataViewRequest.cs                         # what a provider is asked
├── DataViewPage.cs                            # what a provider answers
├── DataViewSort.cs                            # the sort, with its compound keys
├── DataViewItemsProvider.cs                   # the provider delegate
├── DataViewAddressOptions.cs
├── IDataViewQueryExecutor.cs                  # with the in-memory default beside it
├── CheckedSelectionOptions.cs                 # moved from the collections category
├── CheckedSelectionCaption.cs
├── SelectAllMode.cs                           # what the master control reaches
├── SelectionBoundary.cs
├── SelectionBounds.cs
├── SelectionRejected.cs
├── FormaDataGrid.razor                        # the grid view
├── FormaDataGrid.razor.cs
├── FormaDataGridTable.razor                   # the table the grid draws, a part; the three parts share one stylesheet scope
├── FormaDataGridTable.razor.css
├── FormaDataGridRow.razor                     # one row, a part
├── FormaDataGridRow.razor.css
├── FormaDataGridColumnHeader.razor            # a header cell's content, a part
├── FormaDataGridColumnHeader.razor.css
├── FormaColumnBase.cs                         # the derivable column base
├── FormaPropertyColumn.cs
├── FormaTemplateColumn.cs
├── ColumnAlign.cs
├── RowEditSession.cs
├── ItemKeyComparer.cs
├── ItemTokenTable.cs
├── FormaDataList.razor                        # planned (phase 5)
├── FormaDataCards.razor                       # planned (phase 5)
├── FormaDataTree.razor                        # planned (phase 5)
├── .locale.json                               # the root namespace, for a host laying the assembly's files out by it
└── README.md
src/Forma.Locales/Forma.Blazor.Locales.{Language}/Forma.Blazor.DataView/
└── {culture}.json                             # the engine's and the views' strings
tests/Forma.Blazor.DataView.Tests/             # mirrors the source tree
src/Forma.Blazor.Showcase/Components/Pages/Features/
├── FormaDataGridPage.razor                    # moved from the collections pages
├── FormaDataViewPage.razor                    # planned (phase 5): the engine with a view switch over every view
├── FormaDataListPage.razor                    # planned (phase 5)
├── FormaDataCardsPage.razor                   # planned (phase 5)
└── FormaDataTreePage.razor                    # planned (phase 5)
```

The project is laid out as the explorer and the composer are: a Razor class library referencing the product, the interop assemblies its behaviors need, and the Framework assemblies it compiles against, packed as `FormaUI.NET-Blazor.DataView` with the product's package as its dependency. The sortable list stays in the product's collections category; only the grid and what belongs to it move. The explorer's project and, later, the gallery's take a dependency on the data view's package as consumers of it; the data view references neither.

## Phases

Each phase ends buildable, tested and reviewable, with a showcase page showing every state the phase adds.

1. **The project, the engine and the grid.** ✅ The `Forma.Blazor.DataView` project and its test project; the engine with its contract pair and view registration, the source with its query executor, the request and page records, the provider delegate, cancellation, the busy and empty states, refresh and the page giving way; the sort and page with the bindable page and the reported total; the address with the active view; the footer; the scroll region with the height reservation; and the grid as the first view, with the derivable column base, the property and template columns, the header with its sort buttons and options surfaces, the rows the grid renders itself, sorting by header with compound keys and the default sort column, and the grid's tests moved over. The framework's grid leaves the product, the showcase and the test projects with this phase, the catalog's structure lists the project, and the grid's stylesheet reaches nothing through `::deep` that is not a library component. Gate: every scenario the grid's showcase page shows today renders as it did, and every existing test passes against the owned markup.
2. **Selection.** ✅ Checked and pointer selection as the engine's, drawn by the grid, with the four select-all modes and the menu as the default, the selection methods, selection over a provider, and the selection remembered by key across paging and sorting. Gate: the selection scenarios in each mode, a provider with checked selection, and a selection kept across a page change.
3. **Virtualization.** ✅ The framework's list virtualization over a set and over a provider in the view's units, with the placeholder, the anchoring set, the initial item, scrolling to an item and the item count announced, and what the master control reaches where nothing pages, since a virtualizing data view holds no page for the menu and pager modes to select. Gate: a hundred thousand rows scroll in a bounded box, open on a given row and hold their viewport when rows are prepended.
4. **Grouping.** ✅ Grouping with its headings, folding and the fold kept across views. Gate: a grouped grid folds and unfolds from the keyboard.
5. **Fitting a width.** ✅ Column priority, the width measurement, the columns menu, frozen columns through the freeze pane, the dense rows and the headerless grid. Gate: a narrow scenario drops its lowest columns and lists them, a frozen scenario holds its name column while scrolling, and the picker scenario shows the headerless dense grid.
6. **Inline editing.** ✅ The edit session and the property column's editors moved over, swapped for the library's inputs. Gate: the editing scenario runs inside a form.
7. **The list and the cards, with the view switch.** ✅ The two views with their item templates and densities, the cards' packing and four-direction arrows, and the view switch bound to the active view and written to the address. Gate: one set shown three ways on the engine's showcase page, switched from the view button and reproduced from a link.
8. **The hierarchy and the tree.** ✅ The engine's hierarchy with its current node and children loaded on demand, and the tree with its disclosure and keyboard pattern. Gate: a folder hierarchy opened in the tree while the grid shows the current node's children.
9. **The sort menu.** ✅ The sort menu over the engine's sort. Gate: the sort chosen from the menu reproduced from a link.
10. **Accessibility, touch, direction, efficiency, tests and showcase.** Five steps in this order, each a commit of its own:
    1. **A stable sort identity.** ✅ A column's sort goes by a name apart from its title, which a property column takes from its property's path when none is given and a sortable column with no property to take it from must be given; the title is the label shown and nothing else. The engine resolves a sort and writes the address by that name, a sort menu's option refers to a column's sort by the same name, and two sortable columns of one grid, or two options of one menu, naming one sort are refused, while a column and a menu option sharing a name are the same sort. Gate: a sorted link opened in another culture opens with the same sort in the same direction.
    2. **The Showcase scenarios.** ✅ Every state the design names shown on a page — empty, loading, busy, a rejected selection, grouped, frozen, virtualized, the hierarchy — so the passes after it have their fixtures. Gate: a state-by-state list against the design, each state shown on a page.
    3. **Accessibility, touch and direction.** A browser pass over the grid, the list, the cards, the tree, the switch and the sort menu in every culture the library ships, fixing what it finds. Names, roles and exposed states are read from the accessibility tree; that the item count and the busy state are announced as they change is checked with a screen reader, since the tree shows what is exposed rather than what is spoken. Every target is measured at the floor of the pointer it serves: the box the press lands on, its border box or the hit area around it, at least 24 by 24 CSS pixels under a fine pointer and 44 by 44 under a coarse one. Gate: no target under its floor, every view's names, roles and states exposed, and the count and busy transitions announced.
    4. **Virtualization.** The scenarios and the scale each view is expected to carry are set down first, from the explorer, the gallery and the list pages the data view replaces, and against them each view's virtualization, the tree's, the list's and the cards', and grouping under it, is recorded as required now or deferred, with the measurement behind the decision. What is required is built in a commit of its own, before the efficiency pass. Gate: the decision recorded for every view, and every view required to virtualize doing so at its expected scale.
    5. **Efficiency.** A browser profile of the many-rows and virtualized scenarios, measuring the first render apart from the interactions after it. Gate: no work nothing needs — each kind of observer the data view uses, the column fit's, the height reservation's and the virtualization's, attached once per data view and never per item; no interop call per item; and a selection change updating the item views it affects and the selection UI that depends on it, a count, a master control or a selection toolbar, while unaffected item views do not re-render.
11. **The explorer.** The explorer's content area becomes a data view with its item template and its three views, its view modes keeping their names, its own views, group headings and selection behavior retired, and its folder-tree sidebar the same data view's tree over the folders, so the explorer draws no browsing surface of its own; its tests kept passing. Gate: the explorer's tests and its browser checks pass unchanged in behavior.
12. **The gallery.** The gallery's browsing becomes a data view in the cards view at its compact density, grouped by date, and its design keeps for itself what is about media. This phase lands with the gallery itself, whenever the roadmap reaches it.

## Renames the rewrite carries

The grid's public names came from the framework's grid, and owning it is the moment to name them for the library. The explorer's own names, its view modes among them, are the explorer's and stay. Each line is `current -> proposed`, with what the member is, for review; only what the review accepts is applied, and a member left unlisted keeps its name.

| Current | Proposed | What it is |
|---|---|---|
| `Forma.Blazor.Components.Collections` (the grid's part) | `Forma.Blazor.Components.DataView` | The namespace of the grid and everything that belongs to it |
| `TGridItem` | `TItem` | The item type parameter, as every other collection component names it |
| `ColumnBase<TGridItem>` | `FormaColumnBase<TItem>` | The derivable column base, now the library's |
| `PropertyColumn`, `TemplateColumn` | `FormaPropertyColumn`, `FormaTemplateColumn` | The two columns a caller writes, now the library's |
| `FormaPropertyColumn<TGridItem, TProp>` | `FormaPropertyColumn<TItem, TValue>` | The bound column's type parameters, the value named as the inputs name theirs |
| `GridItemsProvider<TGridItem>` | `DataViewItemsProvider<TItem>` | The provider delegate |
| `GridItemsProviderRequest<TGridItem>` | `DataViewRequest<TItem>` | The window and sort a provider is asked for |
| `GridItemsProviderResult<TGridItem>` | `DataViewPage<TItem>` | The items and total a provider answers with, shaped as the catalog's page |
| `GridSort<TGridItem>` | `DataViewSort<TItem>` | The sort every view sorts by |
| `SortedProperty` | `DataViewSortedProperty` | One key of an applied sort, by name and direction |
| `IAsyncQueryExecutor` | `IDataViewQueryExecutor` | The service that counts and reads a queryable asynchronously |
| `EnableRowSelection` | `EnableItemSelection` | Pointer selection, which is over items whatever the view |
| `RowClass`, `OnRowClick` | `ItemClass`, `OnItemClick` | The caller's class and click per item, which the grid draws on a row |
| `ItemSize` | `ItemHeight` | The height of the view's virtualization unit |
| `QueryParameterNamePrefix` | `AddressOptions` | The prefix and the parameter names the sort, page and view are written to the address under |

## Open decisions

- **A date picker over a date that cannot be empty.** The picker's value is a nullable date, and the framework identifies a field only through a member of the input's own type, so a date member that cannot be empty is identified through a stand-in and the picker draws no validation state for it. The way through is the library's to decide: a picker bound to a date that cannot be empty, or one generic over both.
- **The tree under virtualization.** The tree draws every visible node. The design has it virtualizing over the visible nodes with a row as its unit, which would also settle grouping under virtualization by carrying headings as nodes; both wait on the same decision.
- **The list and the cards under virtualization.** Both draw every item the data view hands them, and a virtualizing data view hands a view nothing, fetching windows through the viewport instead; the two views refuse it. Virtualizing them in their own units, a row and a row of cards, waits on a decision about whether the framework's list virtualization can be asked for a row of cards at all.
- **Inputs the library lacks.** A number, a date with a time and a time are edited through the framework's inputs in the text box's look. The roadmap's number input replaces the first when it lands; the other two wait for a decision on whether the library wants them.

- **Grouping under virtualization.** A virtualizing view draws its windows as flat rows, and a heading has no row of its own in that model; a grouped set is paged instead. Whether the tree's model, virtualizing over the visible nodes with headings as nodes, should carry grouping too is decided when the tree lands.

- **Asking again for an equal query.** A new queryable is a new source, asked again, so a query to a store built in a page's markup asks the store on every render of the page. Skipping the question for a query equal to the last would take comparing expression trees with their captured values evaluated, as a store's query cache keys them, and a comparison that missed a real change would show stale items; nothing the data view serves needs it yet.
- **The gallery's timing.** Phase 12 is the gallery's own build, so it waits on the roadmap rather than on this plan.
