# Forma Explorer Design

## Scope

`FormaExplorer` and its parts, and the `Forma.Framework.Explorer` vocabulary they speak:
items and folders, the two ways data arrives, the two options records and the constraints,
item state and capabilities, and the view and interaction contracts the parts read.

Sharing, tagging, search past an item's name, and bulk edit are later additions rather than
non-goals: each is a surface the explorer is expected to grow, and none is designed here.

## Purpose

A file manager for what is already on the server, in the shape of a resource explorer: a
resizable folder-tree sidebar, an address bar with back, forward, up, refresh and a
breadcrumb menu, thumbnail, details and list views, search, grouping and sorting, drag and
drop between folders, upload through `FormaFilePicker` and `FormaFileUploader` in a panel
its toolbar opens, and enlargement through `FormaFileViewer` rather than a viewer of its
own. It owns no storage: every mutation is reported to the host as intent through a
callback, and an upload goes into the host's `IFileStore`.

> Which items are shown, what may be done to each, and how does the host learn what the
> reader asked for?

## Decision summary

- **Any resource.** An item is anything with a `ResourceInfo` behind it, or nothing at
  all: an image draws its thumbnail, anything else the glyph of its kind, and
  the details view lists kind, size and dates for all of them. It browses the server; it
  never picks from the device, which is `FormaFilePicker`'s job.
- **Items, not resources.** The host supplies `IExplorerItem`s, a folder being an
  `IExplorerFolderItem` whose parentage is a key, and every callback and request hands
  back the item the host supplied, so a folder with no resource behind it is a first-class
  folder. `ExplorerItem.For`, `ForAll` and `ForFolder` wrap resources for a host that has
  them.
- **Two ways in, exactly one supplied.** `Items` hands over a collection the explorer
  holds and filters, sorts and pages itself, right for a few hundred items.
  `ItemsProvider` answers one `ExplorerItemQuery` at a time and the explorer holds only
  the window it is showing, so a collection far too large to enumerate stays browsable.
  The split follows `FormaDataGrid`'s.
- **The query carries only what the explorer owns.** The window, the search box and the
  sort. The folder is the host's own `CurrentFolder` parameter and its implementation
  already has it, and the same closure supplies any other filter the explorer never needs
  to learn about. `ExplorerItemPage.TotalCount` is optional, so a store that cannot count
  cheaply returns null and the explorer drops its pager rather than demanding a number.
- **Hidden items count in the store's total.** A store that returns hidden items counts
  them; the explorer pages and asks for the next window in the store's own count, and
  reports an appended window's progress against the total less the items it left out.
- **Two options records, split by what they govern.** `ExplorerItemOptions` gates every
  mutating capability per caller: the item and folder capabilities the explorer permits,
  intersected with what each item itself supports, plus `AllowUpload`. `ExplorerOptions`
  covers how it presents itself: dialog and resizable modes, sidebar and breadcrumb-menu
  visibility, the optional details columns and their header, the selection limit, whether
  the collection is paged or scrolls infinitely, the search debounce, and `SelectOnOpen`
  for a caller that exists only to choose, which starts in selection mode with the toggle
  out of the toolbar.
- **Constraints are ceilings the explorer refuses to cross.** `ExplorerConstraints` names
  the resource kinds it lists and accepts, and the most top-level folders, subfolders,
  nesting depth, items per folder, items and bytes it holds; an upload, a new folder or a
  drop that would cross one is refused, announced in an alert region under the header and
  reported through `OnRejected` with its reason.
- **Effective capabilities.** `item.GetCapabilities(options)` and
  `item.Permits(options, capability)` are the item's own set intersected with what the
  explorer permits for its kind, and none while the item is disabled. Every entry point
  enforces them and the views draw from them. Read, write and execute are reserved and
  not supported; open is supported for folders only.
- **Item state.** A hidden item leaves the listing, the provider window, the tree, the
  breadcrumb menus and every count of what is listed; an invisible one keeps its place
  undrawn; a disabled
  one is drawn muted, its controls disabled, and cannot be selected.
- **One kind selected at a time.** Choosing a folder clears selected items and choosing
  an item clears selected folders, so a selection holds one kind and delete-selected routes
  to exactly one batch.
- **The parts read a view and act through an interaction.** The explorer implements
  `IExplorerView` and `IExplorerInteraction`, aggregates of subject-named pairs for
  configuration, location, content, search, selection and item, and cascades them with the
  item manager beside them as `IExplorerItemInteraction`; the sidebar implements the
  sidebar pair and cascades it to its nodes. A part takes the narrowest
  pair it reads, so adding an action or a column is a one-file change and every selection
  change is reported exactly once.
- **One confirmation surface.** Rename, delete, delete-selected and create-folder are
  pending actions the item manager owns behind one slot, and the explorer renders one
  modal per pending-action type, so a folder and an item share the rename dialog.

## The parts

`FormaExplorerCommandArea` composes the address bar and the toolbar; the address bar
carries back, forward, up and refresh with the breadcrumb menu and the search box, and the
toolbar selection, a new folder where no sidebar hosts one, upload, the view switch, the
columns menu in the details view and a sort menu that also groups, the whole row scrolling
behind arrows when it outgrows the width. While items are selected
`FormaExplorerSelectionBar` stands in for the toolbar with delete, clear, the count and the
way out of selection mode. `FormaExplorerSidebar` is the folder tree, its width set by a
splitter and its height by a sash, a new-folder and a collapse-all tool over it, its nodes
asking the item interaction to rename and raising folder deletion for the explorer to
route. `FormaExplorerContentArea` renders the empty state, the three views, the
infinite-scroll sentinel with its progress, and the pagination; the views share
`FormaExplorerThumbnail` for the depiction, `FormaExplorerActionBar` for per-item
operations, swapped for a checkbox while selecting, and `FormaExplorerGroupHeading` over
each group. Where the header has no room for the search box, the search stands as a glyph
until pressed; at a phone's width the header collapses to the breadcrumb, the search glyph
and one menu holding navigation, actions, view mode, sorting and grouping, and below a
tablet's width the sidebar stacks above the content.

Three surfaces stay on `FormaExplorer` rather than on the region they appear over, because
each is reached from more than one of them: the preview lightbox, which activation, the
action menu and `FormaFileViewer`'s sibling stepping all drive through the explorer; the
confirmation modals, since one rename dialog serves items and folders alike; and the upload
panel, whose completion re-reads the provider window and raises `OnUploaded`.

The details view keeps every column it is configured with, its headers sorting, its name
column resized by dragging the frozen edge, and lets its pane scroll sideways when they do
not fit; the columns menu adds or removes the optional columns and hides the header. The
list view flows names down and into further columns. Grouping by name, size, type or date
folds under headings, a folded group staying folded across views and opening on a new
grouping.

Typing in the search box filters a held collection at once and is debounced through
`Options.SearchDebounceDelay` before a provider is asked; a superseded query is canceled
rather than merely ignored, and a mutation of the items re-reads the window. The items
scroll inside their own panel so the toolbar, folder tree and pager stay put.

## File layout

Two assemblies, foldered by concern with the namespace following the folder.

**`Forma.Framework.Explorer.Items` is the model every browsing surface shares**, packable, rooted in
its own namespace: the items and folders, the provider with its query and page, the sort, the resource
filter, the requests and the rejection, the constraints, and the bookkeeping a surface repeats — the
view over a held collection, the window over a provider, the folder tree, the history and the
selection with what parts read of it. A host writes its items and its provider against this package
alone, and a second surface such as the gallery builds on it without reaching the explorer.

**`Forma.Framework.Explorer` is the explorer's own engine**, bundled inside the component's package:
its options and item options, every view and interaction contract its parts read and ask, the item
manager with its actions, pending confirmations and drag state, the grouping and its bands, the
preview, and the paging and infinite-scroll choices. Nothing here is written for another surface.

**`Forma.Blazor.Explorer` is the component**, one namespace, its parts reading the engine's contracts
through cascades. The tests mirror the three: the items package and the engine each have a test
project named after them, and the component's tests drive it whole.

## Non-goals

- Storage. The explorer reports intent; the host moves bytes.
- A viewer of its own. Enlargement is `FormaFileViewer`.
- The device. New files arrive through `FormaFilePicker` and `FormaFileUploader` in
  a panel the toolbar opens.
