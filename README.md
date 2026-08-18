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
using Forma.UI.Features.Feedback;
using Forma.Framework.Localization.Json;

builder.Services.AddJsonLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

**2. Reference the stylesheets from your host `<head>`, in this order:**

```razor
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-tokens.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-reset.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-base.css"]" />
```

Three layers, so a host takes only what it wants:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only, no selectors | Always — components read these |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Unless your app already normalizes |
| `forma-base.css` | Body typography and link colours | You want Forma's look; skip it if you have your own |

Retheming is one token: override `--main-color` in a stylesheet loaded after Forma's, and the hover,
focus, and link shades follow.

**3. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.UI._Imports).Assembly);
```

Components that use JS interop, such as `FormaModal` and `FormaInput`, need the Interactive Server
render mode.

## Components

- **Feedback** — `FormaModal`, `FormaConfirmDialog`, `FormaToastContainer`
  (backed by `IConfirmDialogService` / `IToastService`).
- **Forms** — `FormaDatePicker` (single + range, non-Gregorian aware), `FormaColorPicker`,
  `FormaInput`, `FormaTextArea`, `FormaPasswordInput`, `FormaToggleSwitch`,
  `FormaMultiSelectDropDown`, `FormaSelect`, `FormaButton`, `FormaCheckbox`.
- **Data** — `FormaDataGrid`, `FormaPagination`, `FormaStatusBadge`.
- **Files** — `FormaFilePicker`, `FormaFileUploader`, `FormaFileViewer`.
- **Interactive** — `FormaSortableList`, `FormaDropDownMenu`.
- **Layout** — `FormaPageHeader`, `FormaTabs` (`FormaTab`), `FormaToolbar`, `FormaContextToolbar`.
- **Media** — `FormaVideoPlayer`.

[`COMPONENTS.md`](COMPONENTS.md) describes each one's behaviour.

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

`FormaGlyph` draws the `Glyph` set as inline SVG, so a host loads no icon font. The artwork is
[Font Awesome Free 6](https://fontawesome.com) and is used under
[CC BY 4.0](https://fontawesome.com/license/free); regenerate it with `scripts/fetch-glyphs.ps1`.

Contributor conventions live in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).
