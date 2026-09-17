# Forma.Blazor.Showcase

The application that renders every **Forma** component in every state it can enter, in each culture
the Showcase serves and both writing directions, so a component can be looked at. It is not a
package and never ships; it is the review surface for anyone working in this repository, and a
complete working host to copy from.

**.NET 11 · Interactive Server components.**

## What it shows

One page per component, grouped by the category the component lives in — buttons, inputs, menus,
feedback, layout, files, windows and the rest — plus the composite surfaces of `Forma.Blazor.Pro`,
`FormaExplorer` and `FormaComposer`, under Features. Two further groups hold things that are not
components: Libraries demonstrates a Framework library on its own, such as localization, and
Debugging holds tooling, such as the input inspector that shows which owner each key, wheel turn
and drop resolves to.

The Showcase also registers the AddOns a host may opt into, so their effect can be seen beside the
library's own defaults: the Twemoji emoji pack, the Office and Markdown resource converters, and
the share targets. Where a component's behavior depends on which package supplies it, the page
carries a tab per variant, the library's own first.

Everything a host would supply is supplied here in a stand-in form: a file store that writes into
the project's `Sandbox` folder, an image library the composer's image dialog browses, a custom
media player, a status badge catalog, a stand-in cloud drive account for the share menu, and two
host input routes — a preemptive palette shortcut on `K` with the platform's shortcut modifier,
and an `F1` help key. `wwwroot/media`
holds the sample documents, video, audio and captions the file and media pages open.

## Cultures

The Showcase routes four cultures: English, unprefixed and the default; Spanish (`es-419`) under
`/es`; Hebrew under `/he`; and French under `/fr`. English, Spanish and Hebrew are in the
sidebar's culture switcher, and each page's copy is localized for them beside the page. French
ships with neither the library nor the Showcase: the project's `locales` folder supplies a few
components' strings in it, which is how a host adds a language the library never had, and the
Localization page links into it so everything else can be seen falling back to English.

## Running it

```pwsh
dotnet run --project src/Forma.Blazor.Showcase
```

It listens on `http://localhost:5000` and opens a browser. Three things to know while it runs:

- It runs a compiled binary with no hot reload. After a change, stop it, rebuild, and start it
  again; the library's DLL under the Showcase's `bin` output being newer than the sources you
  changed is the proof the rebuild reached it. A stale process silently serves the old code.
- Stop it before rebuilding. A running Showcase holds its `bin` DLLs open, and the copy fails with
  `MSB3027`.
- Don't pipe its output into `Select-String`, `Select-Object` or another filter; closing the
  pipeline terminates the server.

Reviewing a component means looking at its page in every culture the switcher offers.

## Moving around

The sidebar lists every page by category, and the home page indexes the same list with a count
per group. The culture switcher at the top of the sidebar keeps the current page and changes only
its culture. A page with more than one variant shows a tab strip above its content, each tab a
page of its own.

## Adding a page

A page is any routed component under `Components/Pages/{Category}/` that carries
`@attribute [ShowcasePage("FormaX")]`; the catalog reads the attribute and the folder at startup,
and the sidebar, home page and tab strip follow from that with nothing else to register. The
folder is the component's own category, the page is named `FormaXPage.razor`, its strings are a
section of the category's locale pair, and it is structured as a heading and a description
followed by one section per scenario, each with a heading and a box. A second page named
`FormaXPage.Variant.razor` demonstrates the same component through another package and joins the
first page's tab strip by its name alone. The repository's component authoring guide states what
a page must render.

## License

MIT, under the repository's [`LICENSE.md`](../../LICENSE.md).
