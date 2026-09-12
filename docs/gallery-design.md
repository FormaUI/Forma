# Forma Gallery Design

A component scoped to **media only — images and videos, nothing else** — distinct from `FormaExplorer`, which browses any resource kind. Two views:

- **Files** — every media item as an individual thumbnail, grouped and labelled by date.
- **Collections** — folders shown as thumbnailed groups, also date-labelled, with a **favourites** flag and **categories** as an organizing dimension.

Inspiration is the iOS Photos app's Library/Albums split, renamed to fit this repository's vocabulary — **inspiration only, not a specification**, the same relationship this repo already has with Coi. The final UI fits Forma's own design language; nothing here is a port.

## Settled: architecture

`FormaGallery` is its own top-level component under `Components/Gallery/`, embedding `FormaExplorerAddressBar` and `FormaExplorerToolbar` directly for its chrome, with its own Files and Collections views underneath.

This was a genuine fork, decided rather than defaulted into. The alternative was adding Files/Collections as new `ExplorerViewMode` values on `FormaExplorer` itself, reusing the same `AddressBar`/`Toolbar` *instance* in place beside its existing Thumbnails/List/Details views. Rejected: a Gallery is a different enough surface — media-only, date-grouped, favourites, categories — that folding it into Explorer's view-mode switch would grow that switch past what it was designed for.

Reaching across categories for chrome is not a new pattern here. `FormaExplorer`'s own toolbar already embeds `FormaFilePicker` and `FormaFileUploader`; `FormaExplorer` delegates enlargement to `FormaFileViewer`; `FormaFileViewer` delegates its video branch to `FormaVideoPlayer`. `FormaGallery` embedding `FormaExplorerAddressBar`/`FormaExplorerToolbar` is the same kind of composition.
