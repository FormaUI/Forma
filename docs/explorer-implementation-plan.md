# Forma Explorer Implementation Plan

The plan for building the explorer that [the explorer design](explorer-design.md) states,
and the record of what has landed.

## Status

The initial release is built: browse, select, enlarge, upload, rename, delete, folder
create, rename, delete and drag-move, sort by name, size, kind, date added and date
modified, grouping, the details view's columns and name-column resize, the list view,
item states and per-item capabilities, constraints and their refusals, the header's
collapse at narrow widths, and either pagination or infinite scrolling. The explorer
shipped standalone: its sort control is a `FormaDropDownMenu` and its view switch three
`FormaToggleButton`s, because `FormaSortMenu` and `FormaViewButton` did not exist when it
was built and its own shape turned out simple enough not to need them.

The explorer's regions were split from one component into the command area, the sidebar
and the content area, with the item model, the pending-action types and the view and
interaction pairs following. Two placements stand as departures rather than defects: the
preview lightbox and the confirmation modals live in `FormaExplorer` rather than the
content area, because activation, the action menu and `FormaFileViewer`'s sibling stepping all
reach the preview through the explorer and the rename dialog serves items and folders
alike; and the upload panel lives in `FormaExplorer` rather than the command area, because
its completion re-reads the provider window and raises `OnUploaded`, both the explorer's.
`_breadcrumbs` is memoized on the explorer although only `FormaExplorerBreadcrumb` reads
it.

## Still to come

Sharing, tagging, search beyond the name, and bulk edit. Composing `FormaSortMenu` and
`FormaViewButton` once they exist.
