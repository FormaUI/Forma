# Forma Composer Implementation Plan

The plan for building the composer that [the composer design](composer-design.md) states: the file layout it lands in and the phases it lands through. Section numbers refer to the design.

## File layout

The layout as built; an entry marked planned is one the design still calls for and no phase has landed yet.

```text
src/Forma.Framework/Forma.Framework.Sanitization/
├── AllowlistHtmlSanitizer.cs
├── HtmlSubject.cs
├── IHtmlSanitizer.cs
├── IMarkupPolicy.cs
├── RichContentMarkupPolicy.cs             # the markup policy the sanitizer applies
└── ServiceCollectionExtensions.cs

src/Forma.Framework/Forma.Framework.Plugins/              # the registry every composer is described through
├── IPlugin.cs
├── IPluginConfigurable.cs                 # the options record a plugin's constructor takes
├── PluginActivator.cs                     # builds one owner's plugins, resolving each constructor
├── PluginInstance.cs
├── PluginRegistration.cs
├── PluginRegistry.cs                      # what one owner registers, and what sits under each
├── PluginScope.cs                         # one owner's built plugins, and their disposal
└── README.md

src/Forma.Framework/Forma.Framework.Composer.Abstractions/ # the contracts; compiles into …Composer
├── README.md                              # what a plugin is written against
├── Assets/                                # namespace …Composer.Assets
│   ├── ComposerAsset.cs
│   ├── ComposerAssetKind.cs
│   ├── ComposerAssetLoading.cs
│   └── IComposerAssetLoader.cs
├── Dialogs/                               # namespace …Composer.Dialogs
│   ├── ComposerDialogSize.cs
│   ├── ComposerInsertionRequest.cs        # a dialog body a plugin asks the composer to host
│   └── IComposerInsertionDialog.cs
├── Documents/                             # namespace …Composer.Documents
│   ├── ComposerConversionResult.cs
│   ├── ComposerDocument.cs
│   ├── ComposerDocumentFormat.cs
│   └── IComposerDocumentConverter.cs
├── Editing/                               # namespace …Composer.Editing
│   ├── ComposerCommands.cs                # the engine's own command names
│   ├── ComposerContext.cs
│   ├── ComposerInsertion.cs               # what a dialog hands the active editor
│   ├── ComposerKeyBindingCatalog.cs       # the merged key map
│   ├── ComposerRows.cs                    # the rows the composer always provides
│   ├── ComposerSelection.cs
│   ├── ComposerSelectionBar.cs
│   ├── IComposerGutterMarker.cs
│   ├── IComposerHistory.cs
│   ├── IComposerInteraction.cs
│   ├── IComposerRegionRegistry.cs
│   ├── IComposerStatusItem.cs
│   ├── MediaInsertMode.cs
│   └── ResourceInsertion.cs
├── Plugins/                               # namespace …Composer.Plugins
│   ├── ComposerInputTrigger.cs
│   ├── ComposerKeyBinding.cs
│   ├── ComposerToolbarItem.cs
│   ├── ComposerToolbarRow.cs
│   ├── ComposerViews.cs
│   ├── IComposerDesignerPlugin.cs
│   ├── IComposerEditorPlugin.cs
│   ├── IComposerFeaturePlugin.cs
│   ├── IComposerKeyBindingSource.cs
│   ├── IComposerPlugin.cs
│   └── IComposerViewPlugin.cs
├── Syntax/                                # namespace …Composer.Syntax
│   ├── ComposerSyntaxToken.cs
│   └── IComposerSyntaxHighlighter.cs
└── Widgets/                               # namespace …Composer.Widgets
    ├── ComposerWidget.cs
    ├── ComposerWidgetNode.cs
    └── ComposerWidgetTree.cs

src/Forma.Framework/Forma.Framework.Composer/             # the engine, bundled in the composer's package
├── README.md                              # the engine's vocabulary
├── Documents/                             # namespace …Composer.Documents
│   ├── DocumentConverterRegistry.cs
│   └── HtmlSourceFormatter.cs             # one block per line, for the source view
├── Editing/                               # namespace …Composer.Editing
│   ├── ComposerCommandRegistry.cs         # engine commands first, then the plugin that named it
│   ├── ComposerCommandResult.cs
│   ├── ComposerEdit.cs
│   ├── ComposerHistory.cs
│   ├── ComposerKeyBindingSource.cs        # the engine's own bindings
│   ├── ComposerRegionRegistry.cs          # pushed status items and gutter markers
│   ├── ComposerText.cs
│   ├── ComposerTextSpan.cs
│   └── ComposerToolbarLayout.cs           # rows resolved for one editor or one view
├── Plugins/                               # namespace …Composer.Plugins
│   ├── ComposerDefinition.cs              # a composer described once, activated per composer
│   ├── ComposerEditorBuilder.cs           # typed by its editor, so each editor has its own defaults
│   ├── ComposerPluginBuilder.cs
│   ├── ComposerPluginCatalog.cs           # the available plugins, answered by surface
│   └── ComposerViewBuilder.cs
└── Syntax/                                # namespace …Composer.Syntax
    └── NullSyntaxHighlighter.cs

src/Forma.Framework/Forma.Framework.Composer.Interop/      # namespace …Composer.Interop
├── RichTextEditor.cs                      # the wrapper over the rich text script
└── wwwroot/scripts/
    ├── RichTextEditor.js                  # lifecycle, events and command dispatch
    └── RichTextEditor/                    # one concept per module
        ├── BlockFormatter.mjs
        ├── Canvas.mjs
        ├── ContentWriter.mjs
        ├── Fragments.mjs
        ├── InlineFormatter.mjs
        ├── Links.mjs
        ├── ListFormatter.mjs
        ├── Markup.mjs
        ├── MediaMarkup.mjs
        ├── SelectionInspector.mjs
        ├── SelectionMemory.mjs
        └── TableEditor.mjs

src/Forma.Blazor.Pro/Forma.Blazor.Composer/
├── FormaComposer.razor                    # engine shell, editor switcher
├── FormaComposer.razor.cs
├── FormaComposer.razor.css
├── .locales/he.json                       # one file per culture for the category; the project is the category
├── .locales/es.json
├── ComposerChrome.cs                      # maximized and selection-bar state
├── ComposerKeyBindingCatalogExtensions.cs # the key map as input routes
├── ComposerOptions.cs
├── ComposerPluginLocalizer.cs             # a plugin's titles, from its locale then the composer's
├── FormaComposerAttachments.razor         # the files carried beside the document
├── FormaComposerAttachments.razor.cs
├── FormaComposerAttachments.razor.css
├── FormaComposerCommandBar.razor          # one row, opening with undo/redo when first
├── FormaComposerCommandBar.razor.cs
├── FormaComposerPluginsBar.razor          # the plugins row, overflowing into a menu
├── FormaComposerPluginsBar.razor.cs
├── FormaComposerPluginsBar.razor.css
├── FormaComposerStatusStrip.razor         # the status strip region
├── FormaComposerStatusStrip.razor.cs
├── FormaComposerStatusStrip.razor.css
├── FormaComposerSurface.razor             # one editor's, or one view's, toolbar and canvas
├── FormaComposerSurface.razor.cs
├── FormaComposerSurface.razor.css
├── FormaComposerToolbar.razor             # the rows of one surface
├── FormaComposerToolbar.razor.cs
├── README.md                              # the host's view of the composer
├── ServiceCollectionExtensions.cs         # AddFormaComposer, and each editor's defaults
│
├── Assets/                                # namespace …Composer.Assets
│   ├── ComposerAssetLog.cs
│   └── JSComposerAssetLoader.cs           # IComposerAssetLoader over the JS environment
│
├── Dialogs/                               # namespace …Composer.Dialogs — plain-data dialogs, shared by any editor
│   ├── ComposerDialogBase.cs              # open state, dismissal, the markup policy, reopening
│   ├── ComposerDialogResult.cs
│   ├── FormaComposerDialog.razor          # the modal every dialog is built on
│   ├── FormaComposerDialog.razor.cs
│   ├── FormaComposerDialog.razor.css
│   ├── FormaComposerEmbedDialog.razor
│   ├── FormaComposerEmbedDialog.razor.cs
│   ├── FormaComposerFileUploadDialog.razor
│   ├── FormaComposerFileUploadDialog.razor.cs
│   ├── FormaComposerImageDialog.razor
│   ├── FormaComposerImageDialog.razor.cs
│   ├── FormaComposerLinkDialog.razor
│   ├── FormaComposerLinkDialog.razor.cs
│   ├── FormaComposerTableDialog.razor
│   ├── FormaComposerTableDialog.razor.cs
│   ├── FormaComposerVideoDialog.razor
│   ├── FormaComposerVideoDialog.razor.cs
│   ├── EmbedDialogResult.cs
│   ├── ImageDialogResult.cs
│   ├── LinkDialogResult.cs
│   ├── TableDialogResult.cs
│   └── VideoDialogResult.cs
│
├── Plugins/                               # namespace …Composer.Plugins — apply to any editor
│   ├── FileUploadPlugin.cs
│   ├── FileUploadPluginOptions.cs
│   ├── MaximizePlugin.cs
│   └── SelectionBarPlugin.cs
│
├── PlainTextEditor/                       # namespace …Composer.PlainTextEditor
│   ├── PlainTextEditorPlugin.cs           # the fallback editor
│   ├── FormaPlainTextEditor.razor         # textarea canvas
│   ├── FormaPlainTextEditor.razor.cs
│   └── FormaPlainTextEditor.razor.css
│
├── RichTextEditor/                        # namespace …Composer.RichTextEditor
│   ├── RichTextEditorPlugin.cs
│   ├── RichTextDialogState.cs             # which dialog is open, and what it reopened on
│   ├── FormaRichTextEditor.razor          # contenteditable canvas
│   ├── FormaRichTextEditor.razor.cs
│   ├── FormaRichTextEditor.razor.css
│   ├── FormaStandardRichTextEditor.razor  # preset: Coi Basic
│   ├── FormaStandardRichTextEditor.razor.cs
│   ├── FormaCompactRichTextEditor.razor   # preset: Coi Mini
│   ├── FormaCompactRichTextEditor.razor.cs
│   ├── FormaRichTextSelectionBar.razor
│   ├── FormaRichTextSelectionBar.razor.cs
│   ├── FormaRichTextSelectionBar.razor.css
│   ├── Views/                             # namespace …Composer.RichTextEditor.Views — read-only views over the document
│   │   ├── PreviewPage.cs                 # the sizes the preview lays out at
│   │   ├── RichTextPreviewViewPlugin.cs
│   │   ├── FormaRichTextPreview.razor
│   │   ├── FormaRichTextPreview.razor.cs
│   │   ├── FormaRichTextPreview.razor.css
│   │   ├── RichTextSourceViewPlugin.cs
│   │   ├── FormaRichTextSource.razor
│   │   ├── FormaRichTextSource.razor.cs
│   │   └── FormaRichTextSource.razor.css
│   └── Plugins/                           # namespace …Composer.RichTextEditor.Plugins
│       ├── AlignmentBidiPlugin.cs
│       ├── BasicFormattingPlugin.cs
│       ├── ClipboardPlugin.cs
│       ├── CodeBlockPlugin.cs             # planned (§4.3, Phase 4)
│       ├── ComposerFontSizeUnit.cs
│       ├── ComposerTemplate.cs
│       ├── EmbedPlugin.cs
│       ├── EmojiPlugin.cs
│       ├── EmojiPluginOptions.cs
│       ├── FormaComposerEmojiButton.razor # the toolbar trigger opening FormaEmojiPicker
│       ├── FormaComposerEmojiButton.razor.cs
│       ├── FormaComposerEmojiButton.razor.css
│       ├── HorizontalRulePlugin.cs
│       ├── ImagePlugin.cs
│       ├── LinkPlugin.cs
│       ├── ListPlugin.cs
│       ├── MarkdownBlockPlugin.cs         # planned (§4.3, Phase 4)
│       ├── MathPlugin.cs                  # planned (§4.3, Phase 6)
│       ├── ParagraphFormatPlugin.cs
│       ├── SpecialCharacterPlugin.cs
│       ├── SpecialCharacterPluginOptions.cs
│       ├── TablePlugin.cs
│       ├── TablePluginOptions.cs
│       ├── TemplatePlugin.cs
│       ├── TemplatePluginOptions.cs
│       ├── TypographyPlugin.cs
│       ├── TypographyPluginOptions.cs
│       └── VideoPlugin.cs
│
├── MarkdownEditor/                        # planned (§4.1, Phase 4)
│   ├── MarkdownEditorPlugin.cs
│   ├── FormaMarkdownEditor.razor
│   ├── FormaMarkdownEditor.razor.cs
│   └── FormaMarkdownEditor.razor.css
│
├── CodeEditor/                            # planned (§4.1, Phase 4); its script lands in the interop assembly
│   ├── CodeEditorPlugin.cs
│   ├── FormaCodeEditor.razor
│   ├── FormaCodeEditor.razor.cs
│   └── FormaCodeEditor.razor.css
│
└── PageDesigner/                          # planned (§4.2, Phase 7); its script lands in the interop assembly
    ├── PageDesignerPlugin.cs
    ├── FormaPageDesigner.razor             # palette, canvas, settings panel
    ├── FormaPageDesigner.razor.cs
    ├── FormaPageDesigner.razor.css
    ├── FormaWidgetPalette.razor
    ├── FormaWidgetCanvas.razor
    ├── FormaWidgetRenderer.razor           # the canvas walk without the chrome, for public pages
    ├── FormaWidgetFrame.razor              # per-widget chrome: move, edit, style, delete
    ├── FormaWidgetSettingsPanel.razor
    └── Widgets/                            # namespace …Composer.PageDesigner.Widgets
        ├── WidgetPlugin.cs                 # the built-in palette
        ├── FormaTextWidget.razor           # hosts FormaRichTextEditor
        ├── FormaImageWidget.razor
        ├── FormaGalleryWidget.razor
        ├── FormaVideoWidget.razor
        ├── FormaButtonWidget.razor
        ├── FormaRowWidget.razor            # container: columns
        ├── FormaColumnWidget.razor         # container: a drop target with its own width
        ├── FormaSpacerWidget.razor
        ├── FormaDividerWidget.razor
        ├── FormaFormWidget.razor
        ├── FormaMapWidget.razor
        ├── FormaCodeWidget.razor
        └── FormaEmbedWidget.razor

src/Forma.AddOns/Forma.AddOns.Composer.Plugins.Explorer/  # namespace …Composer.Plugins — the composer joined to FormaExplorer
├── ExplorerPlugin.cs
├── ExplorerPluginOptions.cs
├── FormaComposerExplorerDialog.razor      # the host's stored files through FormaExplorer
├── FormaComposerExplorerDialog.razor.cs
├── FormaComposerExplorerDialog.razor.css
├── .locales/he.json                       # the dialog's strings, and the plugin's command
├── .locales/es.json
└── README.md
```

## Phases

Each phase ends buildable, tested and reviewable.

1. **Capability model.** ✅ The composer assembly, every contract in §3 and §5–§7, the default sanitizer, the converter registry, and history are built, the contracts since split into `Forma.Framework.Composer.Abstractions`, and `IComposerAssetLoader` has the JS-backed implementation it was missing. That one is `JSComposerAssetLoader`, in the composer's own package under `Assets/` rather than here, because it needs the JS environment, which places it with Phase 2's engine shell. This lands first precisely because the external-dependency question decides everything after it.
2. **Engine shell.** ✅ `FormaComposer`, `FormaComposerToolbar`, the `ComposerDocument` binding of §3.7, plugin discovery (since retired, a composer now being described by registration, §3.1a) and availability probing, the selection bridge, `ComposerText`, and the debounced sync of §3.6 are built, along with the command spine, the key map, and the toolbar row model of §3.8. The editor switcher is a controlled `FormaTabs` strip above each surface's own toolbar rather than a segmented control inside it. The selection bridge is the shared `TextEntry` operation, not welded into any one editor plugin, so a textarea-based surface reuses it rather than reimplementing it; the rich text canvas has its own, `RichTextEditor` over the script module in `Forma.Framework.Composer.Interop`, because the DOM APIs genuinely differ, but through the same generic `IComposerInteraction.ReportSelectionAsync` seam. `PlainTextEditorPlugin`, over a `<textarea>`, is the editor the catalog falls back to when no registered editor is available, which the design's C3 had ruled out; C3 records the surface that shipped.
2a. **Regions.** ◐ The status strip of §3.8 is built: `IComposerRegionRegistry` pushes `IComposerStatusItem` handles that any plugin creates and disposes, cascaded on `ComposerContext.Regions` so no editor is a special case. `FormaComposerStatusStrip` renders it once per composer, beneath whichever surface is active. The gutter's contract (`IComposerGutterMarker`) is built and tested the same way; its visual rendering waits on a line-oriented surface, since pixel-aligning a marker beside a wrapping `<textarea>` needs font-metric mirroring that would itself become a one-off hack rather than a real capability.
3. **Rich text editor and core formatting.** ✅ `RichTextEditorPlugin`, the canvas, the selection bar, the preview and source views, and the formatting, typography, paragraph, list and alignment/bidi plugins. The canvas's script is `RichTextEditor.js` in `Forma.Framework.Composer.Interop`, one module per editing concept, wrapped by `RichTextEditor`, rather than a module colocated beside the component. Three departures from the design's sketches are recorded there: the list plugin has no task list, the link plugin no auto-linking, and the canvas reports every distinct selection snapshot rather than coalescing keystroke-driven caret movement (§3.6, §4.3).
4. **Markdown and code editors.** `MarkdownEditorPlugin`, `CodeEditorPlugin`, `Forma.AddOns.Composer.Markdown`, the lossless-only HTML → Markdown guard, and the embedded Markdown and code blocks.
5. **Media, tables and embeds.** ✅ Link, image, explorer, video, table, horizontal rule, emoji, special character, clipboard, template and embed plugins, each contributing a toolbar entry and driving the canvas through the rich text surface; the dialogs sit in `Composer/Dialogs/` rather than under the editor, because nothing in them is contenteditable-specific, each built on `FormaComposerDialog` over `ComposerDialogBase` and reopening, on an element the author double-clicks, with what it already holds. Stored media landed as its own `ExplorerPlugin` (since moved to `Forma.AddOns.Composer.Plugins.Explorer`, §9) rather than inside `ImagePlugin`: `ImagePlugin` takes an address, alt text and caption, while `ExplorerPlugin` browses through `FormaComposerExplorerDialog` over an embedded `FormaExplorer`, gated by `ExplorerPluginOptions`, offering no browsing when the host registered no `IExplorerItemsProvider` and no upload when it registered no `IFileStore`. `VideoPlugin` stores a placeholder rather than an embed, because `iframe` is discarded by the sanitizer, and the dialog refuses an address the document could not keep. `FileUploadPlugin`, which the design had not named, attaches uploaded files beside the document as `ComposerDocument.Attachments`, listed by `FormaComposerAttachments` beneath the canvas; it is engine-agnostic, so it sits in `Composer/Plugins/` and its command is the engine's `ComposerCommands.Attach` (§3.7, §4.3a). Maximize and the selection bar landed as **engine** commands on `FormaComposer` (§4.3a) rather than rich text plugins, since both change chrome the engine owns rather than the document any one editor holds.
6. **Satellite capabilities.** `Forma.AddOns.Composer.TextMate`, `.KaTeX`, a Ganss.Xss sanitizer satellite for hosts wanting a hardened implementation, and the highlighting and math plugins that consume them.
7. **Page designer.** `PageDesignerPlugin`, the palette, canvas, per-widget chrome and settings panel, `IComposerDesignerPlugin` with the built-in palette, `ComposerWidgetTree` with its renderer, pointer-based drag with a keyboard equivalent, and the text widget hosting `FormaRichTextEditor`. It lands last because it composes everything above it — a designer without a working rich text editor inside its text widget is not the component Coi had. The designer's vocabulary landed with Phase 1 and is `internal` until this phase, along with `ComposerDocumentFormat.WidgetTree`: the design has it public, and it becomes public here, with the designer that gives a caller something to do with it.
8. **Accessibility, touch, direction, tests and Showcase.** ◐ bUnit tests under `tests/Forma.Blazor.Composer.Tests/` mirror the source tree, and the Showcase page at `/composer` renders the composer in its default, disabled, read-only, toolbar-less and fixed-height states, both presets, the views, the status strip, the plugins bar, stored files and attachments. Still open: §8 verified in a real browser, and `he` and `es-419` checked.
