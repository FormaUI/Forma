# Forma.Blazor

A reusable Blazor component library — modals, toasts, pickers, tabs, data grids, form inputs, and
file intake. Localized, RTL-ready, and JS-free apart from what the DOM cannot do without script.
`Forma.Blazor.Pro` adds the file explorer and the `FormaComposer` authoring surface on top of it,
under the same namespaces and the same registration.

**.NET 11 · Interactive Server components.**

Installing the one package brings everything a host needs, with the Framework pieces it builds on restored beside it as packages of their own; optional artwork packs and add-ons are separate and opt-in, and the Pro package is installed beside it for the explorer and the composer.
The host owns its own `App.razor` and `Routes.razor`; the package ships components, styles, and JS.
`src/Forma.Blazor.Showcase` is a complete working host to copy from.

## Install

```pwsh
dotnet add package 2Site.Forma.Blazor
```

## Quick start

**1. Register services:**

```csharp
using Forma.Framework.Localization.Json;
using Forma.Blazor;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.Services.AddFormaUI();
```

`AddFormaUI()` registers everything the components need to work — toasts, confirmation dialogs, and
the sanitizer the composer holds content to. Each lands only if nothing is registered for it
already, so registering your own implementation before that call replaces it and leaves the rest
untouched. Call it after `AddRazorComponents()` for the same reason, so Blazor's own registrations
are in place before the fallbacks are considered.

**2. Render Forma's document head from your host's `App.razor`:**

```razor
<head>
    <FormaPageHead Title="My App" IconHref="@Assets["favicon.svg"]">
        <link rel="stylesheet" href="@Assets["app.css"]" />
        <link rel="stylesheet" href="@Assets["MyApp.styles.css"]" />
    </FormaPageHead>
</head>
```

It declares the character set, viewport, title, base address and icon, links Forma's stylesheets in cascade
order, and closes with the framework's own `ResourcePreloader`, `ImportMap` and `HeadOutlet`
— so a host adds none of those itself. A host's own head content goes in the child content, where
its styles land after Forma's and win. `HeadOutlet` renders last, so a page's `PageTitle` still
overrides the title set here.

Set `PreloadAssets="false"` to leave out the runtime preloading, which only a WebAssembly render mode
benefits from.

**3. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);
```

Components that use JS interop, such as `FormaDialog` and `FormaTextBox`, need the Interactive Server
render mode.

## Components

The library covers the admin-app staples — form inputs and pickers, modals and toasts, data
grids, toolbars, tabs, and file intake; `Forma.Blazor.Pro` adds the file explorer and the
`FormaComposer` authoring surface.
[`docs/component-catalog.md`](docs/component-catalog.md) lists the components that exist, one line each, with the design document each belongs to.

`FormaStatusBadge` needs a catalog of the statuses **your** application defines — the library ships
none:

```csharp
builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)),
    new("published", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430))
]));
```

```razor
<FormaStatusBadge Text="Published" Status="published" />
```

## Styling and theming

`FormaPageHead` links the stylesheets for you. The layers, for a host that would rather link them
itself:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only, no selectors | Always — components read these |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Unless your app already normalizes |
| `forma-base.css` | Body typography and link colors | You want Forma's look; skip it if you have your own |
| `forma-input.css` | The input box every field draws — border, radius, colors, focus ring | Whenever you use any Forma input |
| `forma-prose.css` | The look of a document the composer authored — headings, quotes, code, lists, tables, figures, links | Whenever you render composer content, in the editor or to readers |

Retheming is two things: redefine the gray scale and the six role solids in a stylesheet loaded
after Forma's, and every other token — hover, focus, links, backdrops, borders — follows.

## Icons

`FormaIcon` draws an `Icon`, which is one of three things. A `Glyph` — named for what it means
rather than for the set it is drawn from — in a `GlyphVariant`, the provider's primary or alternate
representation; the library's own set draws the two as an outline and a solid shape. A `Brand`,
whose official mark is drawn with the brand's name as its accessible name, for a link to a presence
on that platform. Or a single character, a `Rune`, set by the symbol font in the same box, for a
badge, a keyboard hint or an initial where a drawing would be overkill. A bare `Glyph`, `Brand` or
`Rune` converts to an icon, so `Glyph.Save`, `Brand.GitHub` and `new Rune('Ω')` are all accepted
wherever an icon is asked for; `Icon.FromGlyph(Glyph.Save, GlyphVariant.Alternate)` names the
alternate.

The artwork for every glyph and brand mark ships inside the library — glyphs drawn to
[its own specification](docs/glyph-artwork-spec.md), marks fetched from Simple Icons — so a host
registers nothing and loads no icon font. Artwork is resolved through `IIconProvider`; a host that
wants a different set registers its own provider, and a subtree or a single icon can name one
through a cascading value or the `Provider` parameter. Whatever a host's provider declines to draw,
the library's own artwork answers.

## Emoji

`FormaEmoji` draws an emoji through `IEmojiProvider`, and `FormaEmojiPicker` browses the set an
`IEmojiCatalog` describes. With neither registered, `FormaEmoji` falls back to the reader's own
platform font and the picker shows its empty state, because a catalog is data nothing can stand in
for. To draw the bundled artwork and browse the bundled catalog, add the
[Twemoji artwork pack](src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/README.md):

```pwsh
dotnet add package 2Site.Forma.AddOns.Emojis.Twemoji
```

```csharp
builder.Services.AddFormaTwemojiPack();
```

The artwork pack's own README covers what it registers, the artwork it ships, its license and
attribution, and how to regenerate it.

## Localization

Components localize themselves, so `AddJsonLocalization()` is all they need. To localize your own
strings, inject `IStringLocale<T>` and look a string up by its English text:

```razor
@inject IStringLocale<MyPage> L

<p>@L["Save"]</p>
<p>@L["Stored {stored} of {total}.", stored, total]</p>
```

Translations sit in `MyPage.he.json` beside the type, or in a file named for the folder with a section
per type, keyed by that same English text. A key with no translation renders as written, so a page
reads correctly before any translation exists.
A host rewords a string the library ships, or adds a language it never had, by laying its own files
in a `locales` folder beside the application, named for the assembly and the type they localize.

[`Forma.Framework.Localization`](src/Forma.Framework/Forma.Framework.Localization/README.md)
documents the contract, the ways to fill a hole, the plural syntax, and how a file is found.
[`Forma.Framework.Localization.Json`](src/Forma.Framework/Forma.Framework.Localization.Json/README.md) documents the
file format.

## Culture in the URL

Optional, and off unless you ask for it. Culture routing lives in `Forma.Framework.Globalization`,
which the components do not depend on, so a host references that assembly beside the library:

```csharp
builder.Services.AddCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", Culture = new("en") },
        new() { Token = "he", Culture = new("he") },
        new() { Token = "es", Culture = new("es-419"), Aliases = ["es-419"] }
    ]
});

app.UseCultureRouting();
```

`Token` is the URL segment, `Culture` the culture it applies, and `Aliases` are spellings that
redirect to `Token`. A token must be a value Google accepts as an `hreflang`; set `HrefLang`
explicitly when it is not, as `Token = "latam"` would need `HrefLang = "es"`.
`CulturePrefix.Always` gives every culture a segment; `ExceptDefault` leaves the default culture
unprefixed and redirects `/en/page` to `/page`.

A registered token always names a culture, so no page route may reuse one, and a URL naming a culture
in two consecutive segments such as `/he/en/page` is answered with `400 Bad Request`.

## Contributing

```pwsh
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
npm test --prefix tests/Forma.Blazor.Tests.Scripts
dotnet run --project src/Forma.Blazor.Showcase
```

All four checks must pass before a change is offered for review. The Showcase serves an interactive
page per component, in English, Hebrew, and Spanish — every state a component can enter is rendered
there, and reviewing a component means looking at it in all three cultures. Its localization page
also shows French, which the library never shipped: the Showcase's `locales` folder supplies a few
components' strings in it, to show a host adding a language, and a link opens the page in French
with everything else falling back to English.

Where to read before working:

- [`CLAUDE.md`](CLAUDE.md) — the working rules: structure, dependencies, verification, and the
  working agreement. Its `.claude/skills/` guides carry the detailed standards for authoring,
  naming, styling, scripting, testing, localization, accessibility, verification and prose.
- [`docs/repository-structure.md`](docs/repository-structure.md) — the repository itself: the
  solution structure, where components, scripts, stylesheets and tests live, the build commands,
  where the components' designs derive from, and who consumes the library.
  [`docs/repository-vocabulary.md`](docs/repository-vocabulary.md) defines the words the library
  uses for its concepts.
- [`docs/component-catalog.md`](docs/component-catalog.md) — the components that exist;
  [`docs/component-roadmap.md`](docs/component-roadmap.md) — the components still to build and their order. Work goes one
  component at a time, delivered whole: component, tests, and Showcase page.
- [`docs/`](docs/) — a design document and an implementation plan for each area large enough to
  need them; [`docs/composer-design.md`](docs/composer-design.md) is authoritative for
  `FormaComposer`.
- Per-project READMEs — the localization projects and the artwork packs each document their own
  contract, format, or artwork pipeline.

## License

Forma is MIT licensed — see [`LICENSE.md`](LICENSE.md) — with one exception: `src/Forma.Blazor.Pro/`,
the `FormaExplorer` and `FormaComposer` components, is licensed separately. See
[`COMMERCIAL.md`](COMMERCIAL.md) for what that covers and how to obtain a commercial agreement.
