# Forma.Blazor.Pro

The explorer and the composer for the **Forma** component library: `FormaExplorer`, a browser
over the media a site holds, and `FormaComposer`, the authoring surface hosting rich text,
Markdown and code. Both are built on `Forma.Blazor` and speak its namespaces, so a host that
already renders the library adds this package and nothing else changes: the same `AddFormaUI()`
registers what they need, the same `@using Forma.Blazor.Components.…` lines reach them, and the
same translation layout localizes them.

**.NET 11 · Interactive Server components.**

The contracts the two components speak, `Forma.Framework.Explorer` and `Forma.Framework.Composer`,
ship inside this package; a plugin or an item provider is written against them.
