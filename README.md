# Forma.Blazor

A Blazor component library for .NET 11: form inputs and pickers, dialogs, popovers and toasts, toolbars and menus,
tabs and layout, file pickers, uploaders and viewers, icons, emoji and windows. Every component is localized, lays
itself out in either reading direction, and draws from one set of design tokens a host can retheme.

```razor
<FormaTextBox @bind-Value="Model.Name" Label="Name" />
<FormaButton Variant="ButtonVariant.Primary" OnClick="SaveAsync">Save</FormaButton>
```

**Requirements.** .NET 11, and a host that renders Blazor components:

- a Web App under Interactive Server, WebAssembly or Auto;
- a standalone WebAssembly app;
- a Blazor Hybrid application: WPF, Windows Forms or MAUI on Windows, or MAUI on Android, iOS or Mac Catalyst.

**Availability.** Forma's packages are previews on nuget.org, so every install asks for a pre-release with
`--prerelease`. A preview's contract can change from one release to the next, and
[the changelog](https://github.com/FormaUI/Forma/blob/main/CHANGELOG.md) says how to migrate from each change that
breaks what the release before it allowed.

In this README:

1. [Install](#install)
2. [Set up a server host](#set-up-a-server-host)
3. [Use the components](#use-the-components)
4. [Styling and theming](#styling-and-theming)
5. [Localization](#localization)
6. [Switching the culture](#switching-the-culture)
7. [WebAssembly components](#webassembly-components)
8. [Blazor Hybrid](#blazor-hybrid)
9. [What the registration does](#what-the-registration-does)
10. [More packages](#more-packages)

## Install

```pwsh
dotnet add package formaui-net.Blazor.Server --prerelease
dotnet add package formaui-net.Framework.Localization.Json --prerelease
```

- `formaui-net.Blazor.Server` is the library's server half. It brings the library and the Framework packages it
  builds on.
- `formaui-net.Framework.Localization.Json` is the JSON localization backend the components read their strings
  through.

The library's strings are English. Each other language is a locale pack of its own:

```pwsh
dotnet add package formaui-net.Blazor.Locales.Hebrew --prerelease
dotnet add package formaui-net.Blazor.Locales.Spanish --prerelease
```

## Set up a server host

These steps set up a Web App whose components render under Interactive Server. A Web App that renders components
under WebAssembly or Auto continues with [WebAssembly components](#webassembly-components), and a desktop or mobile
application follows [Blazor Hybrid](#blazor-hybrid) instead.

**1. Register the services** in `Program.cs`:

```csharp
using Forma.Blazor;
using Forma.Framework.Localization.Json;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaServerIntegration();
```

`AddFormaServerIntegration()` registers every service the components use, and what an interactive server circuit needs. A
host can replace any of them, and these calls can come in any order; [what the registration
does](#what-the-registration-does) sets out what is registered and how to configure it.

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

`FormaPageHead`, from the server half, writes the document head and links Forma's stylesheets. Your stylesheets go
in its child content, which renders after Forma's, so your styles win. Setting `dir` from the culture lays every
component out right to left in a right-to-left language.

`Title` is the document's title wherever a page sets none of its own with `PageTitle`. Where `Routes` takes a render
mode, give `FormaPageHead` the same one, so a page's title follows the page once it's interactive:

```razor
<FormaPageHead Title="My App" PageRenderMode="InteractiveServer" />
...
<Routes @rendermode="InteractiveServer" />
```

**3. Map the components** with the Interactive Server render mode:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);
```

[The Showcase](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Showcase) is a complete host that
renders every component in every state.

## Use the components

Components live in `Forma.Blazor.Components.{Category}` namespaces, such as `Forma.Blazor.Components.Buttons` and
`Forma.Blazor.Components.Feedback`. The services and values they take live in `Forma.Framework.*` namespaces, such as
`Forma.Framework.Feedback` and `Forma.Framework.Icons`. Add the ones you use to `_Imports.razor`.
[The component catalog](https://github.com/FormaUI/Forma/blob/main/docs/component-catalog.md) lists every
component, one line each.

### Lay out fields

A field brings no space around itself; the layout that holds it does. A form whose fields stand one above another
gives its container a gap. A row of fields lines up with flex or grid, where every field, and a `FormaSearchBox`
given a `Label`, stands at one height:

```razor
<div style="display: flex; flex-direction: column; gap: var(--space-4)">
    <FormaTextBox @bind-Value="Model.Name" Label="Name" />
    <div style="display: flex; flex-wrap: wrap; align-items: flex-start; gap: var(--space-2)">
        <FormaSearchBox Label="Search" @bind-Value="Query" />
        <FormaSelect TValue="int?" @bind-Value="Model.Status" Options="Statuses" Label="Status" />
    </div>
</div>
```

Aligning a row at its top edge keeps the boxes level when a validation message shows under one of them.

### Forms and long text

**Forma stays within Blazor Server's default message limit, 32 KB**, so a host has no need to raise
`MaximumReceiveMessageSize` for Forma:

- A Forma text field sends the server what the reader changes as they type, rather than its whole text, so a long
  text never outgrows a message.
- Every other value a reader can make large in a Forma component travels in pieces past a message's size: a
  composer's document, a selection across a long list, a page composed for printing.

**A form's submit waits for the form's fields.** A submit `FormaButton`, and a `FormaButton` or `FormaInputScope` set
to `ReadsForm`, as an action previewing the whole form is, waits before it acts until every Forma field in the form
holds what the reader typed, a `FormaComposer` among them. It doesn't act where a field's text didn't arrive, and that
field says so. Any other action in the form acts at once, so a step, a Cancel or a composer's command stays usable
beside a field whose text didn't arrive.

**A form of the host's own waits the same way, with nothing added.** Its submit, by Enter or a plain submit button,
waits for its Forma fields before it validates. A field whose text didn't arrive fails the validation, so
`OnValidSubmit` sees what the reader typed or isn't called:

```razor
<EditForm Model="Model" OnValidSubmit="SaveAsync">
    <FormaTextArea @bind-Value="Model.Notes" Label="Notes" />
    <button type="submit">Save</button>
</EditForm>
```

`ValidateAsync` waits the same way. `SettleEditsAsync` waits alone, for an action of the host's own that reads the
form without validating it.

### Toasts and confirmation dialogs

Toasts and confirmation dialogs are raised through a service and drawn by a component. Place the component once
where it renders interactively: in a layout under global interactivity, or on the page itself:

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

`FormaStatusBadge` draws a status the application defines, so the host registers its statuses as an
`IStatusBadgeCatalog`:

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

`FormaIcon` draws an `Icon`, which is one of:

- a `Glyph`, named for what it means;
- a `Brand` mark;
- a single `Rune`, set in the symbol font.

Each converts to an icon. `Icon.FromGlyph` picks a glyph's `GlyphVariant`, the outline or the solid shape in the
library's own set:

```razor
<FormaIcon Icon="Glyph.Save" />
<FormaIcon Icon="Brand.GitHub" />
<FormaIcon Icon="Icon.FromGlyph(Glyph.Save, GlyphVariant.Alternate)" />
```

An icon is decorative, hidden from assistive technology, so the control or the text beside it names what it shows.
An icon standing alone, as a mark linking to its brand, is named by its `Label`:

```razor
<a href="https://github.com/FormaUI/Forma"><FormaIcon Icon="Brand.GitHub" Label="Forma on GitHub" /></a>
```

The artwork ships inside the library. A host that wants another set registers its own `IIconProvider`. A subtree or
a single icon names a provider through a cascading value or the `Provider` parameter, and the library's artwork
draws whatever that provider declines.

### Emoji

`FormaEmoji` draws an emoji through `IEmojiProvider`, in the reader's platform font by default. `FormaEmojiPicker`
browses the set an `IEmojiCatalog` describes.

The [Twemoji artwork pack](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Emojis.Twemoji/README.md)
supplies both. Register it on each side, the server and a client, beside the product's registrar there, in either
order:

```pwsh
dotnet add package formaui-net.AddOns.Emojis.Twemoji --prerelease
```

```csharp
builder.Services.AddFormaTwemojiPack();
builder.AddFormaServerIntegration();
```

## Styling and theming

`FormaPageHead` links these stylesheets in this order. A host that links them itself takes the ones it needs:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only | Always; every component reads them |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Your app has no reset of its own |
| `forma-base.css` | Body typography and link colors | You want Forma's look for the page |
| `forma-input.css` | The input box every field draws: border, radius, colors, focus ring | You use any Forma input |
| `forma-prose.css` | Headings, quotes, code, lists, tables, figures and links in a composed document | You render `FormaComposer` content |

Each lives under `_content/Forma.Blazor/`.

A theme redefines the gray scale and the six role solids in a stylesheet loaded after Forma's. Every other token
follows from them, hover, focus, links, backdrops and borders among them:

```css
:root {
    --primary-color: #6f42c1;
    --gray-50: #fafaf9;
    --gray-800: #292524;
}
```

A component's own hooks are named `--forma-{component}-{part}-{property}` and fall back to the theme's tokens, so a host
sets one where it differs. A page with a sticky header moves the toasts below it:

```css
:root {
    --forma-toast-container-inset-block-start: 4.5rem;
    --forma-toast-container-inset-inline-end: 1rem;
}
```

## Localization

Components localize themselves in the culture in force. With a locale pack installed and `AddJsonLocalization()`
registered, a Hebrew request renders Hebrew. The application references the locale pack, and its build records the
pack for `AddJsonLocalization()` to find.

### Your own strings

Inject `IStringLocale<T>` from `Forma.Framework.Localization` and look a string up by its English text, passing
the values for its holes in order:

```razor
@inject IStringLocale<Orders> L

<p>@L["Save"]</p>
<p>@L["Stored {Stored} of {Total}.", stored, total]</p>
```

A key with no localization renders as written.

### Where locale files live

The application's own strings live beside the components they belong to, as a library's do: a `.locales` folder in
each folder, with a file per culture holding a section per type.

The application's root `.locales` also holds a folder for each assembly whose strings it rewords, or adds a language
to that has no pack. The first folder is the assembly's name, and the folders below it are the namespace past that
assembly's root namespace:

```text
Components/Pages/.locales/he.json                { "Orders": { "Save": "שמירה" } }
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

The build embeds every file carrying that entry, so a project names none of them:

- The application's own strings go into its assembly, as a library's do.
- Its files for other assemblies are embedded apart from them, and every runtime reads them, a WebAssembly client's
  and a packaged Blazor Hybrid application's among them.
- A `.locales` folder beside the running application, which no build fills, is read last, so a string can change
  there without building again.

A host's file overrides the library's and the pack's for the keys it holds.
[`Forma.Framework.Localization`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization/README.md)
documents the lookup, the plural syntax and every place a file may sit, and
[`Forma.Framework.Localization.Json`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization.Json/README.md)
the file format.

## Switching the culture

### In place

A host that changes the culture without a new request requests it through the `ICultureSwitch` every user scope
resolves. The switch applies it to the scope's `ICultureState`, and every component renders again in it:

```razor
@using System.Globalization
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Culture
@inject ICultureSwitch CultureSwitch

<FormaButton OnClick="@(() => CultureSwitch.SwitchAsync(CultureInfo.GetCultureInfo("he")))">עברית</FormaButton>
```

Text typed into a number or time field and not yet committed keeps the culture it was typed in. `1,234` typed in
English still commits as a thousand and more after a switch to German.

A host's own components render again in the new culture by subscribing to the same `ICultureState`. They read their
strings through a locale bound to it, `factory.Create(typeof(Orders), cultureState)` from the injected
`IStringLocaleFactory`, rather than through `IStringLocale<T>`, which reads the ambient culture. A render can run in
a flow that still holds an earlier ambient culture, such as one that began before the switch. A locale bound to the
state looks its strings up in the state's cultures, whatever flow renders.

A culture of the same name with patterns of its own, a 12- or 24-hour clock, is a change like any other.

### Cultures with patterns of their own

An application whose cultures carry patterns of their own registers an `ICultureProvider`. Its `GetCulture(name)`
returns the application's culture for each name, carrying that name, and every culture Forma holds or applies is
the provider's:

- the culture state's;
- a switch's, coming back from the page by name;
- a request's, under culture routing.

On a server, a provider whose cultures differ by user is registered scoped. A WebAssembly client registers it as a
singleton, since its runtime is one user.

ASP.NET Core's request localization takes a request's cultures from lists fixed at startup. A host choosing them
that way runs each request in the provider's cultures with `UseFormaRequestCulture()` after it, as
[`Forma.Framework.Culture`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture/README.md)
and [`Forma.Framework.Culture.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Blazor.Server/README.md)
set out:

```csharp
using Forma.Framework.Culture;
using Forma.Framework.Culture.Blazor.Server;

builder.Services.AddScoped<ICultureProvider, ClockCultureProvider>();

app.UseRequestLocalization();
app.UseFormaRequestCulture();
```

### Culture in the URL

[`Forma.Framework.Culture.Router`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router/README.md)
takes the page's culture pair from the first URL segment, `/he/orders`, and gives every page one address per route.
A server host adds the request side from
[`Forma.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router.AspNetCore/README.md):

```pwsh
dotnet add package formaui-net.Framework.Culture.Router --prerelease
dotnet add package formaui-net.Framework.Culture.Router.AspNetCore --prerelease
```

```csharp
using Forma.Framework.Culture.Router;
using Forma.Framework.Culture.Router.AspNetCore;

builder.Services.AddFormaCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", UICulture = new("en") },
        new() { Token = "he", UICulture = new("he") },
        new() { Token = "es", UICulture = new("es-419"), Aliases = ["es-419"] }
    ]
});

app.UseFormaCultureRouting();
```

`FormaPageHead` then writes the document's base in the page's route. A switch is a navigation to the pair's
address, which loads it as a new document by default; `Switch = CultureSwitchMode.InPlace` keeps the document and
the user's work instead, as the package's README sets out.

## WebAssembly components

A Web App that renders components under WebAssembly or Auto sets up its client beside its server. Both declare the
same culture arrangement: how the page treats the client's culture.

| Arrangement | A switch in a WebAssembly island changes |
|-------------|------------------------------------------|
| [Coordinated](#coordinated-the-default), the default | Every runtime on the page and the document |
| [Independent](#independent-client-culture) | That runtime alone |

Each arrangement is an [example host](https://github.com/FormaUI/Forma/blob/main/examples/README.md) that the gate
builds from Forma's packages and drives in a browser. `MyApp` below stands for the example's name.

### Coordinated, the default

Every runtime on the page follows the page's culture coordinator. A switch made in a Server island or a WebAssembly
one reaches both and the document, and a prerendered client starts in the culture the server rendered it in.

**The server** adds the WebAssembly render mode and registers as a server host does:

<!-- example: examples/Forma.Examples.Coordinated/Program.cs -->
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaServerIntegration();
```

<!-- example: examples/Forma.Examples.Coordinated/Program.cs -->
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MyApp.Client._Imports).Assembly);
```

Two requirements hold for a coordinated page:

- **The framework's culture handoff stays on.** `AddFormaServerIntegration()` turns on `Components:UseCultureFromServer`,
  the framework's handoff of the server's culture to the client, wherever the configuration names no value. The
  host fails to start where its configuration sets it to `false`.
- **`FormaPageHead` is in the page's head.** It publishes the cultures the server rendered the page in, which the
  page's coordinator starts from.

**The client** installs, in its own project, the library's WebAssembly half, the JSON localization backend and the
locale packs of the languages it shows:

```pwsh
dotnet add package formaui-net.Blazor.WebAssembly --prerelease
dotnet add package formaui-net.Framework.Localization.Json --prerelease
dotnet add package formaui-net.Blazor.Locales.Hebrew --prerelease
dotnet add package formaui-net.Blazor.Locales.Spanish --prerelease
```

It registers its half:

<!-- example: examples/Forma.Examples.Coordinated.Client/Program.cs -->
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddJsonLocalization();
builder.Services.AddFormaWebAssemblyIntegration();

await builder.Build().RunAsync();
```

**The client holds the strings** for both runtimes, and neither project names a locale file:

- The client keeps its own strings beside its components, and both runtimes read them from the client's assembly.
- Its root `.locales` holds a folder for each assembly whose strings the host rewords. The server reads those from
  the client too, since its build records the client it references.

The example rewords the library's pagination label in Hebrew:

<!-- example: examples/Forma.Examples.Coordinated.Client/.locales/Forma.Blazor/Components/Navigation/he.json -->
```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "FormaPagination": {
    "Pagination": "ניווט בין העמודים"
  }
}
```

**The client loads the globalization data of every culture**, since the default covers only the one it starts in:

<!-- example: examples/Forma.Examples.Coordinated.Client/Forma.Examples.Coordinated.Client.csproj -->
```xml
<PropertyGroup>
    <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
</PropertyGroup>
```

### Independent client culture

The client's culture is its own. A switch in a WebAssembly island changes that runtime alone, never the document,
the coordinator, the address or the cookie, while the server's islands follow the page as before.

Both sides declare the arrangement, and the culture handoff may then be off:

<!-- example: examples/Forma.Examples.Independent/Program.cs -->
```csharp
builder.AddFormaServerIntegration(options => options.CultureArrangement = CultureArrangement.Independent);
```

<!-- example: examples/Forma.Examples.Independent.Client/Program.cs -->
```csharp
builder.Services.AddFormaWebAssemblyIntegration(options => options.CultureArrangement = CultureArrangement.Independent);
```

<!-- example: examples/Forma.Examples.Independent/appsettings.json -->
```json
{
  "Components": {
    "UseCultureFromServer": false
  }
}
```

### Standalone WebAssembly

A standalone WebAssembly app has no server. Its runtime starts the page's coordinator itself, from the culture it
starts in, as
[`Forma.Blazor.WebAssembly`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.WebAssembly/README.md)
sets out.

## Blazor Hybrid

A WPF, Windows Forms or MAUI application on Windows, or a MAUI application on Android, iOS or Mac Catalyst, renders
the components in a `BlazorWebView` in each of its native windows. Every web view shows the one culture the
application holds, unless each is given a culture of its own, as [a culture per web view](#a-culture-per-web-view)
sets out.

Each host below is an [example host](https://github.com/FormaUI/Forma/blob/main/examples/README.md) that the gate
builds from Forma's packages and drives through WebView2. `MyApp` stands for the example's name.

```pwsh
dotnet add package formaui-net.Blazor.WebView --prerelease
dotnet add package formaui-net.Framework.Localization.Json --prerelease
```

**A Windows application targets Windows 10.0.19041 or later**, whose SDK projection WebView2's controls load:

<!-- example: examples/Forma.Examples.Wpf/Forma.Examples.Wpf.csproj -->
```xml
<TargetFramework>net11.0-windows10.0.19041.0</TargetFramework>
```

**A MAUI project builds for each platform** from one `TargetFramework` per build, each needing its MAUI workload:
`net11.0-android`, `net11.0-ios`, `net11.0-maccatalyst` or `net11.0-windows10.0.19041.0`. Apple's platforms build
on a Mac.

### 1. Register the services once, for the application

Register Forma beside the platform's registrar, in either order, and hand that one provider to every web view, so
they all read one culture state and switch it through one switch.

A WPF application builds the provider as it starts:

<!-- example: examples/Forma.Examples.Wpf/App.xaml.cs -->
```csharp
var services = new ServiceCollection();
services.AddWpfBlazorWebView();
services.AddFormaWebViewIntegration();
services.AddJsonLocalization();
```

A Windows Forms application builds it in its `Main`. `Main` runs on the single-threaded apartment WebView2 needs, so
it is a method marked `[STAThread]` rather than top-level statements:

<!-- example: examples/Forma.Examples.WinForms/Program.cs -->
```csharp
[STAThread]
private static void Main()
{
    ApplicationConfiguration.Initialize();
```

<!-- example: examples/Forma.Examples.WinForms/Program.cs -->
```csharp
var services = new ServiceCollection();
services.AddFormaWebViewIntegration();
services.AddJsonLocalization();
services.AddWindowsFormsBlazorWebView();
```

A MAUI application registers on its builder, whose one provider every native window already shares. It also adds
[`formaui-net.AddOns.Operations.Maui`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Operations.Maui/README.md).
Through it, the platform's share sheet saves a file, takes a document to print and shares an address on Android, iOS
and Mac Catalyst, whose web views do none of the three:

<!-- example: examples/Forma.Examples.Maui/MauiProgram.cs -->
```csharp
builder.Services.AddMauiBlazorWebView();
builder.Services.AddFormaWebViewIntegration();
builder.Services.AddFormaMauiOperations();
builder.Services.AddJsonLocalization();
```

`AddFormaWebViewIntegration()` registers what `AddFormaUIServices()` does, and the culture's half for an application's web views:
one culture state, one switch and one coordinator for the application. The coordinator applies a switch to the
state, to the process's default cultures and to the thread each web view runs its events on, and delivers it to
every web view's document.

- A component, or the application's own code, switches through `ICultureSwitch`.
- Application-wide shortcuts are passed as
  `AddFormaWebViewIntegration(options => options.InputRouting = new InputRouterOptions { ... })`.
- Culture routing registers as in a Web App, before or after, under `CultureSwitchMode.InPlace`, as
  [the culture design](https://github.com/FormaUI/Forma/blob/main/docs/culture-design.md#blazor-hybrid) sets out.

### 2. Write the page

The page is the application's `wwwroot/index.html`, which the web view serves itself. It links Forma's stylesheets
and the application's scoped styles, and loads the web view's script:

<!-- example: examples/Forma.Examples.Wpf/wwwroot/index.html -->
```html
<link rel="stylesheet" href="_content/Forma.Blazor/forma-tokens.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-reset.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-base.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-input.css" />
<link rel="stylesheet" href="MyApp.styles.css" />
```

<!-- example: examples/Forma.Examples.Wpf/wwwroot/index.html -->
```html
<div id="app"></div>
<script src="_framework/blazor.webview.js"></script>
```

A MAUI application's page leaves Blazor's start to MAUI, which starts it once the web view's channel to the
application is in place:

<!-- example: examples/Forma.Examples.Maui/wwwroot/index.html -->
```html
<script src="_framework/blazor.webview.js" autostart="false"></script>
```

### 3. Dispose a WPF web view as its native window closes

WPF disposes no control on its own, and disposing the web view releases its scope and every subscription its
components made. The WebView2 control's own disposal can throw an `InvalidCastException` moments after a keyboard
shortcut, once that scope was released:

<!-- example: examples/Forma.Examples.Wpf/MainWindow.xaml.cs -->
```csharp
private async void OnClosed(object? sender, EventArgs e)
{
    _cultureState.Unsubscribe(OnCultureChangedAsync);

    try
    {
        await WebView.DisposeAsync();
    }
    catch (InvalidCastException)
    {
    }
}
```

Windows Forms disposes a web view with its form, and MAUI with its native window.

### 4. Refresh what the application draws natively

Forma updates every component, every web view's document and the threads the web views run on. The application
refreshes its native user interface, a native window's title among it, as the culture state notifies, on the thread
that owns it:

<!-- example: examples/Forma.Examples.Wpf/MainWindow.xaml.cs -->
```csharp
_cultureState = services.GetRequiredService<ICultureState>();
_locale = services.GetRequiredService<IStringLocale<MainWindow>>();
ShowTitle();
_cultureState.Subscribe(OnCultureChangedAsync);
```

<!-- example: examples/Forma.Examples.Wpf/MainWindow.xaml.cs -->
```csharp
private Task OnCultureChangedAsync()
    => Dispatcher.InvokeAsync(ShowTitle).Task;

private void ShowTitle()
    => Title = _locale.WithCulture(_cultureState.Culture, _cultureState.UICulture)["Forma Example"];
```

A MAUI application on Windows keeps a native window's caption, which the taskbar shows, in the system's ANSI code
page. A character the code page lacks shows there as a question mark, while the title bar MAUI draws shows the title
whole.

### 5. Keep the reader's culture

Keep the culture in the application's settings. Apply it before the first web view opens, since the culture state
starts from the culture in force as it is first resolved:

<!-- example: examples/Forma.Examples.Wpf/CultureSettings.cs -->
```csharp
CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);
CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
```

Save it each time the state notifies:

<!-- example: examples/Forma.Examples.Wpf/CultureSettings.cs -->
```csharp
var cultureState = services.GetRequiredService<ICultureState>();
cultureState.Subscribe(() =>
{
    Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
    File.WriteAllLines(SettingsFile, [cultureState.Culture.Name, cultureState.UICulture.Name]);
```

A MAUI application keeps it in `Preferences` instead. The switch persists nothing itself, and a cookie policy is
rejected, since no request reaches a server to read one.

### A culture per web view

An application whose native windows each show a culture of their own declares it as it registers:

```csharp
services.AddFormaWebViewIntegration(options => options.CultureArrangement = CultureArrangement.Independent);
```

Each web view then holds a culture state and a switch of its own. A switch in one web view, or under culture routing
a navigation to another culture's address, changes that web view's components, its document and its address alone.
Under culture routing, a native window opens a web view in a culture by its `StartPath`, `/he/orders`.

The process's and the threads' cultures are shared by every web view, so they stay the application's to set:

- A component of the application's own formats through its web view's `ICultureState`, and reads its strings
  through a locale bound to it, `factory.Create(typeof(Orders), cultureState)`.
- Native code switches one web view through that web view's services, `webView.TryDispatchAsync(services => ...)`.

### The application's own strings

The application's own strings sit beside its types: `.locales/he.json` beside `MainWindow`, and
`Components/.locales/he.json` beside its components. Its root `.locales` folder holds a folder for each assembly
whose strings it rewords. The build embeds them all in the application's assembly, so an application MAUI packages
reads them with no folder beside it.

## What the registration does

`AddFormaServerIntegration()` registers the services the components use, through `AddFormaUIServices()`:

- the toast and confirmation dialog services;
- the status badge catalog;
- the HTML sanitizer;
- the culture state;
- the input router that dispatches keyboard shortcuts.

Beside them it registers what an interactive server circuit needs, which keeps the culture and the document's
language in step with each circuit.

**A host replaces any service by registering its own first.** Each service is registered only where nothing is
registered for it yet.

**The calls can come in any order.** The registration adds no stand-in for what the platform or a package supplies:
logging, the JS runtime, the string locale factory `AddJsonLocalization()` registers, or an artwork pack's
providers.

**Options** are passed to `AddFormaServerIntegration(options => ...)`:

| Option | What it sets | Documented by |
|--------|--------------|---------------|
| `InputRouting` | Application-wide shortcuts, as `new InputRouterOptions { ... }` | [`Forma.Framework.Input`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Input/README.md) |
| `CultureSwitchPolicy` | Whether a culture switch also writes request localization's culture cookie | [`Forma.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Server/README.md) |
| `CultureArrangement` | How the page treats a WebAssembly client's culture | [WebAssembly components](#webassembly-components) |

**`FormaPageHead`** writes the character set, viewport, title, base address and icon, links Forma's stylesheets, and
renders the framework's `ResourcePreloader`, `ImportMap` and `HeadOutlet`:

- Its child content renders after Forma's stylesheets, so the host's styles win.
- The document has one title: the one a page sets with `PageTitle`, or `Title` where the page sets none.
- `PageRenderMode` is the render mode `Routes` takes. The `HeadOutlet` takes it too, so a page's title and
  `HeadContent` follow the page wherever it renders.
- `PreloadAssets="false"` leaves out the `ResourcePreloader`, which serves a WebAssembly render mode.

## More packages

Each package's README covers installing, registering and using it.

| Package | What it adds |
|---------|--------------|
| [`formaui-net.Blazor.DataView`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.DataView/README.md) | `FormaDataView`, the collection component, with grid, list, card and tree views |
| [`formaui-net.Blazor.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Explorer/README.md) | `FormaExplorer`, a file manager over the files a site holds |
| [`formaui-net.Blazor.Composer`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Composer/README.md) | `FormaComposer`, a rich text editor assembled from plugins |
| [`formaui-net.AddOns.Composer.Plugins.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Composer.Plugins.Explorer/README.md) | Inserting a stored file into `FormaComposer` through `FormaExplorer` |
| [`formaui-net.AddOns.Emojis.Twemoji`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Emojis.Twemoji/README.md) | Twemoji artwork and catalog for `FormaEmoji` and `FormaEmojiPicker` |
| [`formaui-net.AddOns.Operations.Maui`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Operations.Maui/README.md) | Saving, printing and sharing through the platform's share sheet in a MAUI application on Android, iOS and Mac Catalyst |
| [`formaui-net.AddOns.Resources.Markdown`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Resources.Markdown/README.md) | Markdown documents rendered as pages in `FormaFileViewer` |
| [`formaui-net.AddOns.Resources.Office`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Resources.Office/README.md) | Word, Excel and PowerPoint documents rendered as pages in `FormaFileViewer` |
| [`formaui-net.AddOns.Share.Targets`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Share.Targets/README.md) | Social networks, messengers and cloud drives for `FormaShareMenu` |
| [`formaui-net.Framework.Currencies`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Currencies/README.md) | Currency codes, their minor units, and the symbol each culture writes them with |
| [`formaui-net.Framework.Culture.Router`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router/README.md) | The culture in the URL |
| [`formaui-net.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Router.AspNetCore/README.md) | The culture in the URL, on an ASP.NET Core server |

## Contributing

Forma is developed at [github.com/FormaUI/Forma](https://github.com/FormaUI/Forma).
[`CLAUDE.md`](https://github.com/FormaUI/Forma/blob/main/CLAUDE.md) sets out the working rules and the verification
gate a change passes, and
[`docs/repository-structure.md`](https://github.com/FormaUI/Forma/blob/main/docs/repository-structure.md) the
solution's layout and its build commands.

## License

Forma is licensed under the [Apache License 2.0](https://github.com/FormaUI/Forma/blob/main/LICENSE).
