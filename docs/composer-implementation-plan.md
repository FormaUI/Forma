# Forma Composer Implementation Plan

The plan for building the composer that [the composer design](composer-design.md) states: the phases it lands through, their state, and where the build departed from the design. Section numbers refer to the design.

## Phases

Each phase ends buildable, tested and reviewable.

1. **Capability model.** ✅ The composer assembly, the contracts of §3 and §5–§7, the default sanitizer, the converter registry and history. Departure: `JSComposerAssetLoader`, the asset loader's implementation, ships in the composer's own package, since it needs the JS environment.
2. **Engine shell.** ✅ `FormaComposer` and its toolbar, the document binding of §3.7, availability probing, the selection bridge, `ComposerText`, the debounced sync of §3.6, the command spine, the key map and the toolbar rows of §3.8. Departures: the editor switcher is a tab strip above each surface's own toolbar rather than a segmented control inside it; `PlainTextEditorPlugin`, over a `<textarea>`, is the editor the catalog falls back to, which C3 had ruled out and now records.
2a. **Regions.** ✅ The status strip of §3.8 and the gutter's markers, drawn beside their blocks on the rich text canvas. The plain text editor draws no gutter; line numbers wait for the code editor of phase 4.
3. **Rich text editor and core formatting.** ✅ `RichTextEditorPlugin`, the canvas, the selection bar, the preview and source views, and the formatting, typography, paragraph, list and alignment plugins.
4. **Markdown and code editors.** Open. `MarkdownEditorPlugin`, `CodeEditorPlugin`, `Forma.AddOns.Composer.Markdown`, the lossless-only HTML → Markdown guard, and the embedded Markdown and code blocks.
5. **Media, tables and embeds.** ✅ The link, image, explorer, video, table, horizontal rule, emoji, special character, clipboard, template and embed plugins, with their dialogs. Departures: stored media is its own `ExplorerPlugin` in `Forma.AddOns.Composer.Plugins.Explorer` (§9), beside an `ImagePlugin` that takes an address, alt text and a caption; `VideoPlugin` stores a placeholder rather than an embed, since the sanitizer discards `iframe`; `FileUploadPlugin`, which the design had not named, attaches files beside the document (§4.3a); and maximize, the selection bar and the shortcut reference are engine commands (§4.3a) rather than rich text plugins.
6. **Satellite capabilities.** Open. `Forma.AddOns.Composer.TextMate`, `.KaTeX`, a Ganss.Xss sanitizer satellite for hosts wanting a hardened implementation, and the highlighting and math plugins that consume them.
7. **Page designer.** Open. `PageDesignerPlugin`, the palette, canvas, per-widget chrome and settings panel, `IComposerDesignerPlugin` with the built-in palette, `ComposerWidgetTree` with its renderer, pointer-based drag with a keyboard equivalent, and the text widget hosting `FormaRichTextEditor`. It lands last, since it composes everything above it. The designer's vocabulary and `ComposerDocumentFormat.WidgetTree`, public in the design, stay internal until this phase.
8. **Accessibility, touch, direction, tests and Showcase.** ✅ bUnit tests mirror the source tree, the Showcase page renders the composer in every state it can enter, and §8 holds in a real browser in `he` and `es-419`.
