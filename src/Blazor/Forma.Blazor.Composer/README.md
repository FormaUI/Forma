# Forma.Blazor.Composer

`FormaComposer`, a rich text authoring surface for Blazor: an editor with the toolbar, dialogs,
views and attachments an article or a page body needs, assembled from plugins the host picks. It is
a Forma input bound to a `ComposerDocument`, the content together with the format it is written
in, and it holds every HTML document to the library's sanitizer on the way in and on the way out.

**.NET 11 · Blazor components.**

## Install

```pwsh
dotnet add package formaui-net.Blazor.Composer
```

The package depends on [`formaui-net.Blazor`](https://github.com/FormaUI/Forma/blob/main/README.md),
the library it is built on, and on
[`formaui-net.Framework.Composer.Extensibility`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Composer.Extensibility/README.md),
the contracts a plugin is written against.

## Register

```csharp
using Forma.Blazor;
using Forma.Blazor.Components.Composer;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddFormaUIServer();
builder.Services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor.TryAddDefaultFeatures())
    .TryAddDefaultPlugins());
```

`AddFormaUIServer()` registers what the composer needs from the library, the HTML sanitizer among it.
`AddFormaComposer` describes, once, the plugins every composer in the application is built from.

## Use

```razor
@using Forma.Blazor.Components.Composer
@using Forma.Framework.Composer.Documents

<FormaComposer @bind-Value="_body" />

@code {
    private ComposerDocument _body = new(ComposerDocumentFormat.Html, "<p>Draft.</p>");
}
```

`FormaComposer` binds with `@bind-Value`, inside an `EditForm` or on its own. The HTML the host
reads back is what the sanitizer allowed; a host persisting it still sanitizes on the server,
because the endpoint receiving the document cannot trust what the browser sent.

Two presets carry a fixed set of plugins of their own and need no `AddFormaComposer`:

```razor
@using Forma.Blazor.Components.Composer.RichTextEditor

<FormaStandardRichTextEditor @bind-Value="_body" />
<FormaCompactRichTextEditor @bind-Value="_summary" />
```

| Component | What it is |
| --- | --- |
| `FormaComposer` | The composer: the editors it is given, each with its own toolbar, a switcher between editors and views, the attachments list and the status strip. |
| `FormaStandardRichTextEditor` | Rich text with paragraph formats, typography, basic formatting, lists, alignment and direction, links, images, tables and horizontal rules. |
| `FormaCompactRichTextEditor` | Rich text with typography, basic formatting, alignment and direction, and links, for a blurb or a summary. |

All three take `Disabled`, `ReadOnly` and `Options`, and a read-only composer shows no toolbar.
`FormaComposer` also takes `Plugins`, a `ComposerDefinition` of its own in place of the
application's, `ActiveEditorId` with `ActiveEditorIdChanged` to bind the active editor, and
`History` for an undo stack the host holds; `SwitchEditorAsync` switches the editor from code.

`ComposerOptions` sets `ShowToolbar`, `ShowEditorSwitcher`, `DefaultEditorId` for the editor that
opens when the document's format alone does not decide it, `IsResizable` for the grip that drags
the composer taller, `HistoryCapacity` for the undo stack, and `PlainTextUpdateOn` for when text
typed into the plain text editor reaches the composer: as each character is typed, or, set to
`TextUpdate.Change`, as the editor commits its edit, as it loses the focus or before a shortcut runs.

## Choosing the plugins

Every editor and every capability is a plugin, and where a plugin is registered is where it
applies: on the composer it reaches every editor and view, under an editor that editor's surface,
under a view that view's bar. A plugin with an options record is configured where it is
registered.

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

Each composer builds its own instance of every plugin from the description. A composer with no
editor registered uses the plain text editor.

`TryAddDefaultFeatures()` on the rich text editor registers every rich text plugin, attaching,
the selection bar and both views; on the plain text editor it registers attaching.
`TryAddDefaultPlugins()` on the composer registers maximizing and the shortcuts reference. Each
adds only what that scope has not registered itself, so a host changes one plugin's options by
registering it with them beside the defaults.

### Editors

| Plugin | Format | What it authors |
| --- | --- | --- |
| `RichTextEditorPlugin` | `Html` | A visual canvas, hosting the rich text plugins, the selection bar and the two views. |
| `PlainTextEditorPlugin` | `PlainText` | A text area. |

Switching editor is an undoable step.

### Rich text plugins

In `Forma.Blazor.Components.Composer.RichTextEditor.Plugins`, each registered under the rich text
editor.

| Plugin | Contributes | Options |
| --- | --- | --- |
| `BasicFormattingPlugin` | Bold, italic, underline, strikethrough, inline code, clear formatting, and subscript and superscript where its options include them | `BasicFormattingPluginOptions`: `IncludeSubscriptAndSuperscript` |
| `ParagraphFormatPlugin` | Paragraph, headings 1 to 6, blockquote, lead paragraph | |
| `TypographyPlugin` | Font family, font size, line height, text color, highlight color | `TypographyPluginOptions`: the choices offered, and `FontSizeUnit`, `Rem` or `Points` |
| `ListPlugin` | Bulleted and numbered lists, indent, outdent | |
| `AlignmentBidiPlugin` | Alignment and paragraph direction | |
| `LinkPlugin` | Links and anchors | |
| `ImagePlugin` | An image by address, with alt text and caption | |
| `VideoPlugin` | A video placeholder carrying the address, title and autoplay | |
| `TablePlugin` | Tables, their rows, columns and header row | `TablePluginOptions`: default and maximum rows and columns, and `IncludeHeader` |
| `HorizontalRulePlugin` | A thematic break | |
| `EmojiPlugin` | An emoji picker | `EmojiPluginOptions`: the picker's `EmojiPickerOptions` |
| `SpecialCharacterPlugin` | A palette of typographic and mathematical symbols | `SpecialCharacterPluginOptions`: the `Characters` offered |
| `ClipboardPlugin` | Paste as plain text, paste from Word, select all | |
| `TemplatePlugin` | Document templates, inserted at the caret or replacing the document | `TemplatePluginOptions`: the `ComposerTemplate` list, and `ReplaceContent` |
| `EmbedPlugin` | An embed placeholder the host resolves | |

Double-clicking a link, image, video or embed reopens its dialog. Pasted markup passes through the
sanitizer as typed content does.

### Plugins for any editor

In `Forma.Blazor.Components.Composer.Plugins`.

| Plugin | Contributes |
| --- | --- |
| `FileUploadPlugin` | Attaching files from the author's device to `ComposerDocument.Attachments`, listed beneath the canvas; `FileUploadPluginOptions.Picker` sets the files accepted. |
| `MaximizePlugin` | A toggle filling the viewport with the composer. |
| `SelectionBarPlugin` | A toggle for the formatting bar that follows a selection. |
| `ShortcutsPlugin` | A reference of every shortcut the surface answers, opened from the toolbar or with Ctrl+/. |

### Views

In `Forma.Blazor.Components.Composer.RichTextEditor.Views`, each registered under the rich text
editor and reached from the switcher.

| Plugin | Shows |
| --- | --- |
| `RichTextPreviewViewPlugin` | The document as a reader sees it, at the width on offer, on a paper size or at a device width, turned to landscape or zoomed; printed, or exported as an HTML file. |
| `RichTextSourceViewPlugin` | The document's HTML one block per numbered line; copied, or downloaded. |

## What a host supplies

Each registration is optional and enables one thing.

| Registration | What it enables |
| --- | --- |
| `IFileStore` (`Forma.Framework.Resources`) | Uploading attachments through `FileUploadPlugin`, into the store. |
| `IComposerDocumentConverter` (`Forma.Framework.Composer.Documents`) | Switching between editors whose formats differ, for the pair of formats it converts. |
| `IHtmlSanitizer` or `IMarkupPolicy` (`Forma.Framework.Sanitization`) | A sanitizer of the host's own, or the library's with rules of the host's own, registered before `AddFormaUIServer()`, and in a WebAssembly client before `AddFormaUIWebAssembly()`. |

```csharp
using Forma.Framework.Resources;

builder.Services.AddScoped<IFileStore, UploadFolderStore>();
```

[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Resources/README.md)
documents the store, and
[`Forma.Framework.Sanitization`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Sanitization/README.md)
the sanitizer and its policy.

## Rendering the document to readers

The library's `forma-prose.css`, which `FormaPageHead` links, styles an authored document:
headings, quotes, code, lists, tables, figures and links. A host renders a document inside an
element carrying the `forma-prose` class, so readers see it as the canvas and the preview showed
it.

The video and embed plugins write placeholders, which a host replaces with the player, form or
widget each stands for:

```html
<div class="forma-composer-video" data-video-url="…" data-video-title="…" data-autoplay="true">…</div>
<div class="forma-composer-embed" data-embed-type="…" data-embed-id="…" data-settings="…">…</div>
```

The video placeholder holds a link to the address, so it reads as a link where it is left as is.

## Localization

The composer's strings localize through the library's localization, with `AddJsonLocalization()` from
`formaui-net.Framework.Localization.Json`.
Each language is a locale pack the application references, such as
`formaui-net.Blazor.Locales.Hebrew` or `formaui-net.Blazor.Locales.Spanish`.

## Extending

A class implementing `IComposerFeaturePlugin` adds a capability, one implementing
`IComposerEditorPlugin` an editor with a canvas of its own, and one implementing
`IComposerViewPlugin` a view; each is registered like the package's own. A format is a value a host
makes with `new ComposerDocumentFormat("…")`, and an `IComposerDocumentConverter` carries a document
between two formats.
[`Forma.Framework.Composer.Extensibility`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Composer.Extensibility/README.md)
documents the contracts, with an example plugin.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
