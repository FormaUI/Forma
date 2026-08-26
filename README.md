# Forma.UI.Blazor

A reusable Blazor component library — modals, toasts, pickers, tabs, data grids, and form inputs.
Localized, RTL-ready, and JS-free apart from what the DOM cannot do without script.

**.NET 11 · Interactive Server components.**

## Install

```pwsh
dotnet add package 2Site.Forma.UI.Blazor
```

The host owns its own `App.razor` and `Routes.razor`; the package ships components, styles, and JS.
`src/Forma.UI.Blazor.Showcase` is a complete working host to copy from.

## Quick start

**1. Register services:**

```csharp
using Forma.Framework.Localization.Json;
using Forma.UI.Blazor;

builder.Services.AddJsonLocalization();
builder.Services.AddFormaUI();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

`AddFormaUI()` registers everything the components need to work — toasts, confirmation dialogs, and the
sanitizer the composer holds content to. Each lands only if nothing is registered for it already, so
registering your own implementation before that call replaces it and leaves the rest untouched.

**2. Render Forma's document head from your host's `App.razor`:**

```razor
<head>
    <FormaPageHead Title="My App">
        <link rel="stylesheet" href="@Assets["app.css"]" />
        <link rel="stylesheet" href="@Assets["MyApp.styles.css"]" />
    </FormaPageHead>
</head>
```

It declares the character set, viewport, title and base address, links Forma's stylesheets in cascade
order, and closes with the framework's own `ResourcePreloader`, `ImportMap` and `HeadOutlet`
— so a host adds none of those itself. A host's own head content goes in the child content, where
its styles land after Forma's and win. `HeadOutlet` renders last, so a page's `PageTitle` still
overrides the title set here.

Set `PreloadAssets="false"` to leave out the runtime preloading, which only a WebAssembly render mode
benefits from.

The layers, for a host that would rather link them itself:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only, no selectors | Always — components read these |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Unless your app already normalizes |
| `forma-base.css` | Body typography and link colours | You want Forma's look; skip it if you have your own |
| `forma-input.css` | The input box every field draws — border, radius, colours, focus ring | Whenever you use any Forma input |

Retheming is one token: override `--main-color` in a stylesheet loaded after Forma's, and the hover,
focus, and link shades follow.

**3. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.UI.Blazor._Imports).Assembly);
```

Components that use JS interop, such as `FormaModal` and `FormaTextBox`, need the Interactive Server
render mode.

## Components

The library covers the admin-app staples — form inputs and pickers, modals and toasts, data
grids, toolbars, tabs, file intake, a file explorer, and the `FormaComposer` authoring surface.
[`COMPONENTS.md`](COMPONENTS.md) is the inventory, describing each component's behaviour.

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

## Localization

Components localize themselves, so `AddJsonLocalization()` is all they need. To localize your own
strings, inject `IStringLocale<T>` and look a string up by its English text:

```razor
@inject IStringLocale<MyPage> L

<p>@L["Save"]</p>
<p>@L[$"Stored {stored} of {total}."]</p>
```

Translations sit beside the type in `MyPage.he.json`, keyed by that same English text. A key with no
translation renders as written, so a page reads correctly before any translation exists.

[`Forma.Framework.Localization.Abstractions`](src/Forma.Framework.Localization.Abstractions/README.md)
documents the contract, the ways to fill a hole, and the plural syntax.
[`Forma.Framework.Localization.Json`](src/Forma.Framework.Localization.Json/README.md) documents the
file format and how a file is found.

## Culture in the URL

Optional, and off unless you ask for it:

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

## Build, test, and run

```pwsh
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet run --project src/Forma.UI.Blazor.Showcase
```

The showcase serves an interactive page per component, in English, Hebrew, and Spanish.

## Icons

`FormaIcon` draws an `Icon` as inline SVG, so a host loads no icon font. An `Icon` pairs a `Glyph`
— named for what it means rather than for the set it is drawn from — with a `GlyphStyle`, either an
outline or a solid shape. A bare glyph converts to an icon, so `Glyph.Save` and
`Icon.Filled(Glyph.Save)` are both accepted wherever an icon is asked for.

Artwork is resolved through `IIconProvider`, so a host can draw its own set instead. With none
registered, `FormaIcon` draws the glyphs the reader's own fonts can depict and nothing at all for
the rest, so a host that wants no artwork package still renders. To draw the full set:

```
dotnet add package 2Site.Forma.UI.Features.Icons.Phosphor
```

```csharp
builder.Services.AddFormaPhosphorPack();
```

The artwork is [Phosphor Icons](https://phosphoricons.com), used under the MIT licence reproduced
in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), which ships inside that package. Regenerate
it with `scripts/fetch-glyphs.ps1`, which reads the glyph-to-Phosphor map in `assets/phosphor.json`
and writes the artwork to `src/Forma.UI.Features.Icons.Phosphor/Assets/glyphs.json`.

## Emoji

`FormaEmoji` draws an emoji through `IEmojiProvider`, and `FormaEmojiPicker` browses the set an
`IEmojiCatalog` describes. With neither registered, `FormaEmoji` falls back to the reader's own
platform font and the picker shows its empty state, because a catalog is data nothing can stand in
for. To draw the bundled artwork and browse the bundled catalog:

```
dotnet add package 2Site.Forma.UI.Features.Emojis.Twemoji
```

```csharp
builder.Services.AddFormaTwemojiPack();
```

The artwork is [Twemoji](https://github.com/jdecked/twemoji), used under CC-BY 4.0 with the
attribution reproduced in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), which ships inside
that package, and the search keywords come from [Unicode CLDR](https://cldr.unicode.org).
Regenerate both with `scripts/fetch-emoji.ps1`.

Contributor conventions live in [`CLAUDE.md`](CLAUDE.md).
