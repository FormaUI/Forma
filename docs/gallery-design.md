# Forma Gallery Design

A component for **media**, images and videos, beside `FormaExplorer`, which browses any kind of item. Two views:

- **Files** — every media item as an individual thumbnail, grouped and labeled by date.
- **Collections** — folders shown as thumbnailed groups, also date-labeled, with a **favorites** flag and **categories** as an organizing dimension.

The split takes after the iOS Photos app's Library and Albums, named in the library's own vocabulary, as an inspiration in the way the legacy application is one: the gallery takes its shape in Forma's own design language.

## Architecture

`FormaGallery` is its own top-level component, shipping as its own package beside the explorer, `Forma.Blazor.Gallery`, because what makes it a gallery, media alone, grouped by date, with favorites and categories, would grow the explorer's view switch past what that switch is for. It builds on the catalog and the data view alone, browsing through `FormaDataView` in the cards view at its compact density with the items grouped by date, so its source, selection, paging, address and states are the engine's; what the gallery keeps for itself is what is about media rather than browsing: favorites, categories, collections, and the lightbox, slideshow and playback it opens an item into.

What it shares with the explorer is the model both browse, `Forma.Framework.Catalog`: a host supplies catalog items in full or a provider answering one slice at a time, and the gallery lists the items that are images or videos. One provider therefore serves both surfaces, and a host that already feeds the explorer feeds the gallery with nothing new. The gallery's collections are the catalog's folders, and the requests it hands back, the ceilings it keeps and the history are the package's too, so the gallery writes none of that bookkeeping a second time.

Its chrome is built from the library's own components, as every component's is. A piece of chrome both surfaces need, such as the explorer's address bar with its history, moves into `Forma.Blazor` as a general component that both compose. The gallery's parts read view and interaction contracts in the gallery's own engine, as the explorer's parts read the explorer's.

## Open decisions

- **Are favorites a per-item toggle only, or also a virtual collection beside the real ones?** The answer decides whether a favorite is a flag on the item, which a catalog item's state and capabilities would carry, or a query concern the provider answers.
- **Are categories user-defined and hierarchical like folders, or a fixed taxonomy such as People, Places and media type?** The answer decides whether they ride on the catalog's folders or need a second axis alongside it.
- **What are the Files and Collections layouts exactly?** The answer decides how much of each the data view's cards view draws and what the gallery adds around it.
