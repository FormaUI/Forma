# FormaComposer — Design & Implementation Plan

An extensible authoring surface for the **Forma** component library, replacing both halves of Coi's authoring stack: the CKEditor 4 rich text editor and the `template.aspx` drag-and-drop page designer. It hosts rich text, Markdown, code and page composition, and every capability it has — including every editor — arrives as a plugin.

The name is the claim: **the composer composes content, and content can be composed of more content.** A page designer places widgets; one of those widgets is a text widget; that text widget hosts the rich text editor. Coi did exactly this with `CKEDITOR.inline`, and the plugin model makes it fall out rather than be special-cased.

- **Component category:** `Components/Composer/`, namespace `Forma.UI.Blazor.Components.Composer`
- **Root component:** `FormaComposer`
- **Supersedes:** `FormaRichTextEditor` (#36 in [COMPONENTS.md](../COMPONENTS.md)), and Coi's `admin/pops/template.aspx` over `Controls/Edit/`
- **Consumers:** Coi's admin, whose page designer and product templates this replaces wholesale; Épica Deportiva's News article body (P6), page and product editors

## Vocabulary

There is no such thing as a composer *mode*. An editing surface is an **editor plugin** and a formatting tool is a **feature plugin** — the switcher lists editor plugins, not modes. Anything that must work no matter which editor is active belongs to the **engine**, not to a plugin.

The page designer places **widgets**. Deliberately not *element*, which the repository's naming section reserves for raw HTML against *component* for something Forma ships; and deliberately not *block*, which this document already uses for a code block, a Markdown block, a blockquote and a `<style>` block. Widget is a fresh word for a genuinely distinct thing — a palette entry describing something the designer can place — so all four concepts keep one word each.

§1 keeps Coi's own vocabulary when describing Coi: its palette entries were *elements* (`iElm`, `elm.js`, *גרור מכאן אלמנטים*), and that is what someone reading the legacy source will find.

---

## 1. Coi parity and what changes

**Coi is the floor, not the specification.** It establishes what the component must be able to do, because a surface Coi's authors used daily is a surface Forma cannot ship without. It does not establish *how*, and it is not a visual target. Where Coi's software design, code quality or appearance can be bettered — and in a 2014 jQuery admin most of it can — Forma betters it. Every place this document deliberately departs from Coi says so and says why; nothing here is a port.

### 1.1 What Coi shipped

Two surfaces, not one — and the second is the larger of them.

**The rich text editor.** CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — plus a collapsed arrangement under 600px. `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, `contentsLangDirection = rtl`, `allowedContent = true` (no filtering at all). Custom plugins: `imagelibrary` (opened `pops/images.aspx?type=editor`), `youtube` (iframe embed dialog with width/height/autoplay/loop), `lineheight` (pixel line-heights, `1px`–`72px`), `qform` (injected `<script src="/c.js">`), plus bundled `smiley`, `specialchar`, `colordialog`, `table`, `tabletools`, `pastefromword`, `link`, `image`, `a11yhelp`.

**The page designer.** `admin/pops/template.aspx` over `Controls/Edit/` — a drag-and-drop page builder, and the surface Coi actually used to compose a page. A palette down the side, a live canvas in the middle, and per-element chrome (`.iHndl`) carrying delete, style and edit affordances. Reordering is drag-and-drop through jquery-sortable (`drag_src.js`), with column resizing (`InitColsResize`), element resizing (`InitResize`) and a context menu (`InitContext`). `elm.js` holds a settings editor per element kind.

The palette: text, image, Facebook, contact form, table/cells, horizontal rule, button, image slider, add-ons, image gallery, YouTube/video, map, spacer, code/script. On top of those sit **host-domain token elements** the palette swaps in per target — product name, summary, images, price, details, body, options, buy button, quantity; the mini-product tile variants; page name, summary, image and read-more for a category tile.

One entry point, `OpenTemplate(type, title)`, serves every target through a numeric type: `100`/`112`/`113` product page templates, `101` product tile, `102` heading template, `110` rich content, `120` popup, `130` custom dropdown menu.

Two details matter more than the rest:

- **It persists HTML plus a scoped `<style>` block.** `SavePage()` posts `body`, `header`, `side` and `footer` through `HandleHtml`, which strips the editor chrome, swaps the menus for `{TOPMENU}`/`{SIDEMENU}` tokens and marks images lazy. There is no structured document — the DOM *is* the document.
- **A text element hosts the rich text editor inline.** `ElmText` calls `CKEDITOR.inline(…)` on the element's own `.elIn`. The designer is the outer surface and the rich text editor is one element inside it.

That second point is the whole argument for this component's name. **The composer composes content, and one of the things content can be composed of is more content.** An editor plugin must therefore be able to host another editor plugin.

> On naming: Coi has no surface called "Website". `toolbar_Website` is the CKEditor preset, and `Website` is a C# class whose `Theme` the designer reads. The designer itself is *template* (תבנית) throughout.

### 1.2 Full parity, nothing dropped

Every toolbar entry Coi's `Website` preset carried is in scope, mapped to a plugin in §4, and so is the page designer. Coi removed Subscript and Superscript; Forma keeps them available but out of the default preset.

Two Coi behaviours are deliberately **not** reproduced as-authored:

| Coi behaviour | Why not, and what replaces it |
|---|---|
| `allowedContent = true` — no content filtering | An unfiltered editor is a stored-XSS pipe. Replaced by the sanitisation pipeline in §6, which is mandatory and cannot be switched off. |
| `qform` inserting `<script src="/c.js">` | Author-authored `<script>` tags are arbitrary code execution by design. Replaced by `EmbedPlugin`, which inserts a declarative, attribute-only placeholder element the **host** resolves when it renders the document. The author gets the same capability — drop a form into an article — without the editor ever becoming a script injector. |

### 1.3 Modernisations

- **Font size** in `rem`/`em`, not `px`/`pt`: `0.75rem`, `0.875rem`, `1rem`, `1.125rem`, `1.25rem`, `1.5rem`, `2rem`, `2.5rem`, and relative `0.875em`/`1.25em`.
- **Line height** as unitless multipliers: `1.2`, `1.4`, `1.6`, `1.8`, `2.0` — replacing Coi's 72 pixel values.
- **Composition over reimplementation:** `FormaSelect` for the format/font/size pickers, `FormaButton` and `FormaDropDownMenu` for the toolbar and its overflow, `FormaColorPicker` for colour, `FormaExplorer` (#53) for stored media, `FormaFilePicker` (#34) + `FormaFileUploader` (#35) for uploads, `FormaVideoPlayer` (#55) for embeds, `FormaModal` for dialogs.
- **Logical CSS** throughout, and per-block `dir` so bidi is a first-class property of the document rather than a toolbar afterthought.

---

## 2. Assembly layout

The composer's contracts do **not** live in `Forma.UI.Features.Abstractions`. A host that wants the composer should not inherit the toast, confirm-dialog and status-badge contracts along with it, so the composer gets its own pair, following the same convention: the assembly name carries the split, the namespace does not.

| Project | Namespace | References |
|---|---|---|
| `Forma.UI.Features.Composer.Abstractions` | `Forma.UI.Features.Composer` | `Forma.UI.Style` |
| `Forma.UI.Features.Composer` | `Forma.UI.Features.Composer` | `Forma.UI.Features.Composer.Abstractions` |
| `Forma.UI.Features.Composer.Markdown` | `Forma.UI.Features.Composer.Markdown` | `.Abstractions` + Markdig |
| `Forma.UI.Features.Composer.TextMate` | `Forma.UI.Features.Composer.TextMate` | `.Abstractions` + TextMateSharp |
| `Forma.UI.Features.Composer.KaTeX` | `Forma.UI.Features.Composer.KaTeX` | `.Abstractions` + KaTeX browser assets |
| `Forma.UI.Features.Composer.HtmlSanitizer` | `Forma.UI.Features.Composer.HtmlSanitizer` | `.Abstractions` + HtmlSanitizer (Ganss.Xss) |
| `Forma.UI.Blazor` | `Forma.UI.Blazor.Components.Composer` | adds `Forma.UI.Features.Composer.Abstractions` |

**The abstractions project must not take `FrameworkReference Microsoft.AspNetCore.App`.** The repository confines that to `Forma.UI.Blazor` and `Forma.Tests.Support`, so no contract may mention `RenderFragment`, `EventCallback` or `ComponentBase`. Editor plugins therefore surface a `Type` and the composer hosts it in `<DynamicComponent>`; a plugin that needs custom toolbar markup supplies a `Type` too. This is a constraint on the design, not an inconvenience — it keeps every contract testable from a plain class library.

**Every public type in `Forma.UI.Features.Composer.Abstractions` carries the prefix** — `IComposer*` for contracts, `Composer*` for types. The namespace would disambiguate on its own, but a host sees these names in its DI registrations and its `using` list, where `IComposerHtmlSanitizer` beside `Ganss.Xss.IHtmlSanitizer` needs no alias and `ComposerDocumentFormat` cannot be mistaken for a framework type. The prefix is the convention for this assembly rather than a special case for the one collision that prompted it.

`Forma.UI.Blazor` stays the only packable project; each satellite is packable in its own right so a host takes only the capabilities it wants.

---

## 3. The engine

### 3.1 Plugin contracts

```csharp
namespace Forma.UI.Features.Composer;

/// <summary>A capability contributed to the composer, whatever it is implemented in.</summary>
public interface IComposerPlugin
{
    /// <summary>The unique identifier of the plugin.</summary>
    string Id { get; }

    /// <summary>Toolbar items contributed by this plugin.</summary>
    IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; }

    /// <summary>Key bindings contributed by this plugin.</summary>
    IReadOnlyList<ComposerKeyBinding> KeyBindings { get; }

    /// <summary>Typed-input triggers contributed by this plugin.</summary>
    IReadOnlyList<ComposerInputTrigger> InputTriggers { get; }

    /// <summary>Browser assets the plugin needs before it can run.</summary>
    IReadOnlyList<ComposerAsset> Assets { get; }

    /// <summary>Whether every dependency the plugin needs is present.</summary>
    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

/// <summary>Provides an authoring surface such as rich text, Markdown, or code.</summary>
public interface IComposerEditorPlugin : IComposerPlugin
{
    /// <summary>The title of the editor, in English, used as the localization key.</summary>
    string Title { get; }

    /// <summary>The icon associated with the editor.</summary>
    Glyph Icon { get; }

    /// <summary>The document format the editor reads and writes.</summary>
    ComposerDocumentFormat Format { get; }

    /// <summary>The component rendering the editing surface.</summary>
    Type EditorComponent { get; }
}
```

`Title` is the **English source string**, not a resolved translation, so the composer renders it as `L[Title]`. A plugin never resolves a localizer of its own — a singleton that did would capture whichever culture constructed it and show the wrong language to every other reader.

### 3.2 One plugin model for every kind of dependency

A capability may come from .NET, from a browser module, from a native library behind a managed wrapper, or from a service the host wrote. The engine does not care. A plugin declares what it needs and the engine decides whether it can run:

- **Managed dependencies** arrive by constructor injection. If the host never registered `IComposerDocumentConverter` for Markdown, `MarkdownEditorPlugin` is never constructed, is never registered, and its entry never appears in the switcher.
- **Browser dependencies** are declared as `ComposerAsset` values and loaded by the engine's `IComposerAssetLoader`, once per circuit, on demand by default. The loader owns caching, `JSException`/`JSDisconnectedException` handling and disposal, so no plugin writes that boilerplate twice.
- **Availability** is a question the plugin answers through `IsAvailableAsync`. An unavailable plugin degrades to absent — never to a control that throws when pressed.

```csharp
public sealed record ComposerAsset(ComposerAssetKind Kind, string Url, ComposerAssetLoading Loading);

public enum ComposerAssetKind { Module, Stylesheet, Font }

public enum ComposerAssetLoading { Eager, OnDemand }
```

This is what makes KaTeX, TextMate and Markdig the same shape of problem: each is a satellite package registering a service and, where it needs one, declaring an asset. Adding a capability never edits the engine.

### 3.3 What the carve-out permits, and what it does not

The library is JS-free by default and the composer is the one place that rule bends. It bends by category rather than by naming a library, so a capability may bundle an external dependency provided all five hold:

1. It lives in its own `Forma.UI.Features.Composer.{Capability}` satellite, so a host that does not want it never downloads it.
2. It is reached only through a plugin contract — the engine never references it.
3. It stays in a supporting role: it renders, converts, or classifies. The editing surface, the selection, the undo stack and the toolbar remain Forma's, in C#.
4. It ships as plain static assets under `_content/…`, loaded through the plugin's declared `Assets`. No bundler, no build step, no `npm install`.
5. Absent or failing to load, it degrades to absent, never to a control that throws.

Markdig, TextMateSharp and KaTeX each satisfy all five, which is why they are satellites rather than exceptions.

### 3.4 Engine built-ins, shared by every editor

These are not plugins, because a plugin can be absent and these cannot:

| Concern | Contract | Notes |
|---|---|---|
| Undo / redo | `IComposerHistory` | One stack for the whole document, spanning editor switches — switching editor is itself an undoable step, so an author can revert a conversion. |
| Selection | `ComposerSelection` | Snapshot of the caret and range, refreshed from the browser on `selectionchange`. |
| Editing primitives | `ComposerText`, `ComposerEdit` | The operations every editor would otherwise reinvent against raw strings: the word under the caret, the selected text, replacing a range, mapping the lines a selection touches, replacing every occurrence. Pure functions returning a `ComposerEdit` — new content plus where the caret lands. |
| Surface regions | §3.8 | Toolbar rows, the gutter and the status strip, each open to the same three contribution categories. |
| Conversion | `IComposerDocumentConverter` | §5. |
| Sanitisation | `IComposerHtmlSanitizer` | §6. Not optional. |
| Asset loading | `IComposerAssetLoader` | §3.2. |
| Clipboard normalisation | engine | Paste is intercepted, sanitised and normalised before it reaches any plugin. |
| Syntax highlighting | `IComposerSyntaxHighlighter` | §7. |

### 3.5 Shared state

`ComposerContext` is cascaded to every plugin component: the active editor plugin id, the document, the selection snapshot, the engine services above, and a `Func<Task>` to request a re-render. It carries no Blazor types, so it lives in the abstractions project with everything else.

### 3.6 Where the editing surface actually lives

The colocated JS module owns the `contenteditable` surface and the browser selection. C# receives changes **debounced, and on blur** — never per keystroke. Under Interactive Server every keystroke would otherwise be a SignalR round-trip, and an article body over a mobile connection is exactly the case that has to feel local. Commands travel the other way as explicit calls into the module; the document in C# is the source of truth for persistence, the DOM is the source of truth for the caret. The same rule covers the selection report: a click reports immediately, since it is a single low-frequency event, but keystroke-driven caret movement — typing or navigating with arrow keys — is coalesced on the same debounce as the content commit, so holding a key does not turn into one interop round-trip per repeat.

### 3.7 What the host binds

```csharp
/// <summary>Document content together with the format it is written in.</summary>
public sealed record ComposerDocument(ComposerDocumentFormat Format, string Content);
```

```razor
<FormaComposer @bind-Value="_body" />

@code {
    private ComposerDocument _body = new(ComposerDocumentFormat.Html, "");
}
```

The bound value carries its own format because the composer holds more than one kind of content and only one of them is HTML. Binding a bare string would force every document through HTML, which rewrites a Markdown author's source on every round-trip even when §5.2 permits the conversion — reference links return inline, Setext headings return as `###`, hand-wrapped paragraphs return joined.

Switching editor converts `Content` and refuses when §5.2 finds the conversion lossy, so the stored value stays in the format the author last used. The switch is itself an undoable step (§3.4), so a conversion the author dislikes is one undo away.

A host that only ever wants HTML registers the rich text plugin alone and reads `Content`: one property, no conversion, no switcher. `InputBase<TValue>` is generic, so `InputBase<ComposerDocument>` is the conventional base here; its `TryParseValueFromString` reads the serialised form, which is the format id, a newline, then the content. A host is free to persist `Format` and `Content` as two columns instead, which keeps the format queryable.

---

### 3.8 Surface regions

Everything around the canvas is a **region**, and every region takes contributions from the same three categories. The category is not an enum — it falls out of who contributes and which region they name.

| # | Category | Contributor | Default region when it names none |
|---|---|---|---|
| 1 | Composer general | the engine | the built-in toolbar row |
| 2 | Editor specific | `IComposerEditorPlugin` | the built-in toolbar row |
| 3 | External | any other `IComposerPlugin` | the plugin toolbar row |

An editor therefore chooses between joining the shared row and standing on its own purely by setting `ComposerToolbarItem.Row`, and a plugin does the same. Both may declare rows of their own through `ToolbarRows`; declared rows render after the shared row they would otherwise have joined, and a row nothing fills is dropped. An item naming a row nobody declared falls back to its default rather than vanishing.

| Region | Placement | State |
|---|---|---|
| Toolbar rows | above the canvas, stacked in a `FormaCommandBar` | built |
| Gutter | inline-start of the canvas | planned — line numbers plus contributed line markers |
| Status strip | block-end of the canvas | planned — contributed readouts such as Ln/Col, word count, language |

Toolbar contributions are static lists, because a toolbar item's identity does not change. Gutter markers and status readouts are not: they move with the caret and the document. Those are **pushed** — a plugin acquires a handle from the engine, mutates it, and disposes it — rather than pulled by the engine on every render.

That follows VS Code, where `window.createStatusBarItem` returns a `StatusBarItem` whose `text`, `tooltip` and `command` are mutable and which is disposed when done, and where gutter icons are decorations pushed through `setDecorations` against a disposable `TextEditorDecorationType`. Its API reference states the rule plainly: *"The VS Code API uses the dispose pattern for resources that are obtained from VS Code."* The pull shape appears there only for providers such as `NotebookCellStatusBarItemProvider`, which is not the editor status bar.

Being pushed makes a status readout change often, which decides how it is announced. A live region covering the whole strip would speak every caret move, so the strip is not one; a persistent, visually hidden region sits beside it and carries only the items whose `IsAnnounced` is set. A readout such as Ln/Col stays silent and is read on demand, while an event worth interrupting for, such as a save confirmation, opts in. The region is rendered from the composer's first paint rather than with the first item, because a live region inserted already holding its text is not reliably announced.

---

## 4. Plugins

### 4.1 Editor plugins
Each lives in its own directory, mirroring how `Explorer` earned a category of its own.

| Directory | Plugin | Surface |
|---|---|---|
| `RichTextEditor/` | `RichTextEditorPlugin` | Visual `contenteditable` canvas. Hosts the feature plugins, the bubble menu, and embedded Markdown and code blocks. |
| `MarkdownEditor/` | `MarkdownEditorPlugin` | Markdown source with line numbers, highlighting and a live rendered preview. |
| `CodeEditor/` | `CodeEditorPlugin` | Lightweight code editor — line numbers, active-line marker, smart `Tab`, bracket pairing, language picker. Not a Monaco replacement, and not HTML-only: HTML, CSS, JavaScript, TypeScript, C#, SQL, JSON, YAML, Bash, Python. |
| `PageDesigner/` | `PageDesignerPlugin` | Drag-and-drop widget canvas with a palette, per-widget chrome, nesting through rows and columns, and a settings panel per widget. Replaces Coi's `template.aspx`. |

### 4.2 The page designer

The designer is an editor plugin like the other three, so it costs the engine nothing: it appears in the switcher when registered, and a host that only wants a rich text field never sees it.

What it adds is a second extension point, because a palette that only Forma can fill is useless — Coi's palette was half host-domain (product price, buy button, page summary) and Épica's would be too (race results, rider cards, stage profiles).

```csharp
/// <summary>Contributes widgets the designer can place on its canvas.</summary>
public interface IComposerDesignerPlugin : IComposerPlugin
{
    /// <summary>The widgets this plugin contributes to the designer.</summary>
    IReadOnlyList<ComposerWidget> Widgets { get; }
}

/// <summary>Describes a widget available to a designer.</summary>
public sealed record ComposerWidget
{
    /// <summary>The unique identifier of the widget kind.</summary>
    public required string Id { get; init; }

    /// <summary>The widget's name, in English, used as the localization key.</summary>
    public required string Title { get; init; }

    /// <summary>The palette group the widget is listed under.</summary>
    public required string Category { get; init; }

    /// <summary>The icon shown in the palette.</summary>
    public Glyph Icon { get; init; }

    /// <summary>The component rendering the widget on the canvas.</summary>
    public required Type Component { get; init; }

    /// <summary>The component editing the widget's settings, or null when it has none.</summary>
    public Type? SettingsComponent { get; init; }

    /// <summary>The record the widget's settings deserialize to, or null when it has none.</summary>
    public Type? SettingsType { get; init; }

    /// <summary>Whether the widget may contain other widgets.</summary>
    public bool IsContainer { get; init; }
}
```

Built-in widgets cover Coi's generic palette — text, image, gallery, video, button, row, column, spacer, divider, form, map, embed, code. Everything domain-specific is the host's, registered the same way Forma's own are, so Épica adds a race-results widget without forking anything.

#### The saved document is a widget tree

```csharp
/// <summary>The widget tree the designer canvas edits and the renderer draws.</summary>
public sealed record ComposerWidgetTree
{
    /// <summary>The serialization schema version used by this tree.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>The widgets at the root, in order.</summary>
    public IReadOnlyList<ComposerWidgetNode> Widgets { get; init; } = [];
}

/// <summary>One placed widget, its content, its settings and its children.</summary>
public sealed record ComposerWidgetNode
{
    /// <summary>This placement's identity, stable across edits.</summary>
    public required string InstanceId { get; init; }

    /// <summary>The <see cref="ComposerWidget.Id"/> of the kind placed here.</summary>
    public required string WidgetId { get; init; }

    /// <summary>What the author edits on the canvas, or null when the widget has none.</summary>
    public string? Content { get; init; }

    /// <summary>What the settings panel edits.</summary>
    public JsonElement Settings { get; init; }

    /// <summary>Nested widgets, empty unless the widget is a container.</summary>
    public IReadOnlyList<ComposerWidgetNode> Children { get; init; } = [];
}
```

Serialised, that tree is a `ComposerDocument.Content` of format `widget-tree` (§3.7), so the designer needs no special case in the binding.

`Content` and `Settings` are split by **which surface edits them**: content is typed on the canvas — the text widget's rich text, the code widget's source — while settings are what `FormaWidgetSettingsPanel` commits. They also sync differently, content debounced and on blur per §3.6. `Content` is therefore exactly the sanitisation surface of §6; the JSON never is.

The engine never deserialises `Settings`. It hands the `JsonElement` to the widget's `SettingsComponent`, which reads it as its own record, so a host's race-results widget defines `RaceResultsSettings` and Forma never learns the field names.

**Every `Settings` element must be cloned into its node.** A `JsonElement` is a window onto its owning `JsonDocument`'s pooled buffers and is invalid once that document is disposed, so a node holding an unowned element outlives its own data. `JsonElement.Clone()` returns one that owns itself, which is also what keeps a node immutable enough for the undo stack in §3.4 to snapshot the tree.

**A column is a node, not a number on its parent.** A row holds `column` children, each an ordinary container widget with its own settings, rather than a `columns: [50, 50]` array. A column is a drop target with its own children, so it needs a stable identity that "index 0 of the row's children" cannot give it; it needs settings that grow past a width — alignment, padding, background; and per-breakpoint widths are the mobile-first default rather than an afterthought.

```json
{
  "instanceId": "p1",
  "widgetId": "row",
  "children": [
    {
      "instanceId": "p2",
      "widgetId": "column",
      "settings": { "width": { "base": 100, "lg": 50 } },
      "children": [
        { "instanceId": "p3", "widgetId": "product-images", "settings": { "layout": "slider" } }
      ]
    },
    {
      "instanceId": "p4",
      "widgetId": "column",
      "settings": { "width": { "base": 100, "lg": 50 } },
      "children": [
        { "instanceId": "p5", "widgetId": "product-name" },
        { "instanceId": "p6", "widgetId": "product-price", "settings": { "showCurrency": true } },
        { "instanceId": "p7", "widgetId": "buy-button", "settings": { "size": "large" } }
      ]
    }
  ]
}
```

Those token widgets carry no content and barely any settings, because *what* they render is fixed and the value comes from whichever product the page is displaying. One template renders ten thousand products. Coi approximated this with `{TOPMENU}`-style tokens substituted into an HTML string.

**HTML is output, not storage.** `FormaWidgetRenderer` walks the tree emitting a `<DynamicComponent>` per widget — the same walk the canvas performs, minus `FormaWidgetFrame` — so it works under static SSR and needs no `HtmlRenderer`. A host wanting an HTML *string*, for an email, an API or a cache, calls an opt-in service; that is the only path needing `HtmlRenderer`, and it lives in `Forma.UI.Blazor` because the abstractions project may not reference ASP.NET Core.

This is why the tree wins over Coi's saved HTML. Coi froze rendering decisions at save time — `<img src="/media/8842.jpg">`, `style="width:60%"` — so a new image format, a CDN move or a theme change could not reach an already-saved page, and a widget dropped from the palette left stale markup rendering silently instead of a visible "no longer available". Storing `mediaId: 8842` and `raceId: 42` keeps the author's *intent* and decides the markup at render time. Coi's approach also never escaped the tree: recovering a widget's identity and settings from HTML requires `data-widget` and `data-settings` attributes, which is the same JSON in a worse container, read back with an HTML parser the tree does not need.

**Document metadata is the host's, not Forma's.** Identity, author, timestamps, revision, title, tags, publish state, visibility and access all belong on the host's row, which Épica's own conventions already mandate. Keeping them out of the tree avoids two sources of truth, keeps a publishing workflow out of a component library, and — decisively — keeps authorization out of a client-authored blob: the document round-trips through the browser, so anything inside it is attacker-controlled, and an editor that can write its own `Access` value is broken access control. A host that wants metadata travelling alongside wraps the tree in its own record.

One shape considered and rejected: **HTML authoritative for rendering with a sidecar tree for editing.** It appears to offer both and is the worst available option — two persisted representations of one document, free to disagree, with no rule for which wins after a hand-edit.

**A text widget hosts `FormaRichTextEditor`.** That is the recursion Coi had through `CKEDITOR.inline`, and it falls out of the plugin model rather than being special-cased: a widget's `Component` is a component, and a component may be an editor. The engine's selection, history and sanitisation stay single and shared, so an undo inside a text widget and a widget reorder sit on one stack.

**Drag-and-drop follows the rules `FormaSortableList` already established** — Pointer Events rather than mouse events, press-and-hold before a touch drag so an ordinary swipe still scrolls, and a guarded non-passive `touchmove` so the compositor cannot cancel the gesture mid-drag. Reordering must also be possible from the keyboard; Coi's was mouse-only, and that is not a bar Forma clears by matching.

#### Templates, layouts and themes — deferred, and why

Three separate concerns, and Coi did **not** bundle them under one umbrella:

| Concern | What it is | Coi's surface |
|---|---|---|
| **Theme** | The skin — colours, type scale, spacing tokens | `App_Themes/{Name}/` CSS, 28 of them, chosen through `Website.Theme`; plus `design.aspx` for the background, primary and secondary colours, fonts and site width, and `pops/designs.aspx` as a gallery of skins with desktop and mobile screenshots |
| **Layout** | The structural frame — header, side, footer, content region, column grid | **Coi had no separate concept.** `SavePage()` posted `header`, `side`, `footer` and `body` as four independent HTML strings, and swapping a design wiped header and footer while leaving pages intact |
| **Template** | The composed content — which widgets, in what order, with what settings | `pops/template.aspx`, plus `pops/premade.aspx` and `pops/templates.aspx` as pickers over ready-made ones |

So the gap is layout, and it is the one Coi papered over: because there was no layout object, structure and content were the same HTML string, which is why changing a design had to destroy the header and footer rather than re-frame them.

**None of this is in scope for `FormaComposer` yet.** A theme is a set of design tokens, and a layout is a structural frame — both are library-wide concerns that outlive any one editor, and building either *inside* the composer would put them in the wrong place and make them unreusable. `FormaLayout` and `FormaTheme` come first, as components in their own right; the designer then composes them the way it already composes `FormaExplorer` and `FormaVideoPlayer`.

What the designer keeps for itself is the template: the widget tree, its ordering and its settings. `TemplatePlugin` (§4.3) already covers loading a ready-made one, which is Coi's `premade.aspx` without the design-wiping.

#### What the canvas may contain

A widget may host an **editor plugin**. That is how the text widget gets the rich text editor, and it means a Markdown widget and a code widget cost nothing extra — the surfaces already exist, so `WidgetPlugin` offers one widget per registered editor and the engine hosts it exactly as the switcher would.

A widget may **not** host a designer. A canvas inside a canvas either recurses without end or duplicates the palette it already sits in, and neither is a document anyone meant to author.

The rule is a type test rather than a list, so it needs no new API and holds for any designer a host adds later:

```csharp
public sealed class PageDesignerPlugin : IComposerEditorPlugin, IComposerDesignerPlugin;
```

A designer **is** an editor plugin — it is a surface the switcher offers — **and** a designer plugin, because it contributes the built-in palette. So "any editor plugin that is not also an `IComposerDesignerPlugin`" is exactly the placeable set, and a designer excludes itself by its own type.

That also settles a naming worry this document carried earlier: `PageDesignerPlugin` does implement `IComposerDesignerPlugin` after all, so the two interfaces read as the pair they look like.

### 4.3 Feature plugins

| Plugin | Covers | Coi origin |
|---|---|---|
| `BasicFormattingPlugin` | Bold, Italic, Underline, Strike, Inline code, Subscript, Superscript, Remove formatting | Bold/Italic/Underline/Strike, RemoveFormat |
| `TypographyPlugin` | Font family, `rem`/`em` font size, line-height multiplier, text colour, highlight colour | Font, FontSize, TextColor, `lineheight` |
| `ParagraphFormatPlugin` | Paragraph, H1–H6, Blockquote, Lead | Format (`p;h1;h2;h3;h4`) |
| `ListPlugin` | Bulleted, numbered, task list with live checkboxes, indent, outdent | NumberedList, BulletedList |
| `AlignmentBidiPlugin` | Align start/center/end/justify, direction LTR/RTL | Justify*, BidiRtl, BidiLtr |
| `LinkPlugin` | Link, unlink, anchor, auto-linking | Link, Unlink, Anchor |
| `MediaPlugin` | Stored media through `FormaExplorer`; uploads through `FormaFilePicker` + `FormaFileUploader`; alt text, caption, alignment | `imagelibrary`, Image |
| `VideoPlugin` | Embeds rendered by `FormaVideoPlayer` | `youtube` |
| `TablePlugin` | Insert grid, row/column add and remove, header toggle, delete | Table, tabletools |
| `HorizontalRulePlugin` | Thematic break | HorizontalRule |
| `EmojiPlugin` | Emoji picker with search | `smiley` (image sprites → Unicode) |
| `SpecialCharacterPlugin` | Character palette | SpecialChar |
| `ClipboardPlugin` | Paste as plain text, paste-and-clean from Word, select all | Cut, Copy, Paste, PasteText, PasteFromWord, SelectAll |
| `CodeBlockPlugin` | Embedded code block with language picker and highlighting | — |
| `MarkdownBlockPlugin` | Embedded Markdown block, toggling between source and rendered | — |
| `MathPlugin` | Inline `$…$` and block `$$…$$`, rendered by a registered math capability | — |
| `PreviewPlugin` | Read-only rendered preview of the document | Preview |
| `MaximizePlugin` | Full-viewport editing | Maximize |
| `TemplatePlugin` | Host-supplied document templates | Templates |
| `EmbedPlugin` | Declarative host-resolved embeds (forms, widgets) | `qform`, safely |

Clipboard note: browsers do not grant programmatic cut/copy/paste without user activation, and the native shortcuts already work. `ClipboardPlugin` therefore contributes paste *transformation* and the explicit "paste as plain text" and "paste from Word" entries, not reimplementations of `Ctrl+C`.

### 4.4 Plugin options

Options follow the repository's existing convention — a `sealed record` with `init` properties and a `Default`, per plugin, named for it: `TypographyPluginOptions` chooses which sizes and families are offered, `TablePluginOptions` caps dimensions, `MediaPluginOptions` gates upload against library-only. A plugin with a single flag keeps a plain property instead.

### 4.5 Presets are components, not subclasses

Three components differing only in which plugins they register, each with its own options record. None derives from another; they compose the same engine with different plugin sets, so a preset can never drift from the editor it presents.

| Component | Plugin set | Replaces |
|---|---|---|
| `FormaComposer` | Everything registered | Coi `Website` |
| `FormaBasicRichTextEditor` | Formatting, typography, paragraph, lists, alignment/bidi, link, media, table, horizontal rule | Coi `Basic` |
| `FormaLiteRichTextEditor` | Formatting, alignment/bidi, link, font and size only | Coi `Mini` |

The two presets live in `RichTextEditor/` beside the editor they present, not at the category root: each is a rich text surface with a narrower plugin set, so they belong with `FormaRichTextEditor` rather than with the engine. `FormaComposer` stays at the root because it is the engine, and it presents whatever is registered rather than any one editor.

The `Forma` prefix is required on all three: the repository's naming rule puts it on every component tag.

---

## 5. Conversion

### 5.1 One seam, not one per format

```csharp
/// <summary>A document format the composer can hold or convert.</summary>
public sealed record ComposerDocumentFormat
{
    public ComposerDocumentFormat(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.ToLowerInvariant();
    }

    /// <summary>The format's lowercase identifier.</summary>
    public string Id { get; }

    public static ComposerDocumentFormat Html { get; } = new("html");
    public static ComposerDocumentFormat Markdown { get; } = new("markdown");
    public static ComposerDocumentFormat PlainText { get; } = new("text");
    public static ComposerDocumentFormat WidgetTree { get; } = new("widget-tree");
}

/// <summary>Converts document content from one format to another.</summary>
public interface IComposerDocumentConverter
{
    /// <summary>The format the converter reads.</summary>
    ComposerDocumentFormat Source { get; }

    /// <summary>The format the converter writes.</summary>
    ComposerDocumentFormat Target { get; }

    /// <summary>Converts the content, reporting what it could not represent.</summary>
    ComposerConversionResult Convert(string content);
}

/// <summary>The outcome of a conversion, and what the target format could not carry.</summary>
public sealed record ComposerConversionResult
{
    /// <summary>Whether the conversion carried the whole document.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The converted content, or null when the conversion was refused.</summary>
    public string? Content { get; init; }

    /// <summary>What the target format cannot represent, empty on success.</summary>
    public IReadOnlyList<string> UnsupportedFeatures { get; init; } = [];
}
```

`IComposerDocumentConverter` rather than `IComposerMarkdownConverter` because Markdown is not the last format the composer will meet — LaTeX, plain text and whatever a host adds all fit the same shape, and a registry keyed on `(Source, Target)` resolves them uniformly. The extra cost over a Markdown-specific interface is one record and one lookup.

**`ComposerDocumentFormat` is a string-keyed value, not an enum**, so a host or a satellite mints a format without the library changing: `new ComposerDocumentFormat("latex")`, an editor plugin declaring it, a converter registered for `(latex, html)`, and the switcher offers it. The statics are conveniences for the formats Forma ships a surface for, not the set of legal values.

It is a record **class** rather than a record struct because a struct's `default` would bypass the constructor and carry a null `Id` into the converter registry's keys, where it would compare and hash as a legitimate format and then throw on first read. The statics are cached rather than expression-bodied for the same reason the constructor exists: each one otherwise re-validates and re-lowercases on every access.

The engine therefore never `switch`es on a format. `ComposerAssetKind` (§3.2) is a closed enum because the loader must know how to load each kind, whereas a format the engine only ever hands to a registry lookup needs no such knowledge. Behaviour comes from what is registered, which is what makes "no converter, no editor" (§3.2) fall out rather than have to be enforced.

**`widget-tree` has no converter in either direction, and needs none.** Converting into the designer would mean parsing HTML back into widgets, and converting out would flatten the tree into HTML and discard it — both refused for the reasons §4.2 gives. Neither is a rule the engine enforces: the registry simply holds no entry, so the switcher never offers the conversion. Producing HTML *from* a tree is rendering rather than conversion, and §4.2 says where it lives.

### 5.2 Markdown to HTML always; HTML to Markdown only when it is lossless

Markdown → HTML is total and always available. **HTML → Markdown is offered only when the HTML happens to be within the subset Markdown can represent.** The converter inspects the document first and, when it finds anything Markdown cannot carry, refuses with `Succeeded = false` and names what blocked it.

This replaces the "lossless transitions" claim in the first draft, which was untrue: Markdown has no representation for font family, font size, text colour, highlight, line height, underline, alignment or per-block `dir` — every one of which §1.3 promises as a headline feature. Silently discarding an author's formatting on an editor switch is worse than declining to switch.

The refusal is surfaced, not swallowed: the switcher disables the Markdown editor and explains which features are in the way, so the author can remove them and try again.

`Forma.Framework.Resources.Markdown` is **not** reused for this. It exists to render a stored `.md` resource for viewing, so its pipeline calls `DisableHtml()` and sanitises the output — one-way by construction. `Forma.UI.Features.Composer.Markdown` is a separate converter over the same Markdig dependency.

---

## 6. Security

An editor that accepts HTML, offers a code editor over raw markup, and pastes from Word is a stored-XSS surface. Coi got this wrong with `allowedContent = true`.

```csharp
/// <summary>Removes markup a document may not carry.</summary>
public interface IComposerHtmlSanitizer
{
    /// <summary>Returns the content with disallowed markup removed.</summary>
    string Sanitize(string html);
}
```

- Forma ships a **default allowlist sanitiser** in `Forma.UI.Features.Composer` with no external dependency, covering the element and attribute set the built-in plugins can produce.
- A host may replace it — `Forma.UI.Features.Composer.HtmlSanitizer` wraps Ganss.Xss for hosts wanting a hardened, widely-reviewed implementation, and any registration overrides the default.
- Sanitisation runs at **four** points, and none is optional: content loaded into the composer, content pasted in, content leaving the code editor, and content raised to the host. In a widget tree that means every node's `Content` (§4.2); the settings JSON is never sanitised as markup.
- The composer's guarantee stops at the browser. **The host must sanitise server-side on save regardless**, because a client-side editor cannot be trusted by the endpoint receiving its output.

`IComposerSyntaxHighlighter` returns tokens rather than markup for the same reason — see §7.

---

## 7. Syntax highlighting

```csharp
/// <summary>One classified span of source code.</summary>
public readonly record struct ComposerSyntaxToken(int Start, int Length, string Scope);

/// <summary>Classifies source code into scoped tokens.</summary>
public interface IComposerSyntaxHighlighter
{
    /// <summary>Languages this highlighter classifies.</summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>Classifies the code into tokens, in order.</summary>
    IReadOnlyList<ComposerSyntaxToken> Tokenize(string code, string language);
}
```

Returning **tokens, not HTML**. A highlighter that returned `"<span class=…>"` would put the burden of HTML-encoding the author's code on every implementation, and one that forgot would execute `<script>` inside a code block. With offsets and scopes the component emits the text through normal Razor encoding, so an implementation *cannot* introduce markup.

Scopes map to `.forma-tm-*` classes: `keyword`, `string`, `comment`, `function`, `type`, `variable`, `number`, `operator`, `punctuation`.

**TextMate grammars need an Oniguruma-compatible engine.** Real `.tmLanguage.json` files use `\G`, `\h`, possessive quantifiers, inline `(?i)` scoping, and `end` patterns back-referencing `begin` captures — none of which .NET's `Regex` supports. `Forma.UI.Features.Composer.TextMate` therefore wraps **TextMateSharp**, which ships Oniguruma bindings and a grammar registry, rather than attempting a tokenizer over `System.Text.RegularExpressions`.

Without any highlighter registered, code blocks render as plain monospaced text. Highlighting is an enhancement, never a prerequisite.

---

## 8. Accessibility, touch and direction

These are correctness requirements for every Forma component, and the composer is the hardest case in the library — a dense toolbar plus a `contenteditable` surface.

- **Toolbar semantics:** `role="toolbar"` obliges arrow-key roving focus between its controls. Either implement it or use a plain button group, matching the decision already recorded for `FormaToolbar` and `FormaContextToolbar`. The editor switcher is a `radiogroup`, since it selects one of a set.
- **Editing surface:** the `contenteditable` region is labelled, exposes `aria-multiline`, and announces the active block and formatting through a live region — a screen-reader user must be able to learn that the caret is inside a heading or a code block.
- **Every state without colour:** an active toolbar toggle carries `aria-pressed` and a persistent non-colour mark, not a tint alone.
- **Touch:** every control is at least 24×24 CSS px, and 44×44 under `@media (pointer: coarse)`. Every `:hover` rule sits inside `@media (hover: hover)`. The bubble menu is clamped to the viewport and reachable by tap; nothing essential hangs off hover.
- **Direction:** logical properties only, per-block `dir`, and horizontal arrow keys resolved through `CultureInfo.CurrentCulture.TextInfo.IsRightToLeft`. Bidi is not a plugin's problem to solve twice.

---

## 9. File layout

```text
src/Forma.UI.Features.Composer.Abstractions/
├── Assets/                                # namespace …Composer.Assets
│   ├── ComposerAsset.cs
│   ├── ComposerAssetKind.cs
│   ├── ComposerAssetLoading.cs
│   └── IComposerAssetLoader.cs
├── Documents/                             # namespace …Composer.Documents
│   ├── ComposerDocument.cs
│   ├── ComposerDocumentFormat.cs
│   ├── IComposerDocumentConverter.cs
│   └── ComposerConversionResult.cs
├── Editing/                               # namespace …Composer.Editing
│   ├── ComposerContext.cs
│   ├── ComposerSelection.cs
│   └── IComposerHistory.cs
├── Plugins/                               # namespace …Composer.Plugins
│   ├── IComposerPlugin.cs
│   ├── IComposerEditorPlugin.cs
│   ├── IComposerDesignerPlugin.cs
│   ├── ComposerToolbarItem.cs
│   ├── ComposerKeyBinding.cs
│   └── ComposerInputTrigger.cs
├── Sanitization/                          # namespace …Composer.Sanitization
│   └── IComposerHtmlSanitizer.cs
├── Syntax/                                # namespace …Composer.Syntax
│   ├── IComposerSyntaxHighlighter.cs
│   └── ComposerSyntaxToken.cs
└── Widgets/                               # namespace …Composer.Widgets
    ├── ComposerWidget.cs
    ├── ComposerWidgetTree.cs
    └── ComposerWidgetNode.cs

src/Forma.UI.Features.Composer/
├── Documents/
│   └── DocumentConverterRegistry.cs
├── Editing/
│   └── ComposerHistory.cs
└── Sanitization/
    ├── DefaultHtmlSanitizer.cs
    └── HtmlAllowList.cs               # the allowlist the sanitiser enforces

src/Forma.UI.Blazor/Components/Composer/
├── FormaComposer.razor                    # engine shell, editor routing
├── FormaComposer.razor.cs
├── FormaComposer.razor.css
├── FormaComposer.razor.js                 # asset loader, selection bridge
├── FormaComposer.he.json
├── FormaComposer.es.json
├── FormaComposerOptions.cs
├── FormaComposerToolbar.razor             # toolbar + editor switcher
├── FormaComposerToolbar.razor.cs
├── FormaComposerToolbar.razor.css
│
├── RichTextEditor/
│   ├── RichTextEditorPlugin.cs
│   ├── FormaRichTextEditor.razor          # contenteditable canvas
│   ├── FormaRichTextEditor.razor.cs
│   ├── FormaRichTextEditor.razor.css
│   ├── FormaRichTextEditor.razor.js       # owns the surface and the selection
│   ├── FormaBasicRichTextEditor.razor     # preset: Coi Basic
│   ├── FormaLiteRichTextEditor.razor      # preset: Coi Mini
│   ├── FormaRichTextBubbleMenu.razor
│   ├── FormaRichTextBubbleMenu.razor.cs
│   ├── FormaRichTextBubbleMenu.razor.css
│   ├── Plugins/                           # namespace …Composer.RichTextEditor.Plugins
│   │   ├── BasicFormattingPlugin.cs
│   │   ├── TypographyPlugin.cs
│   │   ├── ParagraphFormatPlugin.cs
│   │   ├── ListPlugin.cs
│   │   ├── AlignmentBidiPlugin.cs
│   │   ├── LinkPlugin.cs
│   │   ├── MediaPlugin.cs
│   │   ├── VideoPlugin.cs
│   │   ├── TablePlugin.cs
│   │   ├── HorizontalRulePlugin.cs
│   │   ├── EmojiPlugin.cs
│   │   ├── SpecialCharacterPlugin.cs
│   │   ├── ClipboardPlugin.cs
│   │   ├── CodeBlockPlugin.cs
│   │   ├── MarkdownBlockPlugin.cs
│   │   ├── MathPlugin.cs
│   │   ├── PreviewPlugin.cs
│   │   ├── MaximizePlugin.cs
│   │   ├── TemplatePlugin.cs
│   │   └── EmbedPlugin.cs
│   └── Dialogs/                           # namespace …Composer.RichTextEditor.Dialogs
│       ├── FormaRichTextLinkDialog.razor
│       ├── FormaRichTextTableDialog.razor
│       ├── FormaRichTextVideoDialog.razor
│       └── FormaRichTextEmbedDialog.razor
│
├── MarkdownEditor/
│   ├── MarkdownEditorPlugin.cs
│   ├── FormaMarkdownEditor.razor
│   ├── FormaMarkdownEditor.razor.cs
│   └── FormaMarkdownEditor.razor.css
│
├── CodeEditor/
│   ├── CodeEditorPlugin.cs
│   ├── FormaCodeEditor.razor
│   ├── FormaCodeEditor.razor.cs
│   ├── FormaCodeEditor.razor.css
│   └── FormaCodeEditor.razor.js        # tab, bracket pairing, scroll sync
│
└── PageDesigner/
    ├── PageDesignerPlugin.cs
    ├── FormaPageDesigner.razor             # palette, canvas, settings panel
    ├── FormaPageDesigner.razor.cs
    ├── FormaPageDesigner.razor.css
    ├── FormaPageDesigner.razor.js          # pointer drag, press-and-hold, resize
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
```

Every folder and its namespace stay in step, so `Plugins/` and `Dialogs/` are real namespace segments.

**The two feature projects are foldered by concern rather than flat**, and the implementation mirrors the abstractions folder for folder — `Sanitization/` against `Sanitization/`, `Editing/` against `Editing/` — so an implementation sits under the same name as the contract it satisfies, and the test tree mirrors both.

**Localization follows `FormaExplorer`:** every component under `Composer/`, including those in sub-namespaces, injects `IStringLocale<FormaComposer>` and shares the one `FormaComposer.he.json` / `FormaComposer.es.json` pair at the category root. Resources resolve by the localizer's **type** rather than by file location, so this works — and a per-component pair in a sub-namespace would silently fall back to English keys instead. This is the failure mode that has to be re-verified whenever a file moves.

---

## 10. Phases

Each phase ends buildable, tested and reviewable.

1. **Capability model.** ✅ The two abstraction projects, every contract in §3 and §5–§7, the default sanitiser, the converter registry, and history are built, and `IComposerAssetLoader` has the JS-backed implementation it was missing. That one sits in `Forma.UI.Blazor` rather than here, because it needs `IJSRuntime`, which places it with Phase 2's engine shell. This lands first precisely because the external-dependency question decides everything after it.
2. **Engine shell.** ✅ `FormaComposer`, `FormaComposerToolbar`, the `ComposerDocument` binding of §3.7, plugin discovery and availability probing, the selection bridge, `ComposerText`, and the debounced sync of §3.6 are built, along with the command spine, the key map, and the toolbar row model of §3.8. The editor switcher is a controlled `FormaTabs` strip above each surface's own toolbar rather than a segmented control inside it. The selection bridge is the shared `Text` operation, not welded into any one editor plugin, so a future textarea-based surface reuses it rather than reimplementing it; a contenteditable surface will need its own, because the DOM APIs genuinely differ, but through the same generic `ComposerContext.ReportSelectionAsync` seam.
2a. **Regions.** ◐ The status strip of §3.8 is built: `IComposerRegionRegistry` pushes `IComposerStatusItem` handles that any plugin creates and disposes, cascaded on `ComposerContext.Regions` so no editor is a special case. `FormaComposerStatusStrip` renders it once per composer, beneath whichever surface is active. The gutter's contract (`IComposerGutterMarker`) is built and tested the same way; its visual rendering waits on a line-oriented surface, since pixel-aligning a marker beside a wrapping `<textarea>` needs font-metric mirroring that would itself become a one-off hack rather than a real capability.
3. **Rich text editor and core formatting.** `RichTextEditorPlugin`, the canvas, the bubble menu, and the formatting, typography, paragraph, list and alignment/bidi plugins.
4. **Markdown and code editors.** `MarkdownEditorPlugin`, `CodeEditorPlugin`, `Forma.UI.Features.Composer.Markdown`, the lossless-only HTML → Markdown guard, and the embedded Markdown and code blocks.
5. **Media, tables and embeds.** Link, media, video, table, horizontal rule, emoji, special character, clipboard, preview, maximize, template and embed plugins.
6. **Satellite capabilities.** `Forma.UI.Features.Composer.TextMate`, `.KaTeX`, `.HtmlSanitizer`, and the highlighting and math plugins that consume them.
7. **Page designer.** `PageDesignerPlugin`, the palette, canvas, per-widget chrome and settings panel, `IComposerDesignerPlugin` with the built-in palette, `ComposerWidgetTree` with its renderer, pointer-based drag with a keyboard equivalent, and the text widget hosting `FormaRichTextEditor`. It lands last because it composes everything above it — a designer without a working rich text editor inside its text widget is not the component Coi had.
8. **Accessibility, touch, direction, tests and Showcase.** §8 verified in a real browser, `he` and `es-419` checked, bUnit tests under `tests/Forma.UI.Blazor.Tests/Components/Composer/` mirroring the source tree, and a Showcase page at `/composer` rendering every state each plugin can enter.

---

## 11. Decisions

### 11.1 Settled

| # | Question | Answer |
|---|---|---|
| C1 | Which external libraries may the composer bundle? | Any, provided all five conditions in §3.3 hold. The rule is a standing carve-out by category, not a named list, so Markdig, TextMateSharp and KaTeX all qualify without a per-library approval. |
| C2 | How are the abstractions named, given `Ganss.Xss.IHtmlSanitizer` may be in a host's scope? | Everything in `Forma.UI.Features.Composer.Abstractions` carries the prefix: `IComposer*` for contracts, `Composer*` for types. That settles the collision as a side effect of a convention rather than a special case, and a host can register both sanitisers without an alias. |
| C3 | Should there be a plain-text editor plugin, or is plain text only ever a conversion target? | Conversion target only. `ComposerDocumentFormat.PlainText` exists; no surface ships with it. Nobody has asked for one, and adding it later is a plugin, not a change. |
| C4 | What does `@bind-Value` carry when the active editor's format is not the bound format? | A `ComposerDocument` — the content together with the format it is written in (§3.7). A bare string would force every document through HTML and rewrite a Markdown author's source on each round-trip. The format is a string-keyed value rather than an enum, so a new format costs the library nothing. |
| C5 | Are `TemplatePlugin`'s templates host-supplied or does Forma ship a set? | Host-supplied. A template is editorial content and Forma has no editorial opinion. |
| C6 | What *is* the page designer's saved document? | A `ComposerWidgetTree` — JSON, with HTML as output rather than storage (§4.2). Coi's saved HTML froze rendering decisions at save time and never escaped the tree anyway, since recovering a widget needs `data-widget` and `data-settings` attributes. Columns are nodes, not an array on the row. Document metadata stays on the host's row. |
| C7 | One designer plugin or several, given Coi drove seven targets through `OpenTemplate`'s numeric type? | One, varied by its options and the registered widget set. The palette is already extensible, so a second plugin would only re-register widgets. |
| C8 | Who owns layout and theme? | Not the composer. `FormaLayout` and `FormaTheme` are library-wide components built first and composed afterwards; the designer ships template-only and nothing in phases 1–8 waits on them. Both want their own `COMPONENTS.md` entries. |
| C9 | May a designer be placed on a designer canvas? | No. `PageDesignerPlugin` implements both `IComposerEditorPlugin` and `IComposerDesignerPlugin`, so the placeable set is "any editor plugin that is not also a designer plugin" — a type test, and a designer excludes itself. |
| C10 | How does an editor choose between the shared toolbar row and one of its own? | By setting `ComposerToolbarItem.Row`, or leaving it null for its category's default row (§3.8). Both editors and external plugins declare rows through `ToolbarRows`, so there is one mechanism rather than a separate registration path per category. |
| C11 | Are gutter markers and status readouts pushed or pulled? | Pushed, through a handle the plugin mutates and disposes. Toolbar items stay static lists. This matches VS Code's `createStatusBarItem` and decoration types, and the dispose pattern its API reference states as the general rule (§3.8). |
| C12 | Where do the common editing operations live? | In the engine, as `ComposerText` returning `ComposerEdit` (§3.4). Word-at-caret, selected text, range replacement, per-line mapping and replace-all are needed by every editor and every plugin that touches text; leaving them out means each one reimplements them against raw strings. |

### 11.2 Open

None. Every decision this plan depends on is settled; phase 1 can start.
