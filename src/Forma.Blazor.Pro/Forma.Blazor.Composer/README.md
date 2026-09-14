# Forma.Blazor.Composer

`FormaComposer`, the authoring surface for the **Forma** component library, hosting rich text,
Markdown and code, every capability arriving as a plugin. Built on `Forma.Blazor` and speaking
its namespaces, so a host that already renders the library adds this package and nothing else
changes: the same `AddFormaUI()` registers what it needs, the same
`@using Forma.Blazor.Components.Composer` line reaches it, and the same translation layout
localizes it.

**.NET 11 · Interactive Server components.**

Its file-picker dialog composes `FormaExplorer`, so this package depends on
`Forma.Blazor.Explorer` in turn. The contract the composer speaks, `Forma.Framework.Composer`,
ships inside this package; a plugin is written against it.
