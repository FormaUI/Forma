# Forma Explorer Design

## Scope

`FormaExplorer` and its parts, and the vocabulary they speak: items and folders, the two
ways data arrives, the two options records and the constraints, item state and
capabilities, and what the parts read and ask of the explorer.

Sharing, tagging, search past an item's name, and bulk edit are outside this design: each is a
surface of its own, designed where it is built.

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
- **Items, not resources.** The host supplies `ICatalogItem`s, a folder being an
  `ICatalogFolderItem` whose parentage is a key, and every callback and request hands
  back the item the host supplied, so a folder with no resource behind it is a first-class
  folder. `CatalogItem.For`, `ForAll` and `ForFolder` wrap resources for a host that has
  them.
- **Two ways in, exactly one supplied.** `Items` hands over a collection the explorer
  holds and filters, sorts and pages itself, right for a few hundred items.
  `ItemsProvider` answers one `CatalogQuery` at a time and the explorer holds only
  the items it is showing, so a collection far too large to enumerate stays browsable.
- **The query names its location.** The folder whose files it asks for, captured when the
  request is issued, null for All Media, with the slice, the search box and the sort, so a
  provider answers from the query alone, whichever folder the host has moved to by the time
  it answers. Files the sort ties stand in the ascending ordinal order
  of their keys in either direction, a rule held items and providers share, so neighboring
  slices neither repeat nor skip a file. The provider supplies any other filter the host
  applies itself. A page's total is optional, so a store that cannot count cheaply leaves it
  out and the explorer drops its pager rather than demanding a number.
- **Pages are cut in the store's positions.** A store that returns hidden items counts
  them, and the explorer passes the kinds it lists in the query for a store that applies
  them before paging; the explorer pages and asks for the next slice in the store's own
  positions, so a short slice, or one it leaves out whole, still moves the next one on and
  never ends the collection. It says how many items there are only where the store
  guarantees the count of what the whole query lists, since what one slice left out says
  nothing of the slices not yet read; otherwise it says how many it shows.
- **Two options records, split by what they govern.** The item options gate every mutating
  capability per caller: the item and folder capabilities the explorer permits,
  intersected with what each item itself supports, and whether uploading is allowed. The
  explorer's own options cover how it presents itself: dialog and resizable modes, sidebar
  and breadcrumb-menu visibility, the optional details columns and their header, the
  selection limit, whether the collection is paged or scrolls infinitely, the search
  debounce, and selecting on open for a caller that exists only to choose, which starts in
  selection mode with the toggle out of the toolbar.
- **Constraints are ceilings the explorer refuses to cross.** The constraints name
  the resource kinds it lists and accepts, the kinds an upload may add where they differ,
  and the most top-level folders, subfolders,
  nesting depth, items per folder, items and bytes it holds; an upload, a new folder or a
  drop that would cross one is refused, announced in an alert region under the header and
  reported through `OnRejected` with its reason.
- **Effective capabilities.** What may be done to an item is its own set intersected with
  what the explorer permits for its kind, and nothing while the item is disabled. Every
  entry point enforces that set and the views draw from it. Opening is a folder's
  capability.
- **Item state.** A hidden item leaves the listing, the provider's items, the tree, the
  breadcrumb menus and every count of what is listed; an invisible one keeps its place
  undrawn; a disabled one is drawn muted, its controls disabled, and cannot be selected.
- **One kind selected at a time.** Choosing a folder clears selected items and choosing
  an item clears selected folders, so a selection holds one kind and delete-selected routes
  to exactly one batch.
- **The parts read a view and act through an interaction.** The explorer exposes what its
  parts read and what they may ask as view and interaction pairs named for their subjects,
  location, content, search, selection, item and the sidebar, and configuration as a view alone,
  since a part reads it and asks nothing of it. A part takes the narrowest
  pair it reads, so adding an action or a column is a one-file change and every selection
  change is reported exactly once.
- **One confirmation surface.** Rename, delete, delete-selected and create-folder are
  pending actions held one at a time, and the explorer renders one modal per kind of
  action, so a folder and an item share the rename dialog.

## The parts

A command area composes the address bar and the toolbar. The address bar carries back,
forward, up and refresh with the breadcrumb menu and the search box; the toolbar carries
selection, a new folder where no sidebar hosts one, upload, the view switch, the columns
menu in the details view and a sort menu that also groups, the whole row scrolling behind
arrows when it outgrows the width. While items are selected a selection bar stands in for
the toolbar with delete, clear, the count and the way out of selection mode. Where the
header has no room for the search box, the search stands as a glyph until pressed; at a
phone's width the header collapses to the breadcrumb, the search glyph and one menu
holding navigation, actions, view mode, sorting and grouping.

The content area and the folder-tree sidebar are one data view over the catalog: the
thumbnail, list and details views are its cards, list and grid over the current folder,
drawn through the explorer's item template, and the sidebar is its tree over the folders,
its width set by a splitter, with a new-folder and a collapse-all tool over it. Grouping,
selection, and the paged or infinite collection are the data view's. Below a tablet's
width the sidebar stacks above the content. Each item carries its depiction and its
operations, which a checkbox replaces while selecting.

Three surfaces stay on `FormaExplorer` rather than on the region they appear over, because
each is reached from more than one of them: the preview lightbox, which activation, the
action menu and `FormaFileViewer`'s sibling stepping all drive through the explorer; the
confirmation modals, since one rename dialog serves items and folders alike; and the upload
panel, whose completion re-reads the items and reports what was uploaded.

The details view keeps every column it is configured with, its headers sorting, its name
column resized by dragging the frozen edge; the columns menu adds or removes the optional
columns and hides the header. Grouping by name, size, type or date folds under headings,
a folded group staying folded across views and opening on a new grouping.

Typing in the search box filters a held collection at once and is debounced before a
provider is asked; a superseded query is canceled rather than merely ignored, and a
mutation of the items re-reads the items shown. The items scroll inside their own panel so
the toolbar, folder tree and pager stay put.

## Assemblies

The model every browsing surface shares is `Forma.Framework.Catalog`: the items and
folders, the provider with its query and page, the sort, the resource filter, the requests
and the rejection, the constraints, and the history and selection a surface keeps. A host
writes its items and its provider against it alone, and a second surface such as the
gallery builds on it without reaching the explorer. The explorer's own engine,
`Forma.Framework.Explorer`, holds its options, the contracts its parts read and ask, the
item actions with their confirmations and drag state, and the preview.
`Forma.Blazor.Explorer` is the component.

## Non-goals

- Storage. The explorer reports intent; the host moves bytes.
- A viewer of its own. Enlargement is `FormaFileViewer`.
- The device. New files arrive through `FormaFilePicker` and `FormaFileUploader` in
  a panel the toolbar opens.
