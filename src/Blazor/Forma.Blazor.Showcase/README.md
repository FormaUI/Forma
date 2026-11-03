# Forma.Blazor.Showcase

The application that renders every Forma component in every state it can enter, in each culture it
serves and in both writing directions. It is where a component is reviewed, and a complete working
host to copy from: its `Program.cs` registers and maps the library, the data view, the explorer, the
composer and the AddOns the way a host does.

**.NET 11 · Interactive Server components.**

## Running it

```pwsh
./scripts/Start-Showcase.ps1
```

The script builds the Showcase and starts it at `http://localhost:5000`; `-Port` chooses another port,
and `-Force` stops the Showcase already listening there first.

- It runs a compiled build. After a change, start it again; the library's DLL in the Showcase's `bin`
  output being newer than the sources you changed shows the rebuild reached it.
- A running Showcase holds its `bin` DLLs open, so a build started beside it fails to copy them with
  `MSB3027`; `-Force` stops it first.
- Let its output go to the terminal; piping it into `Select-String`, `Select-Object` or another
  filter ends the server when the pipeline closes.

## What it shows

The sidebar lists one page per component, grouped by the category folder the component lives in;
the home page indexes the same list with a count per group. Features holds `FormaComposer`,
`FormaDataView`, whose grid, list, cards and tree are tabs of its page, and `FormaExplorer`; Libraries shows a
Framework library on its own, such as localization; and Debugging holds tooling, such as the input
inspector that shows which owner each key, wheel turn and drop resolves to.

A page whose component can be supplied by another package, such as `FormaEmoji` drawn by the
Twemoji pack, carries a tab per variant, the library's own first.

Everything a host supplies is supplied here in a sample form: a file store writing into the
project's `Sandbox` folder, an image library for the composer's file dialog, the Office and
Markdown resource providers over the documents in `wwwroot/media`, a custom media player, a status
badge catalog, share accounts for the share menu, and two input routes, a palette shortcut on
`K` with the platform's shortcut modifier and an `F1` help key.

## Cultures

| Culture | Address |
| --- | --- |
| English, the default | `/` |
| Spanish (`es-419`) | `/es` |
| Hebrew | `/he` |
| French | `/fr` |

The switcher at the top of the sidebar changes between English, Spanish and Hebrew in place, keeping
the current page, what the reader typed or opened on it, and the reader's place; an address in a
culture still loads in it as a new document. French shows a host adding a language: the project's `.locales` folder supplies a few
components' strings in it, and the Localization page links into it with everything else in English.
Reviewing a component means looking at its page in every culture the switcher offers.

## Adding a page

A page is a routed component in `Components/Pages/{Category}/`, the component's own category,
named `FormaXPage.razor` and marked with `@attribute [ShowcasePage("FormaX")]`. The sidebar, the
home page and the tabs are read from the attribute and the folder at startup. A page opens with a
heading and a description, followed by a section per scenario, each with a heading and a box, and
its strings are a section of the category folder's `.locales` files. Every component under
`Components/Pages/` derives `ShowcaseComponentBase` through the folder's `_Imports.razor`, so it renders
again when the culture changes in place, reads its strings through the base's `L` and formats by its
`CultureState`; a label it builds once it builds again in `OnCultureChangedAsync`, and one that releases
something of its own overrides `Dispose(bool)`.

A variant is a sibling file named `FormaXPage.{Variant}.razor` with a route of its own; it joins
`FormaXPage`'s tabs by its name, and reads `FormaXPage`'s strings by overriding
`LocalizationSourceType`. The repository's component authoring guide states what a page
must render.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
