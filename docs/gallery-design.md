# Forma Gallery Design

A component scoped to **media only — images and videos, nothing else** — distinct from `FormaExplorer`, which browses any kind of item. Two views:

- **Files** — every media item as an individual thumbnail, grouped and labeled by date.
- **Collections** — folders shown as thumbnailed groups, also date-labeled, with a **favorites** flag and **categories** as an organizing dimension.

Inspiration is the iOS Photos app's Library/Albums split, renamed to fit this repository's vocabulary — **inspiration only, not a specification**, the same relationship this repo already has with Coi. The final UI fits Forma's own design language; nothing here is a port.

## Settled: architecture

`FormaGallery` stands on its own. It is its own top-level component, shipping as its own package beside the explorer, `Forma.Blazor.Gallery`, with an engine of its own bundled inside it, and it never references `Forma.Blazor.Explorer` or the explorer's engine.

What it shares with the explorer is the model both browse, `Forma.Framework.Catalog`: a host supplies `ICatalogItem`s in full or an `ICatalogProvider` answering one window at a time, and the gallery lists the items that are images or videos. One provider therefore serves both surfaces, and a host that already feeds the explorer feeds the gallery with nothing new. Folders are `ICatalogFolderItem`s with the parentage the folder tree already indexes, and the gallery's collections are those folders. The requests it hands back, the ceilings it keeps, the view over a held collection, the window over a provider, the folder tree, the history and the selection are the package's too, so the gallery writes none of that bookkeeping a second time.

Its chrome is built from the library's own components, as every component's is. Where the explorer's chrome has something the gallery needs as well — the folder-tree sidebar, the address bar with its history — that piece is first abstracted into a general component in `Forma.Blazor`, which the explorer then composes too, rather than the gallery reaching into the explorer's package for it. The gallery's own parts read view and interaction contracts of the gallery's own, in its own engine, the way the explorer's parts read the explorer's.

This was a genuine fork, decided rather than defaulted into. The alternative was adding Files/Collections as new `ExplorerViewMode` values on `FormaExplorer` itself, reusing the same address bar and toolbar *instance* in place beside its existing Thumbnails/List/Details views. Rejected: a Gallery is a different enough surface — media-only, date-grouped, favorites, categories — that folding it into Explorer's view-mode switch would grow that switch past what it was designed for.

## Open decisions

- **Are favorites a per-item toggle only, or also a virtual collection beside the real ones?** The answer decides whether a favorite is a flag on the item, which `ICatalogItem`'s state and capabilities would carry, or a query concern the provider answers.
- **Are categories user-defined and hierarchical like folders, or a fixed taxonomy such as People, Places and media type?** The answer decides whether they ride on `ICatalogProvider`'s existing folder concept or need a second axis alongside it.
- **What are the Files and Collections layouts exactly?** Settled against reference images when the component is implemented, not before.
