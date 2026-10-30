# Forma Composer Implementation Plan

The plan for building the composer that [the composer design](composer-design.md) states: the survey it rests on, the phases it lands through, their state, and where the build departed from the design. Section numbers refer to the design.

## What the legacy application shipped

Two surfaces, not one — and the second is the larger of them.

**The rich text editor.** CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — plus a collapsed arrangement under 600px. `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, `contentsLangDirection = rtl`, `allowedContent = true` (no filtering at all). Custom plugins: `imagelibrary` (opened `pops/images.aspx?type=editor`), `youtube` (iframe embed dialog with width/height/autoplay/loop), `lineheight` (pixel line-heights, `1px`–`72px`), `qform` (injected `<script src="/c.js">`), plus bundled `smiley`, `specialchar`, `colordialog`, `table`, `tabletools`, `pastefromword`, `link`, `image`, `a11yhelp`.

**The page designer.** `admin/pops/template.aspx` over `Controls/Edit/` — a drag-and-drop page builder, and the surface the legacy application actually used to compose a page. A palette down the side, a live canvas in the middle, and per-element chrome (`.iHndl`) carrying delete, style and edit affordances. Reordering is drag-and-drop through jquery-sortable (`drag_src.js`), with column resizing (`InitColsResize`), element resizing (`InitResize`) and a context menu (`InitContext`). `elm.js` holds a settings editor per element kind.

The palette: text, image, Facebook, contact form, table/cells, horizontal rule, button, image slider, add-ons, image gallery, YouTube/video, map, spacer, code/script. On top of those sit **host-domain token elements** the palette swaps in per target — product name, summary, images, price, details, body, options, buy button, quantity; the mini-product tile variants; page name, summary, image and read-more for a category tile.

One entry point, `OpenTemplate(type, title)`, serves every target through a numeric type: `100`/`112`/`113` product page templates, `101` product tile, `102` heading template, `110` rich content, `120` popup, `130` custom dropdown menu.

Two details matter more than the rest:

- **It persists HTML plus a scoped `<style>` block.** `SavePage()` posts `body`, `header`, `side` and `footer` through `HandleHtml`, which strips the editor chrome, swaps the menus for `{TOPMENU}`/`{SIDEMENU}` tokens and marks images lazy. There is no structured document — the DOM *is* the document.
- **A text element hosts the rich text editor inline.** `ElmText` calls `CKEDITOR.inline(…)` on the element's own `.elIn`. The designer is the outer surface and the rich text editor is one element inside it.

> On naming: the legacy application has no surface called "Website". `toolbar_Website` is the CKEditor preset, and `Website` is a C# class whose `Theme` the designer reads. The designer itself is *template* (תבנית) throughout.

## Phases

Each phase ends buildable, tested and reviewable.

1. **Capability model.** ✅ The composer assembly, the contracts of §3 and §5–§7, the default sanitizer, the converter registry and history. Departure: `JSComposerAssetLoader`, the asset loader's implementation, ships in the composer's own package, since it needs the JS environment.
2. **Engine shell.** ✅ `FormaComposer` and its toolbar, the document binding of §3.7, availability probing, the selection bridge, `ComposerText`, the debounced sync of §3.6, the command spine, the key map and the toolbar rows of §3.8. Departures: the editor switcher is a tab strip above each surface's own toolbar rather than a segmented control inside it; `PlainTextEditorPlugin`, over a `<textarea>`, is the editor the catalog falls back to, which C3 had ruled out and now records.
2a. **Regions.** ✅ The status strip of §3.8 and the gutter's markers, drawn beside their blocks on the rich text canvas. The plain text editor draws no gutter; line numbers wait for the code editor of phase 4.
3. **Rich text editor and core formatting.** ✅ `RichTextEditorPlugin`, the canvas, the selection bar, the preview and source views, and the formatting, typography, paragraph, list and alignment plugins.
4. **Markdown and code editors.** Open. `MarkdownEditorPlugin`, `CodeEditorPlugin`, `Forma.AddOns.Composer.Markdown`, the lossless-only HTML → Markdown guard, and the embedded Markdown and code blocks.
5. **Media, tables and embeds.** ✅ The link, image, explorer, video, table, horizontal rule, emoji, special character, clipboard, template and embed plugins, with their dialogs. Departures: stored media is its own `ExplorerPlugin` in `Forma.AddOns.Composer.Plugins.Explorer` (§9), beside an `ImagePlugin` that takes an address, alt text and a caption; `VideoPlugin` stores a placeholder rather than an embed, since the sanitizer discards `iframe`; `FileUploadPlugin`, which the design had not named, attaches files beside the document (§4.3a); and maximize, the selection bar and the shortcut reference are engine commands (§4.3a) rather than rich text plugins.
6. **Satellite capabilities.** Open. `Forma.AddOns.Composer.TextMate`, `.KaTeX`, a Ganss.Xss sanitizer satellite for hosts wanting a hardened implementation, and the highlighting and math plugins that consume them.
7. **Page designer.** Open. `PageDesignerPlugin`, the palette, canvas, per-widget chrome and settings panel, `IComposerDesignerPlugin` with the built-in palette, `ComposerWidgetTree` with its renderer, pointer-based drag with a keyboard equivalent, and the text widget hosting `FormaRichTextEditor`. It lands last, since it composes everything above it. The rest of the designer's vocabulary, `ComposerWidget`, `ComposerWidgetNode` and `ComposerDocumentFormat.WidgetTree`, lands with it.
8. **Accessibility, touch, direction, tests and Showcase.** ✅ bUnit tests mirror the source tree, the Showcase page renders the composer in every state it can enter, and §8 holds in a real browser in `he` and `es-419`.
