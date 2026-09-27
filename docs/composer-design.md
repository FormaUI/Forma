# Forma Composer Design

An extensible authoring surface for the **Forma** component library, replacing both halves of Coi's authoring stack: the CKEditor 4 rich text editor and the `template.aspx` drag-and-drop page designer. It hosts rich text, Markdown, code and page composition, and every capability it has — including every editor — arrives as a plugin.

The name is the claim: **the composer composes content, and content can be composed of more content.** A page designer places widgets; one of those widgets is a text widget; that text widget hosts the rich text editor. Coi did exactly this with `CKEDITOR.inline`, and the plugin model makes it fall out rather than be special-cased.

- **Component category:** its own project `Forma.Blazor.Composer`, namespace `Forma.Blazor.Components.Composer`
- **Root component:** `FormaComposer`
- **Supersedes:** the standalone rich text editor, and Coi's `admin/pops/template.aspx` over `Controls/Edit/`
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

Two Coi behaviors are deliberately **not** reproduced as-authored:

| Coi behavior | Why not, and what replaces it |
|---|---|
| `allowedContent = true` — no content filtering | An unfiltered editor is a stored-XSS pipe. Replaced by the sanitization pipeline in §6, which is mandatory and cannot be switched off. |
| `qform` inserting `<script src="/c.js">` | Author-authored `<script>` tags are arbitrary code execution by design. Replaced by `EmbedPlugin`, which inserts a declarative, attribute-only placeholder element the **host** resolves when it renders the document. The author gets the same capability — drop a form into an article — without the editor ever becoming a script injector. |

### 1.3 Modernisations

- **Font size** in `rem`, not `px`: the picker labels a size by its pixel value at the reader's default and writes it as that value over 16 — `12` becomes `0.75rem`, `16` becomes `1rem` — so authored content follows the reader's font size preference; a host wanting a fixed physical size chooses points through `TypographyPluginOptions.FontSizeUnit`.
- **Line height** as unitless multipliers: `1.0`, `1.15`, `1.5`, `1.6`, `2.0` — replacing Coi's 72 pixel values.
- **Composition over reimplementation:** `FormaToolbar` menus for the format, font, size and color pickers, `FormaCommandArea` and `FormaOverflowMenu` for the toolbar and its overflow, `FormaTabs` for the editor switcher, `FormaExplorer` for stored media, `FormaFilePicker` + `FormaFileUploader` for uploads, `FormaEmojiPicker` for emoji, `FormaDialog` for dialogs.
- **Logical CSS** throughout, and per-block `dir` so bidi is a first-class property of the document rather than a toolbar afterthought.

---

## 2. Assembly layout

The composer is two assemblies, split along what an outside package needs. `Forma.Framework.Composer.Extensibility` carries the contracts a plugin author compiles against; `Forma.Framework.Composer` carries the engine that implements them. A capability resting on an external dependency is a third case, and ships as a satellite package a host adds deliberately; so is a capability joining the composer to another component, as browsing stored files joins it to `FormaExplorer`, so that neither component's package depends on the other.

The split exists because a plugin may live outside the composer's own package, as the explorer's does. Whatever such a package extends has to be a package in its own right, or both would carry a copy of the same assembly and a host installing both would get two. Publishing the engine alongside the contracts would put the catalog, the command registry, the key-binding catalog and the history into public API for callers who never touch them, so only the contracts go.

| Project | Namespace | References |
|---|---|---|
| `Forma.Framework.Composer.Extensibility` | `Forma.Framework.Composer` | `Forma.Framework.Plugins`, `Forma.Framework.Icons`, `Forma.Framework.Toolbar`, `Forma.Framework.Resources`, `Forma.Framework.Sanitization` |
| `Forma.Framework.Composer` | `Forma.Framework.Composer` | the contracts, `Forma.Framework.Icons`, `Forma.Framework.Toolbar` |
| `Forma.Framework.Composer.Interop` | `Forma.Framework.Composer.Interop` | `Forma.Framework.JavaScript.Interop` + ASP.NET Core |
| `Forma.AddOns.Composer.Markdown` | `Forma.Framework.Composer.Markdown` | the contracts + Markdig |
| `Forma.AddOns.Composer.TextMate` | `Forma.Framework.Composer.TextMate` | the contracts + TextMateSharp |
| `Forma.AddOns.Composer.KaTeX` | `Forma.Framework.Composer.KaTeX` | the contracts + KaTeX browser assets |
| `Forma.Blazor.Composer` | `Forma.Blazor.Components.Composer` | the engine, the interop assembly and `Forma.Framework.Sanitization` |
| `Forma.AddOns.Composer.Plugins.Explorer` | `Forma.Blazor.Components.Composer.Plugins` | the contracts + `Forma.Blazor.Explorer`, whose `FormaExplorer` its dialog composes |

Both assemblies share the namespace `Forma.Framework.Composer`, because the split is a packaging boundary rather than a second concept: a host moving a type across it should not have to change a `using`.

Sanitization is not the composer's own concern. Anything holding markup to a policy needs it, so the contract and the allowlist implementation sit together in `Forma.Framework.Sanitization`; a host replacing it with a hardened library registers its own `IHtmlSanitizer` and the composer takes it.

The rich text canvas's script module and its `RichTextEditor` wrapper live in `Forma.Framework.Composer.Interop`, the one composer assembly that compiles against ASP.NET Core, so the contracts stay free of it.

**The composer project must not take `FrameworkReference Microsoft.AspNetCore.App`.** The repository confines that to the assemblies that genuinely compile against ASP.NET Core — the components, the interop wrappers and the test support — so no contract may mention `RenderFragment`, `EventCallback` or `ComponentBase`. Editor plugins therefore surface a `Type` and the composer hosts it in `<DynamicComponent>`; a plugin that needs custom toolbar markup supplies a `Type` too. This is a constraint on the design, not an inconvenience — it keeps every contract testable from a plain class library.

**Every public type the composer owns carries the prefix** — `IComposer*` for contracts, `Composer*` for types. The namespace would disambiguate on its own, but a host sees these names in its DI registrations and its `using` list, where `ComposerDocumentFormat` cannot be mistaken for a framework type. The prefix is the convention for this assembly rather than a special case for the one collision that prompted it. It stops at the assembly boundary: a contract that is not the composer's own, such as `IHtmlSanitizer`, is named for what it does.

The engine ships inside the components' own package, because a host installing the composer gets a working one. The contracts are packable in their own right, since packages outside that one extend them, and so is each satellite, so a host takes only the capabilities it wants.

---

## 3. The engine

### 3.1 Plugin contracts

```csharp
namespace Forma.Framework.Composer.Plugins;

public interface IComposerPlugin : IComposerKeyBindingSource
{
    string Id { get; }

    IReadOnlyList<ComposerToolbarItem> Items => [];

    IReadOnlyList<ComposerToolbarRow> Rows => [];

    IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    IReadOnlyList<string> CommandPrefixes => [];

    IReadOnlyList<ComposerAsset> Assets => [];

    IReadOnlyCollection<string> ActiveStates => [];

    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}

public interface IComposerFeaturePlugin : IComposerPlugin;

public interface IComposerEditorPlugin : IComposerPlugin
{
    string Title { get; }

    Icon Icon { get; }

    ComposerDocumentFormat Format { get; }

    Type EditorComponent { get; }

    ValueTask<bool> TryInsertAsync(ComposerInsertion insertion, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(false);
}

public interface IComposerViewPlugin : IComposerPlugin
{
    string Kind { get; }

    string Title { get; }
    Icon Icon { get; }

    Type ViewComponent { get; }
}
```

Key bindings come through `IComposerKeyBindingSource`, which every plugin is and which the engine's own `ComposerKeyBindingSource.Default` also implements, so one `ComposerKeyBindingCatalog` merges them all, the earliest source winning a contested command. **No plugin declares where it applies**; §3.1a's registration tree says that, so a plugin shipped by another package never has to name an editor whose type it cannot reference.

A **view** has the shape of an editor — its own component, rows, items and assets, maximizable like any surface — but shows one editor's document read-only rather than editing it. An editor offers a view by registering it as a child, and a feature registered under that view contributes to its rows exactly as one registered under an editor contributes to the editor's; the layout for a view takes the built-in row, the rows the view declared, and what is registered under the view or at the composer root, and nothing else — so neither an editor's formatting nor a command acting on the document, such as attaching a file, lands on its preview. The switcher lists the active editor's views as trailing tabs after the editors, and those tabs are the only way in: a view is its own surface, not a toolbar toggle. A plugin reports which of its own toggles and options are active through `ActiveStates`, which is how a view's paper size and zoom show in its bar without going through the selection.

`Title` is the **English source string**, not a resolved translation, and so is every title and row label a plugin contributes; the composer localizes them when it draws them. A plugin never resolves a localizer of its own — one that did would capture whichever culture it was built under, and would have to carry the composer's words for every group it joins. What a plugin does name is **where its localization lives**, through `LocalizationSourceType`, the way a component part names its composite's: the composer reads a title from that type's locale first and from its own after, so a plugin shipped from another package brings its own strings and still borrows the composer's for a group they share. A plugin naming none is read from the composer's alone. Localization happens before contributions are merged, since a merged group no longer says which plugin declared each item in it.

### 3.1a Composing a composer

A host describes a composer with a builder. The description is immutable once built, and every `FormaComposer` activates it into a set of plugins it owns alone, because an editor plugin holds events its editor component subscribes to and two composers sharing one would each answer the other's commands.

```csharp
services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor
        .TryAddDefaultFeatures()
        .AddFeaturePlugin<TablePlugin, TablePluginOptions>(options => options.MaxRows = 12)
        .AddFeaturePlugin<ExplorerPlugin, ExplorerPluginOptions>(options => options.AcceptedResourceKinds = "contentType:image")
        .AddViewPlugin<RichTextPreviewViewPlugin>())
    .TryAddDefaultPlugins());
```

**Where a registration sits is the scope it applies to.** The composer root spans every editor and every view; under an editor is that editor's surface; under a view is that view's bar. This replaces the three properties that declared the same thing — a feature's supported editors, whether it joined view bars, and the editor a view belonged to — and it puts the decision where the knowledge is, since the host knows what it installed and a plugin from another package cannot.

**Role comes from the method**, `AddEditorPlugin`, `AddFeaturePlugin` and `AddViewPlugin`, each constraining its type parameter, so a miscategorized plugin is a compile error rather than a surprise in the catalog. `AddRichTextEditor` and its siblings are sugar over `AddEditorPlugin<T>`.

**The builders own the composer's vocabulary alone.** Recording a registration, resolving defaults, constructing the tree in order and disposing what it built are one mechanism serving any feature with extensions, and live in `Forma.Framework.Plugins` as `PluginRegistry<TPlugin>` and the `PluginScope<TPlugin>` it activates into. `IComposerPlugin` is an `IPlugin`, so the composer's builders are role-typed faces over that registry rather than a second copy of it.

**No call depends on the order it was written in.** Defaults and anything else resolved against the whole tree are recorded and applied when the definition is built, so `TryAddDefaultFeatures` means "whatever this scope has not registered itself" however early it appears. Each builder carries only its own defaults method, so attaching one to the wrong scope does not compile.

**Each editor has its own defaults.** An editor's builder is typed by the editor it describes, `ComposerEditorBuilder<TEditor>`, and each editor's package extends its own: the rich text editor's `TryAddDefaultFeatures` brings its formatting, attaching, the selection bar and its views; the plain text editor's brings attaching. Calling one editor's defaults on another does not compile, since the rich text defaults depend on the rich text editor and would otherwise fail only when a composer activated them. The composer's own `TryAddDefaultPlugins` brings what belongs on every editor and every view alike, which is maximizing.

**A registration is a type, its options type and a delegate to configure them** — never a constructed instance, which would be shared. An implementation declares the options it takes through `IPluginConfigurable<TOptions>`, so a registration naming the wrong pair does not compile. Each activation creates its own options, and options are settable rather than `init`-only, which is what a configure delegate and binding from configuration both need; an options record's `Default` therefore answers a fresh value rather than one every plugin could write to.

**A registration may name a factory instead**, for a plugin whose construction neither the composer's own plugins, its options nor the host's services can satisfy. That is the deliberate escape rather than the accident the type-based shape prevents: the host writes the factory, so a factory answering one instance to every composer is a choice made in the open. A composer disposes the plugins it built and leaves a factory's to whoever wrote it.

Activation resolves each constructor parameter in order: a plugin in this composer's own set, the registration's options, the host's service provider, the parameter's default. Anything else is an `InvalidOperationException` naming the plugin, the parameter and what could have supplied it. The composer disposes what it built and nothing resolved from the host.

**A registration reaches its own scope and no further.** Nothing registers a feature on every view of an editor, or on an editor and its views together: the one plugin that wanted either, maximizing, sits at the root, which already reaches them all, and a builder method added for a real case later breaks nothing.

An identifier is unique within its owner rather than across the composer, so two editors may each own a link plugin. Command dispatch is therefore rebuilt from the scope in play — root, active editor, active view — whenever the active editor or view changes.

There is **no assembly scanning**. Scanning cannot decide scope, since the property that would have told it is the one the tree replaced, so every scan would be the host asserting a placement nothing checked. Naming a type from a package costs a host no more than naming its assembly did, and the set a scan was really wanted for — an editor's own standard features — is what `TryAddDefaultFeatures` supplies.

### 3.2 One plugin model for every kind of dependency

A capability may come from .NET, from a browser module, from a native library behind a managed wrapper, or from a service the host wrote. The engine does not care. A plugin declares what it needs and the engine decides whether it can run:

- **Managed dependencies** arrive by constructor injection. If the host never registered `IComposerDocumentConverter` for Markdown, `MarkdownEditorPlugin` is never constructed, is never registered, and its entry never appears in the switcher.
- **Browser dependencies** are declared as `ComposerAsset` values and loaded by the engine's `IComposerAssetLoader`, once per circuit, on demand by default. The loader owns caching, `JSException`/`JSDisconnectedException` handling and disposal, so no plugin writes that boilerplate twice.
- **Availability** is a question the plugin answers through `IsAvailableAsync`. An unavailable plugin degrades to absent — never to a control that throws when pressed.

```csharp
public sealed record ComposerAsset(ComposerAssetKind Kind, string Url, ComposerAssetLoading Loading = ComposerAssetLoading.OnDemand);

public enum ComposerAssetKind { Module, Stylesheet, Font }

public enum ComposerAssetLoading { Eager, OnDemand }
```

This is what makes KaTeX, TextMate and Markdig the same shape of problem: each is a satellite package registering a service and, where it needs one, declaring an asset. Adding a capability never edits the engine.

### 3.3 What the carve-out permits, and what it does not

The library is JS-free by default and the composer is the one place that rule bends. It bends by category rather than by naming a library, so a capability may bundle an external dependency provided all five hold:

1. It lives in its own `Forma.AddOns.Composer.{Capability}` satellite, so a host that does not want it never downloads it.
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
| Commands | `ComposerCommandRegistry`, `ComposerCommands` | Resolves a command against the engine's own commands first, then against the plugin that named it through an item, a key binding, an input trigger or a command prefix. |
| Key map | `ComposerKeyBindingCatalog` | The engine's bindings merged with every plugin's, registered on the composer's root and again on the active editor's editable element, where a native control would otherwise claim them. |
| Surface regions | §3.8 | Toolbar rows, the gutter and the status strip, each open to the same three contribution categories. |
| Conversion | `IComposerDocumentConverter` | §5. |
| Sanitization | `IHtmlSanitizer` | §6. Not optional. |
| Asset loading | `IComposerAssetLoader` | §3.2. |
| Clipboard | `IHtmlSanitizer` | Pasted markup reaches the document through the same sanitized commit as typed content; paste from Word cleans the markup before it is inserted. |
| Syntax highlighting | `IComposerSyntaxHighlighter` | §7. |
| Dialogs and insertion | `ComposerInsertionRequest`, `IComposerInsertionDialog`, `ComposerInsertion` | §3.4a. |

### 3.4a Dialogs and insertion

A plugin that collects something from the author asks the engine for a dialog and supplies only its body; the engine owns the shell. Every composer dialog collects something to put into the document, so the contract says insertion rather than claiming to host any dialog at all.

```csharp
public sealed record ComposerInsertionRequest
{
    public required Type Body { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = ReadOnlyDictionary<string, object?>.Empty;
    public ComposerDialogSize Size { get; init; } = ComposerDialogSize.Medium;
}

public interface IComposerInsertionDialog
{
    string Title { get; }

    string? Notice => null;

    IReadOnlyList<string> Messages => [];

    ValueTask<ComposerInsertion?> TryConfirmAsync(CancellationToken cancellationToken = default);
}

public abstract record ComposerInsertion;

public sealed record ResourceInsertion(string Source, string Name, string Kind, bool Embed, bool Link) : ComposerInsertion;
```

The engine hosts `Body` in a `<DynamicComponent>` inside its own shell, which draws the header, the notice, the message list and the confirm and cancel controls, so every dialog looks the same however it arrived. A `Body` that does not implement the interface is rejected where the request is made rather than ignored at confirm time.

**The body resolves its own title, notice and messages**, rather than the request carrying them, because those strings belong to whichever package ships the body: a plugin never resolves a localizer, and the composer has no resources for a body it has never heard of. The request says which body and how much room it gets, and nothing about what it says.

Confirming asks the body for an insertion and hands it to the active editor through `IComposerEditorPlugin.TryInsertAsync`, which answers false when it takes none of that kind. **That bool is the compatibility check.** A plugin registered under an editor that cannot take what it produces needs no capability contract to declare the mismatch: the dialog stays open and says so, which is also what a read-only or disabled composer does. What the body refused and what the editor refused are shown together, the body's own messages first.

Two members on `IComposerInteraction` carry it: `OpenDialogAsync(ComposerInsertionRequest)` and `TryInsertAsync(ComposerInsertion)`.

The rich text editor's own link, table, video, image and embed dialogs stay its own rather than taking this path, since moving them would also mean reopening one through the request on an element the author double-clicks, which is a change with a reason of its own.

### 3.5 Shared state

Two values are cascaded to every plugin component. `ComposerContext` is an immutable record — the active editor and the view over it if one is showing, the document, the selection, the chrome state (disabled, read-only, maximized, selection bar shown), the key map, and the live engine services above — rebuilt whenever any of it changes, so a changed context is also the re-render signal.

The surface is carried as the plugins themselves rather than their identifiers, because a plugin holds no catalog to resolve an identifier against, and it is carried as two values because an editor showing a read-only view of its document is a different surface from the editor itself — which a plugin registered at the composer root has to tell apart to know whether a command that edits belongs there. A host still binds `FormaComposer.ActiveEditorId` as an identifier: it chooses a surface before the composer has constructed any plugin, and an identifier is what a host can store and restore. `IComposerInteraction`, implemented by the composer itself, carries the operations a plugin invokes: changing the document, reporting a selection, running a command, requesting a render, opening a dialog and inserting what it collected. Neither carries Blazor types, so both are contracts in the extensibility package, and a plugin from another package reads and drives the composer without referencing the engine. An editor's component is handed one value more, the selection bar: the items plugins placed on the selection row, already localized, which an editor with a text selection draws over it.

### 3.6 Where the editing surface actually lives

The rich text editor's script module, in the interop assembly and reached through `RichTextEditor`, owns the `contenteditable` surface and the browser selection. C# receives changes **debounced, and on blur** — never per keystroke. Under Interactive Server every keystroke would otherwise be a SignalR round-trip, and an article body over a mobile connection is exactly the case that has to feel local. Commands travel the other way as explicit calls into the module; the document in C# is the source of truth for persistence, the DOM is the source of truth for the caret. The selection report is deduplicated rather than debounced: the canvas reports a selection once per distinct block, styles and caret-or-range — on click, on key-up and on `selectionchange` — which is what the toolbar reflects, so a keystroke or an arrow that moves the caret within one formatting costs no round-trip; the offsets a report carries are those of the change that raised it, and nothing on the rich text path reads them. The plain text editor, over a `<textarea>`, coalesces keystroke-driven caret movement on the same debounce as its content commit.

### 3.7 What the host binds

```csharp
public sealed record ComposerDocument(ComposerDocumentFormat Format, string Content)
{
    public IReadOnlyList<ResourceInfo> Attachments { get; init; } = [];
}
```

```razor
<FormaComposer @bind-Value="_body" />

@code {
    private ComposerDocument _body = new(ComposerDocumentFormat.Html, "");
}
```

The bound value carries its own format because the composer holds more than one kind of content and only one of them is HTML. Binding a bare string would force every document through HTML, which rewrites a Markdown author's source on every round-trip even when §5.2 permits the conversion — reference links return inline, Setext headings return as `###`, hand-wrapped paragraphs return joined.

Switching editor converts `Content` and refuses when §5.2 finds the conversion lossy, so the stored value stays in the format the author last used. The switch is itself an undoable step (§3.4), so a conversion the author dislikes is one undo away.

`Attachments` carries files beside the content, never written into it, and the engine lists them beneath the canvas whichever editor is active, each removable from there.

A host that only ever wants HTML registers the rich text plugin alone and reads `Content`: one property, no conversion, no switcher. `FormaInputBase<TValue>` is generic, so `FormaInputBase<ComposerDocument>` is the conventional base here; its `TryParseValueFromString` reads the serialized form, which is the format id, a newline, then the content, and a value carrying no newline is HTML. A host is free to persist `Format` and `Content` as two columns instead, which keeps the format queryable.

---

### 3.8 Surface regions

Everything around the canvas is a **region**, and every region takes contributions from the same three categories. The category is not an enum — it falls out of who contributes and which region they name.

| # | Category | Contributor | Row its items join unless they name another |
|---|---|---|---|
| 1 | Composer general | the engine | the built-in toolbar row |
| 2 | Editor specific | `IComposerEditorPlugin` | the built-in toolbar row |
| 3 | External | any other `IComposerPlugin` | the plugin toolbar row |

An item names its row through `ComposerToolbarItem.RowId`: the shared built-in row, one of the rows the composer always provides — formatting, paragraph, plugins and selection, named in `ComposerRows` — or a row its contributor declares through `Rows`. An editor therefore chooses between joining the shared row and standing on its own purely by the row it names, and a plugin does the same. Rows an editor declares render after the built-in row, the composer's own rows follow, then the rows external plugins declared; a row nothing fills is dropped, and an item naming a row nobody declared lands on the plugins row rather than vanishing. Rows holding only a few commands are folded together so no bar is spent on a handful; the plugins row shows on demand behind a toggle at the end of the first bar, overflowing its surplus into a menu; and the selection row is what the selection bar renders.

| Region | Placement |
|---|---|
| Toolbar rows | above the canvas, stacked in a `FormaCommandArea` |
| Gutter | inline-start of the canvas: line numbers plus contributed line markers |
| Status strip | block-end of the canvas: contributed readouts such as Ln/Col, word count, language |

Toolbar contributions are static lists, because a toolbar item's identity does not change. Gutter markers and status readouts are not: they move with the caret and the document. Those are **pushed** — a plugin acquires a handle from the engine, mutates it, and disposes it — rather than pulled by the engine on every render.

That follows VS Code, where `window.createStatusBarItem` returns a `StatusBarItem` whose `text`, `tooltip` and `command` are mutable and which is disposed when done, and where gutter icons are decorations pushed through `setDecorations` against a disposable `TextEditorDecorationType`. Its API reference states the rule plainly: *"The VS Code API uses the dispose pattern for resources that are obtained from VS Code."* The pull shape appears there only for providers such as `NotebookCellStatusBarItemProvider`, which is not the editor status bar.

Being pushed makes a status readout change often, which decides how it is announced. A live region covering the whole strip would speak every caret move, so the strip is not one; a persistent, visually hidden region sits beside it and carries only the items whose `IsAnnounced` is set. A readout such as Ln/Col stays silent and is read on demand, while an event worth interrupting for, such as a save confirmation, opts in. The region is rendered from the composer's first paint rather than with the first item, because a live region inserted already holding its text is not reliably announced.

---

## 4. Plugins

### 4.1 Editor plugins
Each lives in its own directory, mirroring how `Explorer` earned a category of its own.

| Directory | Plugin | Surface |
|---|---|---|
| `PlainTextEditor/` | `PlainTextEditorPlugin` | A `<textarea>` over the document's text, and the editor the composer falls back to when no registered editor is available. |
| `RichTextEditor/` | `RichTextEditorPlugin` | Visual `contenteditable` canvas. Hosts the feature plugins, the selection bar, and embedded Markdown and code blocks. |
| `MarkdownEditor/` | `MarkdownEditorPlugin` | Markdown source with line numbers, highlighting and a live rendered preview. |
| `CodeEditor/` | `CodeEditorPlugin` | Lightweight code editor — line numbers, active-line marker, smart `Tab`, bracket pairing, language picker. Not a Monaco replacement, and not HTML-only: HTML, CSS, JavaScript, TypeScript, C#, SQL, JSON, YAML, Bash, Python. |
| `PageDesigner/` | `PageDesignerPlugin` | Drag-and-drop widget canvas with a palette, per-widget chrome, nesting through rows and columns, and a settings panel per widget. Replaces Coi's `template.aspx`. |

#### Views

The rich text editor ships two views under `RichTextEditor/Views/`. **Preview** lays the document out as a reader will see it at a chosen size — the ISO A series, Letter and Legal as sheets with a printer's margin, or the width of a phone, tablet or desktop — turned to landscape or zoomed, so the author can see how it reads at that size; Print sends the document alone to the printer, whose dialog owns the paper, and Export saves it as an HTML file. **Source** shows the document's HTML one block per numbered line, wrapped or not, copied to the clipboard or downloaded. Both are read-only by design; a source that edits and commits back is what the code editor is for. The document's own look — headings, quotes, code, lists, tables, figures, links and the composer's placeholders — lives in `forma-prose.css` under the `forma-prose` class, so the canvas, the preview, the printed page and a host page rendering the document to readers all draw it the same way.

### 4.2 The page designer

The designer is an editor plugin like the others, so it costs the engine nothing: it appears in the switcher when registered, and a host that only wants a rich text field never sees it.

What it adds is a second extension point, because a palette that only Forma can fill is useless — Coi's palette was half host-domain (product price, buy button, page summary) and Épica's would be too (race results, rider cards, stage profiles).

```csharp
public interface IComposerDesignerPlugin : IComposerPlugin
{
    IReadOnlyList<ComposerWidget> Widgets { get; }
}

public sealed record ComposerWidget
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Category { get; init; }

    public required Icon Icon { get; init; }

    public required Type Component { get; init; }

    public Type? SettingsComponent { get; init; }

    public Type? SettingsType { get; init; }

    public bool IsContainer { get; init; }
}
```

Built-in widgets cover Coi's generic palette — text, image, gallery, video, button, row, column, spacer, divider, form, map, embed, code. Everything domain-specific is the host's, registered the same way Forma's own are, so Épica adds a race-results widget without forking anything.

#### The saved document is a widget tree

```csharp
public sealed record ComposerWidgetTree
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<ComposerWidgetNode> Widgets { get; init; } = [];
}

public sealed record ComposerWidgetNode
{
    public required string InstanceId { get; init; }

    public required string WidgetId { get; init; }

    public string? Content { get; init; }

    public JsonElement Settings { get; init; }

    public IReadOnlyList<ComposerWidgetNode> Children { get; init; } = [];
}
```

Serialized, that tree is a `ComposerDocument.Content` of format `widget-tree` (§3.7), so the designer needs no special case in the binding.

`Content` and `Settings` are split by **which surface edits them**: content is typed on the canvas — the text widget's rich text, the code widget's source — while settings are what `FormaWidgetSettingsPanel` commits. They also sync differently, content debounced and on blur per §3.6. `Content` is therefore exactly the sanitization surface of §6; the JSON never is.

The engine never deserializes `Settings`. It hands the `JsonElement` to the widget's `SettingsComponent`, which reads it as its own record, so a host's race-results widget defines `RaceResultsSettings` and Forma never learns the field names.

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

**HTML is output, not storage.** `FormaWidgetRenderer` walks the tree emitting a `<DynamicComponent>` per widget — the same walk the canvas performs, minus `FormaWidgetFrame` — so it works under static SSR and needs no `HtmlRenderer`. A host wanting an HTML *string*, for an email, an API or a cache, calls an opt-in service; that is the only path needing `HtmlRenderer`, and it lives in `Forma.Blazor` because the composer project may not reference ASP.NET Core.

This is why the tree wins over Coi's saved HTML. Coi froze rendering decisions at save time — `<img src="/media/8842.jpg">`, `style="width:60%"` — so a new image format, a CDN move or a theme change could not reach an already-saved page, and a widget dropped from the palette left stale markup rendering silently instead of a visible "no longer available". Storing `mediaId: 8842` and `raceId: 42` keeps the author's *intent* and decides the markup at render time. Coi's approach also never escaped the tree: recovering a widget's identity and settings from HTML requires `data-widget` and `data-settings` attributes, which is the same JSON in a worse container, read back with an HTML parser the tree does not need.

**Document metadata is the host's, not Forma's.** Identity, author, timestamps, revision, title, tags, publish state, visibility and access all belong on the host's row, which Épica's own conventions already mandate. Keeping them out of the tree avoids two sources of truth, keeps a publishing workflow out of a component library, and — decisively — keeps authorization out of a client-authored blob: the document round-trips through the browser, so anything inside it is attacker-controlled, and an editor that can write its own `Access` value is broken access control. A host that wants metadata traveling alongside wraps the tree in its own record.

One shape considered and rejected: **HTML authoritative for rendering with a sidecar tree for editing.** It appears to offer both and is the worst available option — two persisted representations of one document, free to disagree, with no rule for which wins after a hand-edit.

**A text widget hosts `FormaRichTextEditor`.** That is the recursion Coi had through `CKEDITOR.inline`, and it falls out of the plugin model rather than being special-cased: a widget's `Component` is a component, and a component may be an editor. The engine's selection, history and sanitization stay single and shared, so an undo inside a text widget and a widget reorder sit on one stack.

**Drag-and-drop follows the rules `FormaSortableList` already established** — Pointer Events rather than mouse events, press-and-hold before a touch drag so an ordinary swipe still scrolls, and a guarded non-passive `touchmove` so the compositor cannot cancel the gesture mid-drag. Reordering must also be possible from the keyboard; Coi's was mouse-only, and that is not a bar Forma clears by matching.

#### Templates, layouts and themes — deferred, and why

Three separate concerns, and Coi did **not** bundle them under one umbrella:

| Concern | What it is | Coi's surface |
|---|---|---|
| **Theme** | The skin — colors, type scale, spacing tokens | `App_Themes/{Name}/` CSS, 28 of them, chosen through `Website.Theme`; plus `design.aspx` for the background, primary and secondary colors, fonts and site width, and `pops/designs.aspx` as a gallery of skins with desktop and mobile screenshots |
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

### 4.3 Feature plugins

| Plugin | Covers | Coi origin |
|---|---|---|
| `BasicFormattingPlugin` | Bold, Italic, Underline, Strike, Inline code, Subscript, Superscript, Remove formatting | Bold/Italic/Underline/Strike, RemoveFormat |
| `TypographyPlugin` | Font family, `rem` (or point) font size, line-height multiplier, text color, highlight color | Font, FontSize, TextColor, `lineheight` |
| `ParagraphFormatPlugin` | Paragraph, H1–H6, Blockquote, Lead | Format (`p;h1;h2;h3;h4`) |
| `ListPlugin` | Bulleted, numbered, indent, outdent | NumberedList, BulletedList |
| `AlignmentBidiPlugin` | Align start/center/end/justify, direction LTR/RTL | Justify*, BidiRtl, BidiLtr |
| `LinkPlugin` | Link, unlink, anchor, remove anchor; double-clicking one reopens its dialog on it | Link, Unlink, Anchor |
| `ImagePlugin` | An image by address, with alt text and caption; double-clicking one reopens its dialog on it | Image |
| `ExplorerPlugin` | Shipped by `Forma.AddOns.Composer.Plugins.Explorer`: the host's stored files through `FormaExplorer`, which carries `FormaFilePicker` + `FormaFileUploader` in its own toolbar; a chosen picture, video or audio file is embedded, linked or both, any other file linked | `imagelibrary` |
| `VideoPlugin` | A placeholder carrying the address, title and autoplay, which the host resolves when it renders the document — through `FormaVideoPlayer` or otherwise — since the sanitizer discards an `iframe` | `youtube` |
| `TablePlugin` | Insert grid, row/column add and remove, header toggle, delete | Table, tabletools |
| `HorizontalRulePlugin` | Thematic break | HorizontalRule |
| `EmojiPlugin` | Emoji picker with search | `smiley` (image sprites → Unicode) |
| `SpecialCharacterPlugin` | Character palette | SpecialChar |
| `ClipboardPlugin` | Paste as plain text, paste-and-clean from Word, select all | Cut, Copy, Paste, PasteText, PasteFromWord, SelectAll |
| `CodeBlockPlugin` | Embedded code block with language picker and highlighting | — |
| `MarkdownBlockPlugin` | Embedded Markdown block, toggling between source and rendered | — |
| `MathPlugin` | Inline `$…$` and block `$$…$$`, rendered by a registered math capability | — |
| `TemplatePlugin` | Document templates supplied through `TemplatePluginOptions`, inserted at the caret or replacing the document | Templates |
| `EmbedPlugin` | Declarative host-resolved embeds (forms, widgets) | `qform`, safely |

Clipboard note: browsers do not grant programmatic cut/copy/paste without user activation, and the native shortcuts already work. `ClipboardPlugin` therefore contributes paste *transformation* and the explicit "paste as plain text" and "paste from Word" entries, not reimplementations of `Ctrl+C`.

### 4.3a Engine-agnostic plugins

`MaximizePlugin`, `SelectionBarPlugin` and `FileUploadPlugin` are not rich text features: full-viewport editing, the choice of whether a formatting bar follows the selection, and attaching files beside the document are useful to more than one editor, so all three take no dependency on `RichTextEditorPlugin` and live at `Composer/Plugins/`, one level up from the editor-specific ones.

Where each is registered follows what it acts on. Maximizing acts on the composer, so it sits at the root and reaches every editor and every view. Attaching changes the document, and a view never edits it, so it is each editor's default rather than the root's; the selection bar follows formatting a surface must have for it to show anything, so it is a default of the rich text editor alone. An editor registering its own copy is one surface's plugin, never a second one beside the first, because only one surface is in play at a time and a surface's own registration answers in place of the composer's.

| Plugin | Covers | Coi origin |
|---|---|---|
| `MaximizePlugin` | Full-viewport editing | Maximize |
| `SelectionBarPlugin` | Showing or hiding the bar that follows the selection | — |
| `FileUploadPlugin` | Uploading files from the author's device and attaching them beside the document (§3.7), gated by `FileUploadPluginOptions` | — |

All three commands are also registered as **engine** commands (`ComposerCommands.Maximize`, `ComposerCommands.SelectionBar`, `ComposerCommands.Attach`) on `FormaComposer` itself, which owns the maximized and selection-bar state and the attachments dialog because it, not any one editor, renders the chrome that changes.

The selection bar itself sits beneath the selection, moving above it only when the viewport leaves no room beneath, so it never covers the line being read or the toolbar above the first line. Its handle drags it, or arrow-keys it, to wherever the author wants it, and the bar keeps that displacement from one selection to the next. `ComposerCommandRegistry.ExecuteAsync` resolves an engine command before ever consulting a plugin, so each plugin's `ExecuteAsync` is unreachable by design — the plugin exists only to contribute the toolbar entry that names the engine's own command.

### 4.4 Plugin options

Options follow the repository's existing convention — a `sealed record` with `init` properties and a `Default`, per plugin, named for it: `TypographyPluginOptions` chooses which sizes and families are offered, `TablePluginOptions` caps dimensions, `ExplorerPluginOptions` gates upload and chooses how a chosen file is inserted. A plugin with a single flag keeps a plain property instead.

### 4.5 Presets are components, not subclasses

Three components differing only in which plugins they register, each taking the engine's `ComposerOptions`. None derives from another; they compose the same engine with different plugin sets, so a preset can never drift from the editor it presents.

| Component | Plugin set | Replaces |
|---|---|---|
| `FormaComposer` | Everything registered | Coi `Website` |
| `FormaStandardRichTextEditor` | Formatting, typography, paragraph, lists, alignment/bidi, link, image, table, horizontal rule | Coi `Basic` |
| `FormaCompactRichTextEditor` | Formatting, typography, alignment/bidi, link | Coi `Mini` |

The two presets live in `RichTextEditor/` beside the editor they present, not at the category root: each is a rich text surface with a narrower plugin set, so they belong with `FormaRichTextEditor` rather than with the engine. `FormaComposer` stays at the root because it is the engine, and it presents whatever is registered rather than any one editor.

The `Forma` prefix is required on all three: the repository's naming rule puts it on every component tag.

---

## 5. Conversion

### 5.1 One seam, not one per format

```csharp
public sealed record ComposerDocumentFormat
{
    public ComposerDocumentFormat(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.ToLowerInvariant();
    }

    public string Id { get; }

    public static ComposerDocumentFormat Html { get; } = new("html");
    public static ComposerDocumentFormat Markdown { get; } = new("markdown");
    public static ComposerDocumentFormat PlainText { get; } = new("text");
    public static ComposerDocumentFormat WidgetTree { get; } = new("widget-tree");
}

public interface IComposerDocumentConverter
{
    ComposerDocumentFormat Source { get; }

    ComposerDocumentFormat Target { get; }

    ComposerConversionResult Convert(string content);
}

public sealed record ComposerConversionResult
{
    public required bool Succeeded { get; init; }

    public string? Content { get; init; }

    public IReadOnlyList<string> UnsupportedFeatures { get; init; } = [];
}
```

`IComposerDocumentConverter` rather than `IComposerMarkdownConverter` because Markdown is not the last format the composer will meet — LaTeX, plain text and whatever a host adds all fit the same shape, and a registry keyed on `(Source, Target)` resolves them uniformly. The extra cost over a Markdown-specific interface is one record and one lookup.

**`ComposerDocumentFormat` is a string-keyed value, not an enum**, so a host or a satellite mints a format without the library changing: `new ComposerDocumentFormat("latex")`, an editor plugin declaring it, a converter registered for `(latex, html)`, and the switcher offers it. The statics are conveniences for the formats Forma ships a surface for, not the set of legal values.

It is a record **class** rather than a record struct because a struct's `default` would bypass the constructor and carry a null `Id` into the converter registry's keys, where it would compare and hash as a legitimate format and then throw on first read. The statics are cached rather than expression-bodied for the same reason the constructor exists: each one otherwise re-validates and re-lowercases on every access.

The engine therefore never `switch`es on a format. `ComposerAssetKind` (§3.2) is a closed enum because the loader must know how to load each kind, whereas a format the engine only ever hands to a registry lookup needs no such knowledge. Behavior comes from what is registered, which is what makes "no converter, no editor" (§3.2) fall out rather than have to be enforced.

**`widget-tree` has no converter in either direction, and needs none.** Converting into the designer would mean parsing HTML back into widgets, and converting out would flatten the tree into HTML and discard it — both refused for the reasons §4.2 gives. Neither is a rule the engine enforces: the registry simply holds no entry, so the switcher never offers the conversion. Producing HTML *from* a tree is rendering rather than conversion, and §4.2 says where it lives.

### 5.2 Markdown to HTML always; HTML to Markdown only when it is lossless

Markdown → HTML is total and always available. **HTML → Markdown is offered only when the HTML happens to be within the subset Markdown can represent.** The converter inspects the document first and, when it finds anything Markdown cannot carry, refuses with `Succeeded = false` and names what blocked it.

Markdown has no representation for font family, font size, text color, highlight, line height, underline, alignment or per-block `dir` — every one of which §1.3 promises as a headline feature. Silently discarding an author's formatting on an editor switch is worse than declining to switch.

The refusal is surfaced, not swallowed: the switcher disables the Markdown editor and explains which features are in the way, so the author can remove them and try again.

`Forma.AddOns.Resources.Markdown` is **not** reused for this. It exists to render a stored `.md` resource for viewing, so its pipeline calls `DisableHtml()` and sanitizes the output — one-way by construction. `Forma.AddOns.Composer.Markdown` is a separate converter over the same Markdig dependency.

---

## 6. Security

An editor that accepts HTML, offers a code editor over raw markup, and pastes from Word is a stored-XSS surface. Coi got this wrong with `allowedContent = true`.

```csharp
public interface IHtmlSanitizer
{
    IMarkupPolicy MarkupPolicy { get; }

    string Sanitize(string html);
}
```

- Forma ships `AllowlistHtmlSanitizer` in `Forma.Framework.Sanitization` with no external dependency, covering the element and attribute set the built-in plugins can produce. The library registers it as the default when a host has registered none, so a composer is never unsanitised.
- A host may replace it — a satellite wrapping Ganss.Xss suits a host wanting a hardened, widely-reviewed implementation, and any registration overrides the default.
- A host wanting only different *rules* rather than a different implementation supplies an `IMarkupPolicy` to the sanitizer instead, wrapping `RichContentMarkupPolicy.Default` to widen or narrow what it permits. Event handler attributes are refused by the sanitizer itself, so no policy can re-admit them.
- Sanitization runs at **four** points, and none is optional: content loaded into the composer, content pasted in, content leaving the code editor, and content raised to the host. In a widget tree that means every node's `Content` (§4.2); the settings JSON is never sanitized as markup.
- The composer's guarantee stops at the browser. **The host must sanitize server-side on save regardless**, because a client-side editor cannot be trusted by the endpoint receiving its output.

`IComposerSyntaxHighlighter` returns tokens rather than markup for the same reason — see §7.

---

## 7. Syntax highlighting

```csharp
public readonly record struct ComposerSyntaxToken(int Start, int Length, string Scope);

public interface IComposerSyntaxHighlighter
{
    IReadOnlyList<string> SupportedLanguages { get; }

    IReadOnlyList<ComposerSyntaxToken> Tokenize(string code, string language);
}
```

Returning **tokens, not HTML**. A highlighter that returned `"<span class=…>"` would put the burden of HTML-encoding the author's code on every implementation, and one that forgot would execute `<script>` inside a code block. With offsets and scopes the component emits the text through normal Razor encoding, so an implementation *cannot* introduce markup.

Scopes map to `.forma-tm-*` classes: `keyword`, `string`, `comment`, `function`, `type`, `variable`, `number`, `operator`, `punctuation`.

**TextMate grammars need an Oniguruma-compatible engine.** Real `.tmLanguage.json` files use `\G`, `\h`, possessive quantifiers, inline `(?i)` scoping, and `end` patterns back-referencing `begin` captures — none of which .NET's `Regex` supports. `Forma.AddOns.Composer.TextMate` therefore wraps **TextMateSharp**, which ships Oniguruma bindings and a grammar registry, rather than attempting a tokenizer over `System.Text.RegularExpressions`.

Without any highlighter registered, code blocks render as plain monospaced text. Highlighting is an enhancement, never a prerequisite.

---

## 8. Accessibility, touch and direction

These are correctness requirements for every Forma component, and the composer is the hardest case in the library — a dense toolbar plus a `contenteditable` surface.

- **Tab order:** the composer is one stretch of the page's tab order arranged its own way: the canvas first, then its toolbar and status strip, then out to whatever follows the composer. Tab from the canvas therefore reaches this composer's commands, never the next composer on the page, and Shift+Tab from the canvas leaves the composer.
- **Toolbar semantics:** `role="toolbar"` obliges arrow-key roving focus between its controls, which `FormaToolbar` provides through the shared `Focus` behavior; the composer's command bar composes it and adds no key handling of its own. The editor switcher is a `FormaTabs` strip — a `tablist` whose tabs are the editors and, trailing, the active editor's views.
- **Editing surface:** the `contenteditable` region is labeled, exposes `aria-multiline`, and announces the active block and formatting through a live region — a screen-reader user must be able to learn that the caret is inside a heading or a code block.
- **Every state without color:** an active toolbar toggle carries `aria-pressed` and a persistent non-color mark, not a tint alone.
- **Touch:** every control is at least 24×24 CSS px, and 44×44 under `@media (pointer: coarse)`. Every `:hover` rule sits inside `@media (hover: hover)`. The selection bar is clamped to the viewport and reachable by tap; nothing essential hangs off hover.
- **Direction:** logical properties only, per-block `dir`, and horizontal arrow keys resolved through `CultureInfo.CurrentCulture.TextInfo.IsRightToLeft`. Bidi is not a plugin's problem to solve twice.

---

## 9. File layout

Every folder and its namespace stay in step, so `Plugins/` and `Dialogs/` are real namespace segments.

**The composer projects are foldered by concern rather than flat** — assets, documents, editing, plugins, syntax, widgets — and the two assemblies use the same folder names, so a contract in the extensibility package and the engine's implementation of it sit under the same folder name in their own projects and the test tree mirrors both.

**A plugin joining the composer to another component is an AddOn of its own**, `Forma.AddOns.Composer.Plugins.{Component}`, borrowing the composer plugins' namespace as an AddOn borrows what it extends. The stored-files plugin and its dialog body are `Forma.AddOns.Composer.Plugins.Explorer`, with their own strings beside them, so the composer never depends on the explorer, the explorer never depends on the composer, and a host wanting both adds the one package that joins them.

**Localization follows `FormaExplorer`:** the composer's package is its own category and carries one locale pair at its root, sectioned by the component each part reads through. Every component in the package, including those in sub-namespaces, names that component with `LocalizationSourceType` — `FormaComposer` for the engine, its bars, its dialogs and the editor canvases, `FormaRichTextEditor` for the rich text selection bar and views — and its strings sit in that component's section, nested as the component's namespace is. Resources resolve by the named type's full name, so a string filed under the section of a component other than the one its reader names silently falls back to its English key; that is the failure mode to re-verify whenever a type moves or renames. A plugin's titles are the exception, read through the locale the plugin names and then the composer's (§3.1), and an AddOn carries its own pair.

---

## 10. Decisions

| # | Question | Answer |
|---|---|---|
| C1 | Which external libraries may the composer bundle? | Any, provided all five conditions in §3.3 hold. The rule is a standing carve-out by category, not a named list, so Markdig, TextMateSharp and KaTeX all qualify without a per-library approval. |
| C2 | How are the composer's contracts named, given `Ganss.Xss.IHtmlSanitizer` may be in a host's scope? | Everything the composer owns carries the prefix: `IComposer*` for contracts, `Composer*` for types. Sanitization is not among them — it is a framework concern named for what it does, so a host registering both sanitizers aliases one of the two. |
| C3 | Should there be a plain-text editor plugin, or is plain text only ever a conversion target? | Both. `ComposerDocumentFormat.PlainText` is a format any converter may target, and `PlainTextEditorPlugin` authors it over a `<textarea>` as the editor the catalog falls back to when no registered editor is available, so a composer always has a surface. |
| C4 | What does `@bind-Value` carry when the active editor's format is not the bound format? | A `ComposerDocument` — the content together with the format it is written in (§3.7). A bare string would force every document through HTML and rewrite a Markdown author's source on each round-trip. The format is a string-keyed value rather than an enum, so a new format costs the library nothing. |
| C5 | Are `TemplatePlugin`'s templates host-supplied or does Forma ship a set? | Host-supplied, through `TemplatePluginOptions`; the default options carry two sample layouts a host replaces with its own. A template is editorial content and Forma has no editorial opinion. |
| C6 | What *is* the page designer's saved document? | A `ComposerWidgetTree` — JSON, with HTML as output rather than storage (§4.2). Coi's saved HTML froze rendering decisions at save time and never escaped the tree anyway, since recovering a widget needs `data-widget` and `data-settings` attributes. Columns are nodes, not an array on the row. Document metadata stays on the host's row. |
| C7 | One designer plugin or several, given Coi drove seven targets through `OpenTemplate`'s numeric type? | One, varied by its options and the registered widget set. The palette is already extensible, so a second plugin would only re-register widgets. |
| C8 | Who owns layout and theme? | Not the composer. `FormaLayout` and `FormaTheme` are library-wide components built first and composed afterwards; the designer ships template-only and nothing in the composer waits on them. Both want their own catalog entries. |
| C9 | May a designer be placed on a designer canvas? | No. `PageDesignerPlugin` implements both `IComposerEditorPlugin` and `IComposerDesignerPlugin`, so the placeable set is "any editor plugin that is not also a designer plugin" — a type test, and a designer excludes itself. |
| C10 | How does an editor choose between the shared toolbar row and one of its own? | By the row each item names through `ComposerToolbarItem.RowId` (§3.8). Both editors and external plugins declare rows through `Rows`, so there is one mechanism rather than a separate registration path per category. |
| C11 | Are gutter markers and status readouts pushed or pulled? | Pushed, through a handle the plugin mutates and disposes. Toolbar items stay static lists. This matches VS Code's `createStatusBarItem` and decoration types, and the dispose pattern its API reference states as the general rule (§3.8). |
| C12 | Where do the common editing operations live? | In the engine, as `ComposerText` returning `ComposerEdit` (§3.4). Word-at-caret, selected text, range replacement, per-line mapping and replace-all are needed by every editor and every plugin that touches text; leaving them out means each one reimplements them against raw strings. |
| C13 | Why do the contracts split from the engine (§2)? | Because a plugin may ship from another package, and what such a package extends has to be a package itself or both carry a copy of it. Only the contracts go: publishing the catalog, the command registry and the history would make engine internals public API for callers who never touch them. |
| C14 | What decides which editors and views a plugin applies to (§3.1a)? | Where the host registers it. A plugin from another package cannot name an editor whose type it cannot reference, and the host is the only party that knows what it installed. This retires a feature's supported-editor list, its view opt-in, and the editor id a view carried. |
| C15 | May the composer scan assemblies for plugins? | No. Scanning cannot decide scope once the registration tree is what declares it, so every scan would assert a placement nothing checked. Naming a plugin costs a host no more than naming its assembly, and an editor's standard set comes from its own defaults instead. |
| C16 | How does a plugin from another package open a dialog and put something into the document? | It supplies a body and the engine supplies the shell (§3.4a), and the result travels as a `ComposerInsertion` handed to the active editor. The editor answering false is the whole compatibility check, so no capability contract is needed to describe what an editor accepts. |
| C17 | Does a plugin instance belong to the host or to the composer? | To the composer. Registration records a type and its configuration; each composer builds, owns and disposes its own set, because an editor plugin carries events its component subscribes to and a shared one would answer another composer's commands. |
| C18 | Where does registering, building and disposing plugins live? | In `Forma.Framework.Plugins`, generic over the plugin contract a feature declares, since none of it names a composer concept. The composer's builders keep only the editor, feature and view roles (§3.1a). |
| C19 | Whose defaults does an editor get? | Its own. The editor builder is typed by its editor, so each editor's package extends it with defaults of its own and one editor's defaults cannot be called on another. The composer's defaults are what belongs on every surface, which is maximizing; attaching is each editor's, since a view never edits the document (§4.3a). |
| C20 | How does a capability that joins the composer to another component ship? | As an AddOn of its own, `Forma.AddOns.Composer.Plugins.{Component}` (§9), so neither component's package depends on the other and a host wanting both adds the one package that joins them. |
| C21 | Where does a plugin from another package keep its labels' localization? | With itself. A plugin names its localization source through `LocalizationSourceType`, and the composer reads a title from it before its own, so a shared group keeps the composer's word while the plugin's own commands carry theirs (§3.1). |
