# Forma.Blazor

A Blazor component library for .NET 11: form inputs and pickers,
dialogs, popovers and toasts, toolbars and menus, tabs and layout, file pickers, uploaders and
viewers, icons, emoji and windows. Every component is localized, lays itself out in either reading
direction, and draws from one set of design tokens a host can retheme.

## Install

```pwsh
dotnet add package formaui-net.Blazor.Server
dotnet add package formaui-net.Framework.Localization.Json
```

The first package is the library's server half, which brings the library and the Framework packages it
builds on beside it. The second is the JSON localization backend the components read their strings
through. The library's
strings are English, and each other language is a locale pack of its own:

```pwsh
dotnet add package formaui-net.Blazor.Locales.Hebrew
dotnet add package formaui-net.Blazor.Locales.Spanish
```

## Set up a host

**1. Register the services** in `Program.cs`:

```csharp
using Forma.Blazor;
using Forma.Framework.Localization.Json;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaUIServer();
```

`AddFormaUIServer()` registers the services the components use, through `AddFormaUI()`: the toast
and confirmation dialog services, the status badge catalog, the HTML sanitizer, the icon and emoji
providers, the culture state, and the input router that dispatches keyboard shortcuts. Beside them it
registers what an interactive server circuit needs, which keeps the culture and the document's
language in step with each circuit. Each is registered only where nothing is registered for it yet,
so a host replaces any one of them by registering its own first, and calls `AddFormaUIServer()` after
`AddRazorComponents()` and `AddJsonLocalization()` so theirs are in place. Application-wide shortcuts
are passed as `AddFormaUIServer(options => options.InputRouting = new InputRouterOptions { ... })`,
which
[`Forma.Framework.Input`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Input/README.md)
documents, and `options.CultureSwitchPolicy` chooses whether a culture switch also writes request
localization's culture cookie, as
[`Forma.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Server/README.md)
sets out.

**2. Render the document head** in `App.razor`:

```razor
@using System.Globalization
@using Forma.Blazor.Components.Page

<!DOCTYPE html>
<html lang="@CultureInfo.CurrentUICulture.Name"
      dir="@(CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr")">
<head>
    <FormaPageHead Title="My App" IconHref="@Assets["favicon.svg"]">
        <link rel="stylesheet" href="@Assets["app.css"]" />
        <link rel="stylesheet" href="@Assets["MyApp.styles.css"]" />
    </FormaPageHead>
</head>
<body>
    <Routes />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
</html>
```

`FormaPageHead`, from the server half, writes the character set, viewport, title, base address and icon, links Forma's
stylesheets, and renders the framework's `ResourcePreloader`, `ImportMap` and `HeadOutlet`. Its
child content renders after Forma's stylesheets, so the host's styles win, and a page's
`PageTitle` still sets the title. `PreloadAssets="false"` leaves out the `ResourcePreloader`, which
serves a WebAssembly render mode. Setting `dir` from the culture lays every component out right to
left in a right-to-left language.

**3. Map the components** with the Interactive Server render mode:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);
```

[The Showcase](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Showcase) is a complete
host that renders every component in every state.

## Use the components

Components live in `Forma.Blazor.Components.{Category}` namespaces, such as
`Forma.Blazor.Components.Buttons` and `Forma.Blazor.Components.Feedback`, and the services and
values they take live in `Forma.Framework.*` namespaces, such as `Forma.Framework.Feedback` and
`Forma.Framework.Icons`. Add the ones you use to `_Imports.razor`.
[The component catalog](https://github.com/FormaUI/Forma/blob/main/docs/component-catalog.md)
lists every component, one line each.

A field brings no space around itself; the layout that holds it does. A form whose fields stand one
above another gives its container a gap, and a row of fields lines up with flex or grid, where every
field and a `FormaSearchBox` given a `Label` stand at one height:

```razor
<div style="display: flex; flex-direction: column; gap: var(--space-4)">
    <FormaTextBox @bind-Value="Model.Name" Label="Name" />
    <div style="display: flex; flex-wrap: wrap; align-items: flex-start; gap: var(--space-2)">
        <FormaSearchBox Label="Search" @bind-Value="Query" />
        <FormaSelect TValue="int?" @bind-Value="Model.Status" Options="Statuses" Label="Status" />
    </div>
</div>
```

Aligning a row at its top edge keeps the boxes level when a validation message shows under one of
them.

Toasts and confirmation dialogs are raised through a service and drawn by a component placed once
where it renders interactively, in a layout under global interactivity or on the page itself:

```razor
@page "/articles"
@rendermode InteractiveServer
@inject IConfirmDialogService Confirm
@inject IToastService Toasts

<FormaButton Variant="ButtonVariant.Danger" OnClick="DeleteAsync">Delete</FormaButton>

<FormaConfirmDialog />
<FormaToastContainer />

@code {
    private async Task DeleteAsync()
    {
        if (await Confirm.ConfirmAsync("Delete this article?", "Delete", "Cancel"))
        {
            await Toasts.ShowAsync("Article deleted.");
        }
    }
}
```

### Status badges

`FormaStatusBadge` draws a status the application defines, so the host registers its statuses as
an `IStatusBadgeCatalog`:

```csharp
using Forma.Framework.Css;
using Forma.Framework.Feedback;
using Forma.Framework.Icons;

builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)) { Marker = Glyph.Pen },
    new("published", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430)) { Marker = Glyph.CircleCheck }
]));
```

```razor
<FormaStatusBadge Text="Published" Status="published" />
```

### Icons

`FormaIcon` draws an `Icon`: a `Glyph` named for what it means, a `Brand` mark drawn with the
brand's name as its accessible name, or a single `Rune` set in the symbol font. Each converts to an
icon, and `Icon.FromGlyph` picks a glyph's `GlyphVariant`, the outline or the solid shape in the
library's own set:

```razor
<FormaIcon Icon="Glyph.Save" />
<FormaIcon Icon="Brand.GitHub" />
<FormaIcon Icon="Icon.FromGlyph(Glyph.Save, GlyphVariant.Alternate)" />
```

The artwork ships inside the library. A host that wants another set registers its own
`IIconProvider`, and a subtree or a single icon names one through a cascading value or the
`Provider` parameter; the library's artwork draws whatever that provider declines.

### Emoji

`FormaEmoji` draws an emoji through `IEmojiProvider`, in the reader's platform font by default,
and `FormaEmojiPicker` browses the set an `IEmojiCatalog` describes. The
[Twemoji artwork pack](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/README.md)
supplies both:

```pwsh
dotnet add package formaui-net.AddOns.Emojis.Twemoji
```

```csharp
builder.Services.AddFormaTwemojiPack();
```

## Styling and theming

`FormaPageHead` links these stylesheets in this order; a host that links them itself takes the
ones it needs:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only | Always; every component reads them |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Your app has no reset of its own |
| `forma-base.css` | Body typography and link colors | You want Forma's look for the page |
| `forma-input.css` | The input box every field draws: border, radius, colors, focus ring | You use any Forma input |
| `forma-prose.css` | Headings, quotes, code, lists, tables, figures and links in a composed document | You render `FormaComposer` content |

Each lives under `_content/Forma.Blazor/`. A theme redefines the gray scale and the six role
solids in a stylesheet loaded after Forma's, and every other token, hover, focus, links,
backdrops and borders among them, follows:

```css
:root {
    --primary-color: #6f42c1;
    --gray-50: #fafaf9;
    --gray-800: #292524;
}
```

## Localization

Components localize themselves in the culture in force: with a locale pack installed and
`AddJsonLocalization()` registered, a Hebrew request renders Hebrew. A locale pack is referenced by
the application, which records it at build time for `AddJsonLocalization()` to find.

To localize your own strings, inject `IStringLocale<T>` from `Forma.Framework.Localization` and
look a string up by its English text, passing the values for its holes in order:

```razor
@inject IStringLocale<Orders> L

<p>@L["Save"]</p>
<p>@L["Stored {Stored} of {Total}.", stored, total]</p>
```

A key with no localization renders as written. The localized text lives in a `.locales` folder
beside the application: the first folder is the assembly's name, the folders below it are the
namespace past the assembly's root namespace, and each file is named for its culture with a
section per type. The same layout rewords a string the library ships or adds a language it has no
pack for:

```text
.locales/MyApp/Components/Pages/he.json          { "Orders": { "Save": "שמירה" } }
.locales/Forma.Blazor/Components/Bars/he.json    { "FormaSelectionToolbar": { "Clear Selection": "ניקוי הבחירה" } }
.locales/Forma.Blazor/Components/Bars/fr.json    { "FormaSelectionToolbar": { "Clear Selection": "Effacer la sélection" } }
```

Each file opens with a `$localization` entry naming its culture:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "Orders": {
    "Save": "שמירה",
    "Stored {Stored} of {Total}.": "נשמרו {Stored} מתוך {Total}."
  }
}
```

The folder reaches the output as content:

```xml
<ItemGroup>
    <Content Include=".locales\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

A host that changes the culture without a new request, switching language in place, requests it
through the `ICultureSwitch` every user scope resolves, which applies it to the scope's
`ICultureState`, and every component renders again in it:

```razor
@using System.Globalization
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Culture
@inject ICultureSwitch CultureSwitch

<FormaButton OnClick="@(() => CultureSwitch.SwitchAsync(CultureInfo.GetCultureInfo("he")))">עברית</FormaButton>
```

Text typed into a number or time field and not yet committed keeps the culture it was typed in, so
`1,234` typed in English still commits as a thousand and more after a switch to German. A host's
own components render again in the new culture by subscribing to the same `ICultureState`, and read
their strings through a locale bound to it, `factory.Create(typeof(Orders), cultureState)` from the
injected `IStringLocaleFactory`, rather than `IStringLocale<T>`, which reads the ambient culture: a
render can run in a flow that still holds an earlier ambient culture, such as one that began before
the switch, and a locale bound to the state looks its strings up in the state's cultures whatever flow
renders. A culture of the same name with patterns of its own, a 12- or 24-hour clock, is a change like
any other.

A host's file overrides the library's and the pack's for the keys it holds.
[`Forma.Framework.Localization`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Localization/README.md)
documents the lookup, the plural syntax and every place a file may sit, and
[`Forma.Framework.Localization.Json`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Localization.Json/README.md)
the file format.

## Culture in the URL

[`Forma.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Router.AspNetCore/README.md)
takes the request culture from the first URL segment, `/he/orders`, and gives every page one
address per culture:

```pwsh
dotnet add package formaui-net.Framework.Culture.Router.AspNetCore
```

```csharp
using Forma.Framework.Culture.Router.AspNetCore;

builder.Services.AddFormaCultureRouting(new CultureRouterOptions
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

app.UseFormaCultureRouting();
```

A switch to another culture loads its address as a new document by default;
`Switch = CultureSwitchMode.InPlace` keeps the document and the user's work instead, as the package's
README sets out.

## More packages

Each package's README covers installing, registering and using it.

| Package | What it adds |
|---------|--------------|
| [`formaui-net.Blazor.DataView`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.DataView/README.md) | `FormaDataView`, the collection component, with grid, list, card and tree views |
| [`formaui-net.Blazor.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Explorer/README.md) | `FormaExplorer`, a file manager over the files a site holds |
| [`formaui-net.Blazor.Composer`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Composer/README.md) | `FormaComposer`, a rich text editor assembled from plugins |
| [`formaui-net.AddOns.Composer.Plugins.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Composer.Plugins.Explorer/README.md) | Inserting a stored file into `FormaComposer` through `FormaExplorer` |
| [`formaui-net.AddOns.Emojis.Twemoji`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/README.md) | Twemoji artwork and catalog for `FormaEmoji` and `FormaEmojiPicker` |
| [`formaui-net.AddOns.Resources.Markdown`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Resources.Markdown/README.md) | Markdown documents rendered as pages in `FormaFileViewer` |
| [`formaui-net.AddOns.Resources.Office`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Resources.Office/README.md) | Word, Excel and PowerPoint documents rendered as pages in `FormaFileViewer` |
| [`formaui-net.AddOns.Share.Targets`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Share.Targets/README.md) | Social networks, messengers and cloud drives for `FormaShareMenu` |
| [`formaui-net.Framework.Currencies`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Currencies/README.md) | Currency codes, their minor units, and the symbol each culture writes them with |
| [`formaui-net.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Router.AspNetCore/README.md) | The culture in the URL |

## Contributing

Forma is developed at [github.com/FormaUI/Forma](https://github.com/FormaUI/Forma).
[`CLAUDE.md`](https://github.com/FormaUI/Forma/blob/main/CLAUDE.md) sets out the working rules and
the verification gate a change passes, and
[`docs/repository-structure.md`](https://github.com/FormaUI/Forma/blob/main/docs/repository-structure.md)
the solution's layout and its build commands.

## License

Forma is licensed under the [Apache License 2.0](https://github.com/FormaUI/Forma/blob/main/LICENSE).
