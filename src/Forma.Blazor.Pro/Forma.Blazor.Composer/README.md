# Forma.Blazor.Composer

`FormaComposer`, the authoring surface of the **Forma** component library and part of Forma Pro: a
rich text editor with the toolbar, dialogs, views and file handling an article or a page body
needs, assembled from plugins a host picks. It binds a `ComposerDocument` — the content together
with the format it is written in — and holds every HTML document to a markup policy on the way in
and on the way out. Built on `Forma.Blazor` and speaking its namespaces, so a host that already
renders the library adds this package and the same `AddFormaUI()` registers what it needs.

**.NET 11 · Interactive Server components.**

## Install and register

```pwsh
dotnet add package 2Site.Forma.Blazor.Pro.Composer
```

The package depends on `2Site.Forma.Blazor`, the library it is built on, and on
`2Site.Forma.Framework.Composer.Abstractions`, the contracts a plugin is written against; both
restore beside it. The composer's engine and its browser script ship inside the package. Browsing
the host's stored files from the composer is
[`Forma.AddOns.Composer.Plugins.Explorer`](../../Forma.AddOns/Forma.AddOns.Composer.Plugins.Explorer/README.md),
a package of its own, so a host that does not want `FormaExplorer` never downloads it.

```csharp
using Forma.Blazor;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFormaUI();
```

`AddFormaUI()` registers everything the composer needs from the library, including the HTML
sanitizer, each only where the host registered nothing for it. Reach the components with
`@using Forma.Blazor.Components.Composer`, the editor with `.RichTextEditor`, its plugins with
`.RichTextEditor.Plugins`, its views with `.RichTextEditor.Views`, and the plugins that apply to any
editor with `.Plugins`. `ComposerDocument` and its format live in
`Forma.Framework.Composer.Documents`.

Hebrew and Spanish translations ship with the component and resolve through the library's
localization, so a host that calls `AddJsonLocalization()` localizes the composer with the rest.

## What a host supplies

**The document.** `FormaComposer` is a Forma input over `ComposerDocument`, bound with
`@bind-Value` inside an `EditForm` or on its own. A `ComposerDocument` carries its `Format`, its
`Content` and the `Attachments` held beside the content. The formats the package ships an editor
for are `ComposerDocumentFormat.Html`, which the rich text editor authors, and
`ComposerDocumentFormat.PlainText`. A document in HTML is sanitized when it is bound and every time
it changes, so what the host reads back is what the sanitizer allowed; a host persisting the
document still sanitizes on the server, because nothing the browser sends can be trusted by the
endpoint receiving it.

**The plugins.** Every editor and every capability is a plugin. A host describes them once with
`AddFormaComposer`, or hands one composer a description of its own through the `Plugins`
parameter, which takes the place of the application's. Each composer builds its own plugins from
the description, the rich text ones against the rich text editor it built, and with no editor
registered falls back to plain text. [Example](#example) shows a description.

**Stores and providers, all optional.**

| Registration | What it enables |
| --- | --- |
| `IFileStore` (`Forma.Framework.Resources`) | Attaching files from the author's device through `FileUploadPlugin`; without it attaching offers no upload. |
| `IComposerDocumentConverter` (`Forma.Framework.Composer.Documents`) | Switching between editors whose formats differ; a switch with no converter registered for the pair is refused, and so is one whose converter reports what it could not carry across. |
| `IHtmlSanitizer` or `IMarkupPolicy` (`Forma.Framework.Sanitization`) | Replacing the sanitizer, or only its rules, by registering before `AddFormaUI()`; the library's own is the default. |

[`Forma.Framework.Resources`](../../Forma.Framework/Forma.Framework.Resources/README.md) documents
the store and [`Forma.Framework.Sanitization`](../../Forma.Framework/Forma.Framework.Sanitization/README.md)
the sanitizer and its policy.

## Features

### The composer and its presets

| Component | What it is |
| --- | --- |
| `FormaComposer` | The engine: whichever editors are given, each with its own toolbar, an editor switcher when there is more than one, the views the active editor offers, the attachments list and the status strip. |
| `FormaStandardRichTextEditor` | Rich text with paragraph formats, typography, basic formatting, lists, alignment and direction, links, images, tables and horizontal rules. |
| `FormaCompactRichTextEditor` | Rich text with typography, basic formatting, alignment and direction, and links, for a blurb or a ticker. |

All three bind a `ComposerDocument`, take `Disabled`, `ReadOnly` and `Options`, and accept `Id`,
`Class` and `Style`. The two presets are `FormaComposer` with a fixed plugin set and always author
HTML. A read-only composer shows no toolbar.

`ComposerOptions` settles what the host fixes for an instance: `ShowToolbar`,
`ShowEditorSwitcher`, `DefaultEditorId` for the editor that opens when the document's format
alone does not decide it, `IsResizable` for the grip that drags the composer taller, and
`HistoryCapacity` for the undo stack. `ActiveEditorId` with `ActiveEditorIdChanged` binds which
editor is active, and `SwitchEditorAsync` switches it from code.

### Editors

| Plugin | Format | What it authors |
| --- | --- | --- |
| `RichTextEditorPlugin` | `Html` | A visual canvas, host to every plugin below, the selection bar and the two views. |
| `PlainTextEditorPlugin` | `PlainText` | A text area; the editor the composer falls back to when none is given. |

Switching editor is itself an undoable step, so a conversion the author dislikes is one undo away.

### Rich text plugins

Each is registered under the rich text editor, which the composer hands it when it builds the
plugin; the ones with an options record are configured at registration and otherwise take the
record's `Default`.

| Plugin | Contributes | Options |
| --- | --- | --- |
| `BasicFormattingPlugin` | Bold, italic, underline, strikethrough, inline code, subscript, superscript, clear formatting | |
| `ParagraphFormatPlugin` | Paragraph, headings 1–6, blockquote, lead paragraph | |
| `TypographyPlugin` | Font family, font size, line height, text color, highlight color | `TypographyPluginOptions`: the families, sizes, line heights and colors offered, and whether a size is written in `rem` or points |
| `ListPlugin` | Bulleted and numbered lists, indent, outdent | |
| `AlignmentBidiPlugin` | Alignment and paragraph direction | |
| `LinkPlugin` | Links and anchors, added, retargeted and removed; double-clicking one reopens its dialog | |
| `ImagePlugin` | An image by address, with alt text and caption; double-clicking one reopens its dialog | |
| `VideoPlugin` | A video placeholder carrying the address, title and autoplay; double-clicking one reopens its dialog | |
| `TablePlugin` | A table of a chosen size, with rows and columns added and removed, a header row toggled, and the table deleted | `TablePluginOptions`: default and maximum rows and columns, and whether a header row is included |
| `HorizontalRulePlugin` | A thematic break | |
| `EmojiPlugin` | An emoji picker | `EmojiPluginOptions`: the picker's `EmojiPickerOptions` |
| `SpecialCharacterPlugin` | A palette of typographic and mathematical symbols | `SpecialCharacterPluginOptions`: the characters offered |
| `ClipboardPlugin` | Paste as plain text, paste from Word, select all | |
| `TemplatePlugin` | Document templates inserted at the caret or replacing the document | `TemplatePluginOptions`: the `ComposerTemplate` list, and `ReplaceContent` |
| `EmbedPlugin` | A declarative embed placeholder the host resolves; double-clicking one reopens its dialog | |

Pasted markup passes through the same sanitizer as typed content.

The video and embed plugins write placeholders rather than the thing itself, because the
sanitizer discards an `iframe` and a `script`. A video lands as
`<div class="forma-composer-video" data-video-url="…" data-video-title="…" data-autoplay="true">`
around a link to the address, and an embed as
`<div class="forma-composer-embed" data-embed-type="…" data-embed-id="…" data-settings="…">`. A
host rendering the document to readers replaces each with the player, form or widget it stands
for; left alone, the video placeholder still shows its link.

### Plugins for any editor

| Plugin | Contributes |
| --- | --- |
| `MaximizePlugin` | A toggle filling the viewport with the composer. |
| `SelectionBarPlugin` | A toggle showing or hiding the formatting bar that follows a selection; its handle drags the bar to where the author wants it. |
| `FileUploadPlugin` | Attaching files uploaded from the author's device beside the document, in `ComposerDocument.Attachments`; `FileUploadPluginOptions.Picker` is the `FilePickerOptions` deciding which files are accepted. |

Attachments are listed beneath the canvas whichever editor is active, each removable from there.

### Views

A view is a read-only surface over the active editor's document, reached from the trailing tabs
of the editor switcher. The rich text editor offers two, each constructed with the editor:

| Plugin | Shows |
| --- | --- |
| `RichTextPreviewViewPlugin` | The document as a reader will see it, at the width on offer, on an A3, A4, A5, Letter or Legal sheet, or at the width of a phone, tablet or desktop, turned to landscape or zoomed; printed, or exported as an HTML file. |
| `RichTextSourceViewPlugin` | The document's HTML one block per numbered line, wrapped or not; copied to the clipboard, or downloaded. |

### Rendering the document to readers

The look of an authored document — headings, quotes, code, lists, tables, figures, links — lives in
the library's `forma-prose.css`, which `FormaPageHead` links. A host renders a document to its
readers inside an element carrying the `forma-prose` class, so the page reads as the canvas and
the preview did.

### Extending it

A host adds a capability the same way the package's own arrive: a class implementing
`IComposerFeaturePlugin` contributes toolbar items, key bindings and commands wherever the host
registers it, and one implementing `IComposerEditorPlugin` contributes a whole editor with a canvas
of its own. `IComposerDocumentConverter` carries a document between two formats, and a format is a
value a host mints with `new ComposerDocumentFormat("…")`, so a converter and an editor for a
format the package never named take nothing more.

## Example

What every composer in the application is made of is described once, at startup. A composer
authoring an article body in rich text, with pictures, a table, a preview and attachments:

```csharp
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Blazor.Components.Composer.RichTextEditor.Views;

builder.Services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor
        .AddFeaturePlugin<ParagraphFormatPlugin>()
        .AddFeaturePlugin<BasicFormattingPlugin>()
        .AddFeaturePlugin<ListPlugin>()
        .AddFeaturePlugin<LinkPlugin>()
        .AddFeaturePlugin<ImagePlugin>()
        .AddFeaturePlugin<TablePlugin, TablePluginOptions>(options => options.MaxRows = 12)
        .AddFeaturePlugin<FileUploadPlugin>()
        .AddViewPlugin<RichTextPreviewViewPlugin>())
    .AddFeaturePlugin<MaximizePlugin>());
```

Where a plugin is registered is where it applies. One registered on the composer reaches every
editor and every view, which suits maximizing and nothing that edits; one registered on an editor
reaches that editor's surface alone, so attaching a file never lands on its preview.

Registering a plugin names its type rather than an instance, because **every composer builds its
own**: an editor plugin carries the events its canvas subscribes to, so two composers sharing one
would answer each other's commands. A plugin's constructor is satisfied from the plugins that
composer built, the options the registration configured, and then the host's own services.

Each editor has its own defaults. `TryAddDefaultFeatures()` on the rich text editor registers its
formatting, attaching, the selection bar and its views; on the plain text editor it registers
attaching. `TryAddDefaultPlugins()` on the composer registers what belongs on every editor and
every view, which is maximizing. Each adds only what that scope has not registered itself, so a
host wanting everything with one setting changed names only the setting, and none depends on
where it sits in the chain.

The page then places a composer with nothing more to say:

```razor
<FormaComposer @bind-Value="_body" />

@code {
    private ComposerDocument _body = new(ComposerDocumentFormat.Html, "<p>Draft.</p>");
}
```

One composer that differs from the rest takes its own description through `Plugins`, built the
same way with `ComposerDefinition.Create(…)`.

The registration that composer relies on, beside `AddFormaUI()`: the store attached files land in.

```csharp
using Forma.Framework.Resources;

builder.Services.AddScoped<IFileStore, UploadFolderStore>();
```

A short blurb needs none of that:

```razor
<FormaCompactRichTextEditor @bind-Value="_summary" />
```

## License

`FormaComposer` is part of Forma Pro, licensed under the
[Forma Pro Community License](../LICENSE.md) rather than the MIT license the rest of the library
carries. It is free for individuals and for organizations within the license's eligibility, and a
commercial license covers organizations beyond it; [`COMMERCIAL.md`](../COMMERCIAL.md) says what
that covers and how to obtain one.
