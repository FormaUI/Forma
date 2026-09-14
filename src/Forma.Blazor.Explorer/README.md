# Forma.Blazor.Explorer

`FormaExplorer`, a browser over the media a site holds, for the **Forma** component library: a
thumbnail grid or a details table over folders, with selection, upload, paging and a preview.
Built on `Forma.Blazor` and speaking its namespaces, so a host that already renders the library
adds this package and nothing else changes: the same `AddFormaUI()` registers what it needs, the
same `@using Forma.Blazor.Components.Explorer` line reaches it, and the same translation layout
localizes it.

**.NET 11 · Interactive Server components.**

The contract it speaks, `Forma.Framework.Explorer`, ships inside this package; an item provider
is written against it.
