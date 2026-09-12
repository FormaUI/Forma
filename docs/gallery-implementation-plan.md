# Forma Gallery Implementation Plan

## Status

Future. `FormaGallery` is not yet queued in [the component roadmap](component-roadmap.md), and [the gallery design](gallery-design.md) is direction only; it exists so the architecture decision already made survives to whenever the component is picked up.

## Open, deferred to implementation time

- **Favourites** — a per-item toggle only, or also a virtual "Favourites" collection alongside the real ones? Decides whether it is a flag on `ResourceInfo` or a query concern.
- **Categories** — user-defined and hierarchical, like folders, or a fixed taxonomy (People, Places, media type)? Decides whether it rides on `IExplorerResourcesProvider`'s existing folder concept or needs a second axis alongside it.
- The exact Files/Collections layout and interaction. The maintainer will bring reference screenshots when the component is actually implemented, not before.

## Next step

Nothing to build yet. When implementation starts: confirm the Files/Collections layout against reference images, settle Favourites and Categories above, then follow the repository's working rules — a catalog entry, and a fuller design if the component earns one.
