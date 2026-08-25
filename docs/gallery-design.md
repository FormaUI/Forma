# FormaGallery — Design Notes

**Status: future.** Direction only — not yet queued in [COMPONENTS.md](../COMPONENTS.md)'s build order, and not yet a `docs/` "authoritative plan" in the sense [composer-design.md](composer-design.md) is for `FormaComposer`. This file exists so the architecture decision already made survives to whenever the component is actually picked up.

A component scoped to **media only — images and videos, nothing else** — distinct from `FormaExplorer` (#53), which browses any resource kind. Two views:

- **Files** — every media item as an individual thumbnail, grouped and labelled by date.
- **Collections** — folders shown as thumbnailed groups, also date-labelled, with a **favourites** flag and **categories** as an organizing dimension.

Inspiration is the iOS Photos app's Library/Albums split, renamed to fit this repository's vocabulary — **inspiration only, not a specification**, the same relationship this repo already has with Coi. The final UI fits Forma's own design language; nothing here is a port.

## Settled: architecture

`FormaGallery` is its own top-level component under `Components/Gallery/`, embedding `FormaExplorerAddressBar` and `FormaExplorerToolbar` directly for its chrome, with its own Files and Collections views underneath.

This was a genuine fork, decided rather than defaulted into. The alternative was adding Files/Collections as new `ExplorerViewMode` values on `FormaExplorer` itself, reusing the same `AddressBar`/`Toolbar` *instance* in place beside its existing Thumbnails/List/Details views. Rejected: a Gallery is a different enough surface — media-only, date-grouped, favourites, categories — that folding it into Explorer's view-mode switch would grow that switch past what it was designed for.

Reaching across categories for chrome is not a new pattern here. `FormaExplorer`'s own toolbar already embeds `FormaFilePicker` and `FormaFileUploader`; `FormaExplorer` delegates enlargement to `FormaFileViewer` (#56); `FormaFileViewer` delegates its video branch to `FormaVideoPlayer` (#55). `FormaGallery` embedding `FormaExplorerAddressBar`/`FormaExplorerToolbar` is the same kind of composition.

## Open, deferred to implementation time

- **Favourites** — a per-item toggle only, or also a virtual "Favourites" collection alongside the real ones? Decides whether it is a flag on `ResourceInfo` or a query concern.
- **Categories** — user-defined and hierarchical, like folders, or a fixed taxonomy (People, Places, media type)? Decides whether it rides on `IExplorerResourcesProvider`'s existing folder concept or needs a second axis alongside it.
- The exact Files/Collections layout and interaction. The maintainer will bring reference screenshots when the component is actually implemented, not before.

## Next step

Nothing to build yet. When implementation starts: confirm the Files/Collections layout against reference images, settle Favourites and Categories above, then follow the usual per-component workflow from `CLAUDE.md` — a `COMPONENTS.md` entry, and a fuller design document here if the component earns one.
