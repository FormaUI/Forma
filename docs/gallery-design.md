# Forma Gallery Design

A component scoped to **media only — images and videos, nothing else** — distinct from `FormaExplorer`, which browses any resource kind. Two views:

- **Files** — every media item as an individual thumbnail, grouped and labeled by date.
- **Collections** — folders shown as thumbnailed groups, also date-labeled, with a **favorites** flag and **categories** as an organizing dimension.

Inspiration is the iOS Photos app's Library/Albums split, renamed to fit this repository's vocabulary — **inspiration only, not a specification**, the same relationship this repo already has with Coi. The final UI fits Forma's own design language; nothing here is a port.

## Settled: architecture

`FormaGallery` is its own top-level component under `Components/Gallery/`, embedding `FormaExplorerAddressBar` and `FormaExplorerToolbar` directly for its chrome, with its own Files and Collections views underneath.

This was a genuine fork, decided rather than defaulted into. The alternative was adding Files/Collections as new `ExplorerViewMode` values on `FormaExplorer` itself, reusing the same `AddressBar`/`Toolbar` *instance* in place beside its existing Thumbnails/List/Details views. Rejected: a Gallery is a different enough surface — media-only, date-grouped, favorites, categories — that folding it into Explorer's view-mode switch would grow that switch past what it was designed for.

Reaching across categories for chrome is not a new pattern here. `FormaExplorer`'s own toolbar already embeds `FormaFilePicker` and `FormaFileUploader`; `FormaExplorer` delegates enlargement to `FormaFileViewer`; `FormaFileViewer` delegates its video branch to `FormaVideoPlayer`. `FormaGallery` embedding `FormaExplorerAddressBar`/`FormaExplorerToolbar` is the same kind of composition.

## Open decisions

- **Are favorites a per-item toggle only, or also a virtual collection beside the real ones?** The answer decides whether a favorite is a flag on the item or a query concern.
- **Are categories user-defined and hierarchical like folders, or a fixed taxonomy such as People, Places and media type?** The answer decides whether they ride on `IExplorerItemsProvider`'s existing folder concept or need a second axis alongside it.
- **What are the Files and Collections layouts exactly?** Settled against reference images when the component is implemented, not before.
