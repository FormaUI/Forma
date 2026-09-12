# Forma Component Roadmap

**26 components not yet implemented, 1 in progress.** Each moves to [the component catalog](component-catalog.md) when it is approved; the structure lists the ones not yet started, and one in progress stands in the catalog's structure until then.

## Components Structure

```text
Forma.Blazor/Components/
├── (10) Buttons/
│   ├── FormaAddButton
│   ├── FormaDeleteButton
│   ├── FormaDuplicateButton
│   ├── FormaEditButton
│   ├── FormaExportButton
│   ├── FormaPublishButton
│   ├── FormaRefreshButton
│   ├── FormaShareButton
│   ├── FormaUpdateButton
│   └── FormaViewButton
├── (4) Editors/
│   ├── FormaFilterEditor
│   ├── FormaNotesEditor
│   ├── FormaPermissionMatrix
│   └── FormaProductOptionsEditor
├── (1) Feedback/
│   └── FormaCapacityIndicator
├── (2) Files/
│   ├── FormaFileManager
│   └── FormaMediaPicker
├── (1) Inputs/
│   └── FormaCurrencyInput
├── (3) Media/
│   ├── FormaDesignPicker
│   ├── FormaFontPicker
│   └── FormaImageEditor
├── (2) Menus/
│   ├── FormaContextMenu
│   └── FormaSortMenu
├── (1) Navigation/
│   └── FormaCategorySelector
└── (1) Windows/
    └── FormaLaunchBar
```

## Build order

The components still to build, in the order dependency and reuse suggest.

| Order | Components | Rationale |
|---|---|---|
| 1 | `FormaCurrencyInput` | Form inputs — needed before building any edit forms |
| 2 | `FormaCapacityIndicator` | Data display — needed for list pages |
| 3 | `FormaCategorySelector` | Interactive widgets — needed for editor pages |
| 4 | `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built |
| 5 | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors — built alongside their specific pages |
| 6 | `FormaDeleteButton`, `FormaUpdateButton`, `FormaEditButton`, `FormaAddButton`, `FormaDuplicateButton`, `FormaRefreshButton`, `FormaShareButton`, `FormaPublishButton` | The rest of the icon-button vocabulary — the same thin wrapper over `FormaButton`, one glyph each, so a row's actions stop being ad-hoc markup per page |
| 7 | `FormaContextMenu` | The menu panel of `FormaOverflowMenu` opened by right-click, once there is a menu worth opening that way |
| 8 | `FormaMediaPicker` | The second type-restricted skin over `FormaFilePicker`, beside `FormaImagePicker` |
| 9 | `FormaSortMenu`, `FormaViewButton` | The two controls that exist for the managers' tile grids — a layout with no column headers to sort by and two layouts to switch between. Neither has a surface to serve until `FormaFileManager` exists |
| 10 | `FormaFileManager` | The type-agnostic browsing surface beside `FormaExplorer`; a shared browsing half between them is worth extracting once both exist |
| 11 | `FormaLaunchBar` | The bar a workspace is launched from, hosting `FormaRestArea` beside its launchers, so the windows and the way back to them stand in one place |
| 12 | `FormaComposer` | The largest component, and last of the composing ones — an editor engine rather than a single control, with every capability and every editing surface arriving as a plugin. It replaces both halves of Coi's authoring stack: the CKEditor rich text field *and* the `template.aspx` page designer. Its chrome is `FormaSelect`, `FormaDropDownMenu` and `FormaColorPicker`, it delegates image insertion to `FormaExplorer`, uploads through `FormaFilePicker` and `FormaFileUploader`, and inserts `FormaVideoPlayer`. It runs to eight phases of its own — see [the composer implementation plan](composer-implementation-plan.md) |
| 13 | `FormaImageEditor` | The heaviest media component and the last — it needs the surfaces above to launch it, and its pointer handles, coordinate mapping and keyboard path are worth building once the rest of the media flow is settled |

## Components

### `FormaAddButton`

| | |
|---|---|
| **Glyph** | A **plus** |
| **Old pattern** | The "new" button in each page's toolbar, opening the new-item modal |
| **Used in** | `FormaToolbar` actions, `FormaMultiSelect`'s add-new affordance, and any editor that appends a row |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, `RenderFragment? ChildContent` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the plus glyph. The one of the set that is **not** always icon-only: a primary "add" is usually labelled in place ("New article"), so it takes optional `ChildContent` and drops `IconOnly` when given it, where the others stay square |

### `FormaCapacityIndicator`

| | |
|---|---|
| **Old pattern** | `<b>קיבולת:</b> <span dir="ltr">X / Y</span>` — shows used vs. allowed count |
| **Used in** | pages, products, images, orders, leads, users (6 pages) |
| **Parameters** | `int Used`, `int Max`, `string? Label` |
| **Blazor approach** | Static SSR. Optionally add a progress bar visual. Change color when approaching limit |

### `FormaCategorySelector`

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |
| **Parameters** | `@bind-SelectedIds` (List<int>), `IEnumerable<Category> AvailableCategories` |
| **Blazor approach** | Two `<ul>` lists side by side. Click item → move between lists (C# list manipulation + re-render). CSS animation via `transition` on list items. No external JS |

### `FormaComposer`

> **[composer-design.md](composer-design.md) is authoritative for this component.** The rows below are the summary; the design document owns the plugin model, the assembly layout, and the phases.

| | |
|---|---|
| **Old pattern** | **Two Coi surfaces, not one.** The rich text editor was CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px; custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`; `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction, and `allowedContent = true` — no content filtering whatsoever. The larger surface was the **page designer**, `admin/pops/template.aspx` over `Controls/Edit/`: a drag-and-drop page builder with a side palette, a live canvas, per-element delete/style/edit chrome, jquery-sortable reordering, column and element resizing, and a settings editor per element kind in `elm.js`. One entry point, `OpenTemplate(type, title)`, drove seven targets by numeric type — product page templates, the product tile, headings, rich content, popups and the custom dropdown menu |
| **Used in** | page editor, product description, product and category templates, popups, custom menus — and in Épica, the News article body |
| **Toolbar (from Coi's `Website` preset)** | Source · Preview · Templates — Cut/Copy/Paste/PasteText/PasteFromWord · Undo/Redo · SelectAll · RemoveFormat — Smiley · SpecialChar · LineHeight · Maximize — Bold/Italic/Underline/Strike — Numbered & bulleted lists — Justify right/center/left/block · BidiRtl/BidiLtr — Link/Unlink/Anchor — YouTube embed · Image · Table · HorizontalRule — Format · Font · FontSize — TextColor — image library. Subscript/Superscript were explicitly removed; Forma keeps them available but outside the default preset |
| **Designer palette (from `template.aspx`)** | Text, image, Facebook, contact form, table/cells, horizontal rule, button, image slider, add-ons, image gallery, YouTube/video, map, spacer, code/script — plus host-domain token elements swapped in per target: product name, summary, images, price, details, body, options, buy button and quantity; the mini-product tile variants; and page name, summary, image and read-more for a category tile |
| **Parameters** | `@bind-Value` (`ComposerDocument` — content plus the format it is written in), `ComposerOptions Options`, the registered plugin set, and `EventCallback`s for media requests and document change. Presets are separate components rather than a toolbar enum: `FormaComposer` (everything), `FormaStandardRichTextEditor`, `FormaCompactRichTextEditor` |
| **Blazor approach** | **Forma's own editor, and an engine rather than a single component.** Every capability is a plugin, *including each editing surface* — `RichTextEditorPlugin`, `MarkdownEditorPlugin`, `CodeEditorPlugin` and `PageDesignerPlugin` implement the same `IComposerEditorPlugin`, so there is no closed set of modes and a host can add its own. The designer adds a second extension point, `IComposerDesignerPlugin` contributing `ComposerWidget`s, because half Coi's palette was host-domain and Épica's would be too. **A text widget hosts `FormaRichTextEditor`**, which is the recursion Coi had through `CKEDITOR.inline` and the reason the component is called a composer at all. Chrome composes the library: `FormaSelect` for format/font/size, `FormaDropDownMenu` for overflow, `FormaColorPicker` for colour, `FormaExplorer` for stored media, `FormaFilePicker` plus `FormaFileUploader` for uploads, `FormaVideoPlayer` for embeds. Contracts and engine live together in `Forma.Framework.Composer`, which the library takes because a composer cannot render without it, and satellites (`.Markdown`, `.TextMate`, `.KaTeX`) carry the external dependencies one package each. Undo/redo, selection, clipboard, conversion, sanitisation and asset loading are engine concerns shared by every editor — so an undo inside a text widget and a widget reorder sit on one stack |
| **Designer document** | A **`ComposerWidgetTree` — JSON, with HTML as output rather than storage.** Each node carries an `InstanceId`, the `WidgetId` of its kind, canvas-edited `Content`, panel-edited `Settings` as an opaque `JsonElement`, and `Children`; columns are nodes rather than an array on the row. Coi saved HTML plus a scoped `<style>` block, which froze rendering decisions at save time — a baked `<img src>` and `style="width:60%"` that a CDN move or theme change could never reach — and never escaped the tree anyway, since recovering a widget needs `data-widget` and `data-settings` attributes read back with an HTML parser. `FormaWidgetRenderer` walks the tree for public pages under static SSR. Document metadata — identity, author, timestamps, publish state, access — stays on the host's row, not least because a client-authored blob cannot be trusted with authorization |
| **Conversion** | Markdown → HTML is always available. **HTML → Markdown is offered only when the document is within the subset Markdown can represent**, and is refused with the blocking features named otherwise — Markdown cannot carry font family, size, colour, highlight, line height, underline, alignment or per-block `dir`, every one of which this component offers. Silently discarding an author's formatting on a switch is worse than declining the switch |
| **Security** | Coi's `allowedContent = true` made the editor a stored-XSS pipe. Sanitisation through `IHtmlSanitizer` is mandatory and runs on load, on paste, on leaving the code editor and on raising content to the host; a host may swap the built-in allowlist for a hardened implementation. `IComposerSyntaxHighlighter` returns **tokens, not markup**, so a highlighter cannot introduce script. `qform`'s `<script src>` injection is replaced by a declarative host-resolved embed |
| **External JS** | This component carries the library's standing carve-out from the JS-free rule, granted by category rather than per library: an editor hosts capabilities — Markdown rendering, maths typesetting, syntax highlighting — that are solved problems nobody should reimplement. Each lives in its own `Forma.AddOns.Composer.{Capability}` satellite behind a plugin contract, ships as static assets under `_content/…` with no bundler, stays in a supporting role, and degrades to absent. Markdig, KaTeX and TextMateSharp all qualify — TextMate because `.tmLanguage.json` needs an Oniguruma-compatible engine that `System.Text.RegularExpressions` is not. The editing surface, selection, undo stack and toolbar stay Forma's, in C# |

### `FormaContextMenu`

| | |
|---|---|
| **Old pattern** | None — Coi had no right-click menu; this is new capability rather than parity |
| **Used in** | Grid rows and media tiles, as a shortcut to the actions the overflow menu already lists |
| **Parameters** | `RenderFragment ChildContent` (menu items), `RenderFragment Target` (what right-clicking opens it), `bool Disabled` |
| **Blazor approach** | Suppresses the native menu on `@oncontextmenu` and opens `FormaDropDownMenu`'s panel at the pointer through the shared `AnchoredPopover.js`, so the same clipping, flipping and viewport clamping apply. Must answer the context-menu key and `Shift+F10` as well as the pointer, and a touch user has no right-click at all — so it stays a **shortcut** to actions reachable elsewhere, never the only route to one |

### `FormaCurrencyInput`

| | |
|---|---|
| **Old pattern** | `.form-addon` wrapping input with currency symbol overlay |
| **Used in** | product (price, sale price, cost), store-settings (min order, shipping), coupons (discount amount) |
| **Parameters** | `@bind-Value` (decimal), `string Symbol = "₪"`, `string? Label` |
| **Blazor approach** | `InputNumber` inside a flex container with the currency symbol as an adjacent `<span>`. CSS for visual grouping |

### `FormaDeleteButton`

| | |
|---|---|
| **Glyph** | A **trash can**. Not the cross — a cross reads as *dismiss this from view*, and borrowing it for an irreversible action is how a user deletes something they meant to close |
| **Old pattern** | The delete icon in each list page's action column, guarded by `AreYouSure(this, 'message')` before the postback |
| **Used in** | Row actions on every list page, and the destructive action in an editor's footer |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | The same thin wrapper over `FormaButton` as `FormaCloseButton` and `FormaEyeButton`, supplying the trash glyph and no CSS of its own. Confirmation stays the caller's job through `IConfirmDialogService` — a button that confirms on its own cannot be composed into a bulk action that confirms once. **To settle when built:** whether it pins `Danger`, which is right for a lone destructive button but loud repeated down every grid row, or stays `Ghost` and leaves emphasis to the confirm step |

### `FormaDesignPicker`

| | |
|---|---|
| **Old pattern** | Grid of design template thumbnails (desktop + mobile preview overlay on hover). Click → confirmation dialog → apply design |
| **Used in** | designs popup, welcome wizard |
| **Parameters** | `IEnumerable<DesignTemplate> Templates`, `EventCallback<int> OnSelect` |
| **Blazor approach** | CSS grid of cards with image + hover overlay. Confirmation via `FormaConfirmDialog` before applying |

### `FormaDuplicateButton`

| | |
|---|---|
| **Glyph** | **Two overlapping sheets** — the copy glyph. Deliberately not a second pencil, so "make another one" can never be misread as "change this one" |
| **Old pattern** | None in Coi's action columns |
| **Used in** | Row actions on list pages — Épica names duplicate in the same breath as edit, publish, delete and view |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | The same thin wrapper over `FormaButton` as the rest of the set. What duplication means — deep copy, draft status, title suffix — belongs entirely to the caller |

### `FormaEditButton`

| | |
|---|---|
| **Glyph** | A **pencil** |
| **Old pattern** | The edit icon in each action column, linking through to the full editor page |
| **Used in** | Row actions on every list page; starts `FormaDataGrid`'s inline edit where the row is edited in place |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the pencil glyph, and nothing else — whether the click navigates or calls `BeginEdit` is the caller's decision |

### `FormaExportButton`

| | |
|---|---|
| **Old pattern** | Confirm dialog, then `WaitForDownload` navigates straight to a per-entity `.aspx` under `admin/services` that builds a workbook with EPPlus, saves it to a public `up/tmp` path and redirects to it. Generation is synchronous inside that request; a `dwait` cookie set alongside the redirect tells a 500 ms poll to hide the spinner, so nothing waits for readiness before downloading. Columns, headers and the query are hardcoded per entity, and the export always covers every row |
| **Used in** | orders, leads |
| **Parameters** | `string ExportUrl`, `string Label = "ייצוא"` |
| **Blazor approach** | Button triggers server-side export. Use `NavigationManager` or JS interop `window.location.href` to initiate download. Consider `StreamRendering` for progress indication. Stream the file from the authorized response rather than parking it under `wwwroot`, which exposed personal data at a guessable URL |

### `FormaFileManager`

| | |
|---|---|
| **Old pattern** | `pops/images.aspx` again — its *management* half: create, rename and delete folders, move files between them, delete in bulk, watch the capacity indicator |
| **Used in** | The document library and any administrative view over stored files that is not specifically media |
| **Parameters** | `IReadOnlyList<ResourceInfo> Resources`, folder navigation callbacks, `EventCallback<ResourceInfo> OnRename`, `EventCallback<IReadOnlyList<ResourceInfo>> OnDelete`, `EventCallback<FolderMove> OnMove`, `EventCallback OnFolderCreate`, `IFileStore? Store` |
| **Blazor approach** | A general file manager over what is **already on the server**, in the shape of Explorer or Finder: a breadcrumb, a folder tree, a list or tile view of resources with name, size and date, selection, and `FormaPagination`. Type-agnostic, so it previews whatever a resource turns out to be through `FormaFileViewer`. New files arrive through `FormaFilePicker` plus `FormaFileUploader` in its toolbar. It owns none of the storage, reporting every mutation as intent through a callback |
| **Initial release** | Browse, navigate folders, select, preview, upload, rename, delete, create folder. Move, bulk operations and search come later |
| **Notes** | It shares its browsing half with `FormaExplorer` — breadcrumb, resource grid, selection, pagination and the picker-plus-uploader toolbar. `IExplorerResourcesProvider` is the shape to copy when this is built, and it deliberately stays in the explorer feature rather than in `Forma.Framework.Resources`: a browsing contract was moved down there once and immediately leaked folders into an assembly that models resources alone. **To settle when built:** whether a shared contract is extracted once there are genuinely two consumers, or the two surfaces keep their own |

### `FormaFilterEditor`

| | |
|---|---|
| **Old pattern** | Card-based filter groups, each with a title and sortable option values inside. Add/remove groups and values, search, dropdown filter by group type |
| **Used in** | filters popup |
| **Parameters** | `@bind-Filters` (List<Filter>) |
| **Blazor approach** | Interactive card grid. Each card is a filter group with `FormaSortableList` of values inside. Reuses `FormaTextBox` for inputs |

### `FormaFontPicker`

| | |
|---|---|
| **Old pattern** | Google Fonts grid with Hebrew sample text preview, click-to-select |
| **Used in** | design settings, page editor |
| **Parameters** | `@bind-Value` (string font name), `IEnumerable<FontInfo> Fonts` |
| **Blazor approach** | Searchable grid inside `FormaDialog`. Each cell shows font name + sample text styled with that font. Load font CSS on-demand |

### `FormaImageEditor`

| | |
|---|---|
| **Old pattern** | Two surfaces, not one. `jcrop` in `slider.aspx` did a **constrained crop-to-fit**: aspect ratio and minimum size both pinned to the destination slot, the image scaled into a fixed 590×350 box, an initial selection set for the editor, and `tellSelect()` posting `x/y/w/h` plus the target dimensions to `crop.asmx`. Separately, `pops/pixie.aspx` hosted the commercial **Pixie** editor in an `<iframe>`, launched per-entry from the image library |
| **Used in** | Making an arbitrary upload fit a fixed slot — article covers and OG images, video thumbnails, sponsor logos, gallery tiles. Launched from `FormaFileViewer`, the manager and the picker, the same way Coi launched it from its library |
| **Parameters** | `string Source` (required), `double? AspectRatio`, `IReadOnlyList<AspectRatioPreset>? Presets`, `int? MinWidth`, `int? MinHeight`, `int? OutputWidth`, `int? OutputHeight`, `EventCallback<ImageEdit> OnApply`, `EventCallback OnCancel` |
| **Scope** | **In:** crop with free or locked ratio and named presets, rotate in 90° steps, flip horizontal and vertical, resize to target dimensions, and zoom for precision. **Out:** filters, text, stickers, drawing, shapes, gradients and layers — that is Pixie's tier, it is a canvas compositing app rather than a control, and reaching it by pulling in a library would spend the one external-JS exception this repo has deliberately not spent twice |
| **Blazor approach** | Follow Coi's actual division of labour, which already matches this library's boundary: **the component describes the edit, the host performs it.** `OnApply` emits an `ImageEdit` — crop rectangle, rotation, flips, target size — and the host renders it with ImageSharp or SkiaSharp, exactly as `FormaFileUploader` leaves the destination to an `IFileStore` and the managers leave every mutation to a callback. A colocated `.razor.js` drives the handles and the live preview through Pointer Events and Canvas 2D; that is hand-written interop for behaviour only script can provide, not an external library |
| **Why not export the canvas** | Returning the finished bitmap from the browser looks simpler and is the wrong trade under Interactive Server: a base64 canvas export of a multi-megabyte photo crosses the SignalR circuit, where the default 32 KB receive limit rejects it outright, and re-encoding through the browser discards EXIF, ICC colour profiles and the original's quality. A rectangle is a few numbers |
| **The two bugs to design out** | **Coordinate space** — report the rectangle in the image's *natural* pixels, never the displayed CSS pixels, because the editor scales the image to fit its box just as Coi's 590×350 did, and a crop that forgets the scale factor is wrong by exactly that ratio. **EXIF orientation** — a phone photo displays upright through `image-orientation` while its stored pixels are rotated, so coordinates taken from the display do not address the bytes the host will cut |
| **Notes** | Neither jcrop nor Pixie is reachable without a mouse, and this repo does not ship that: the selection needs arrow-key nudging with a modifier to resize, numeric width/height fields as an exact path, and a live region announcing the rectangle as it changes. The handles are also the hardest touch target in the library — 44×44 under a coarse pointer around a rectangle whose corners may sit at the image edge, needing press-and-hold to start a drag so an ordinary swipe still scrolls. **To settle when built:** whether straightening by an arbitrary angle joins the 90° steps, since it forces canvas resampling and a bounds recalculation that quarter-turns avoid entirely |

### `FormaLaunchBar`

| | |
|---|---|
| **Old pattern** | None — Coi opened each tool as its own page; launching applications into a workspace of windows is new capability rather than parity |
| **Used in** | The Admin Console's shell, as the bar its applications are launched from |
| **Notes** | Owns application launching, which the window design leaves outside `FormaWindow` and `FormaRestArea`, and may host the rest area as one of its parts so a window's handle sits beside the launcher that opened it |

### `FormaMediaPicker`

| | |
|---|---|
| **Old pattern** | None — Coi's library was images-only, and video arrived as a pasted YouTube URL |
| **Used in** | Surfaces taking either an image or a video off the device — Épica's Videos module, article media, and `FormaComposer` |
| **Parameters** | The same set as `FormaFilePicker`; its `Options.Accept` is frozen |
| **Blazor approach** | The same thin skin as `FormaImagePicker` with a wider pin: image, video and audio types. Preview per accepted file goes through `FormaFileViewer`, so a picked video shows as a player rather than a filename |
| **Notes** | Exists so "media" is one decision made once, rather than each caller writing its own `accept` string and drifting. **To settle when built:** whether audio belongs in the same pin as image and video, or earns its own skin |

### `FormaNotesEditor`

| | |
|---|---|
| **Old pattern** | Modal with `<textarea>` for admin notes on orders/leads, save button, timestamp display |
| **Used in** | order detail, lead detail |
| **Parameters** | `string? Notes`, `EventCallback<string> OnSave` |
| **Blazor approach** | `FormaDialog` containing `FormaTextArea` + save button. Simple composition |

### `FormaPermissionMatrix`

| | |
|---|---|
| **Old pattern** | Radio (FullAdmin vs Custom) + 10 per-section dropdowns with 5 access levels each |
| **Used in** | user editor |
| **Parameters** | `@bind-Value` (UserPermissions model) |
| **Blazor approach** | `EditForm` with `InputRadioGroup` for role type, conditionally rendered grid of `InputSelect` dropdowns for each permission section |

### `FormaProductOptionsEditor`

| | |
|---|---|
| **Old pattern** | `options.html` — modal with type picker (8 option types: dropdown, checkbox, radio, cubes, images, color swatches, textbox, textarea), then option editor with name/price/image/color/stock rows per value. Sortable rows |
| **Used in** | product editor |
| **Parameters** | `@bind-Options` (List<ProductOption>) |
| **Blazor approach** | Complex interactive component. Type selection grid → option value editor form with `FormaSortableList`. Each row has contextual fields based on type |

### `FormaPublishButton`

| | |
|---|---|
| **Glyph** | A **globe**, struck through when the action is to unpublish — reusing the strike-through convention `FormaEyeButton` established, so "make this public" and "take it down" read as one control in two states rather than two unrelated icons |
| **Old pattern** | Coi's per-item active/publish toggle |
| **Used in** | Editor footers and row actions — Épica names publish beside edit, duplicate, delete and view, and `FormaStatusBadge` already renders the draft/scheduled/published state it changes |
| **Parameters** | `bool Published`, `string Label` (required), `RenderFragment? ChildContent`, `ButtonSize Size`, `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | Stateful like `FormaEyeButton` and usually labelled like `FormaAddButton` — a primary "Publish" in an editor footer is text, the same action in a dense row is the glyph alone. **Scheduling stays out of it:** draft/scheduled/published is three states and a button offers two, so "Schedule…" belongs in a menu beside it rather than turning this control into a date picker |

### `FormaRefreshButton`

| | |
|---|---|
| **Glyph** | A **circular arrow** |
| **Old pattern** | None — Coi reloaded the whole page |
| **Used in** | `FormaDataGrid`'s `RefreshDataAsync()`, which is public API today with no affordance of its own, and any list fed by a provider that can go stale |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` that forwards `Busy`, so a slow refresh spins and announces `aria-busy` rather than looking dead while it waits. The only one of the set where `Busy` is the point rather than an afterthought |

### `FormaShareButton`

| | |
|---|---|
| **Glyph** | **Three connected nodes**. Deliberately *not* the arrow leaving a box, which is the external-link glyph — Épica already lists "view" as a row action meaning *open on the public site*, and two arrows-out-of-a-box in one row would be indistinguishable |
| **Old pattern** | None |
| **Used in** | Copying or sending an item's public link, from its editor or its row |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A thin wrapper over `FormaButton` that raises `OnClick` and nothing more. It deliberately does **not** own `navigator.share()`: that API needs transient activation and a secure context, is absent on desktop Firefox, and therefore needs a copy-to-clipboard fallback — which is a product decision about what "share" means here, not something a glyph should choose for the host |

### `FormaSortMenu`

| | |
|---|---|
| **Glyph** | **Two opposed arrows** — `↑↓` |
| **Old pattern** | Coi sorted by clicking a column header, and had no standalone sort control. Header sorting is **not** what this replaces — `FormaDataGrid` already ships it by passing QuickGrid's `Sortable`, `SortBy`, `InitialSortDirection` and `GridSort<T>` straight through |
| **Used in** | Layouts with no header row to click — the tile grids in `FormaExplorer` and `FormaFileManager`, and any card list |
| **Parameters** | `RenderFragment ChildContent` (the sort options as menu items), `string Label` (required) |
| **Blazor approach** | A preset over `FormaDropDownMenu` like `FormaOverflowMenu`, because sorting offers a choice rather than performing one action — which is why it is a `Menu` and not a `Button`, and why it sits with the menus rather than in `Buttons/`. A grid that already has sortable column headers should keep using them — two controls over one sort state is a synchronisation problem with no upside |

### `FormaUpdateButton`

| | |
|---|---|
| **Glyph** | A **check mark** — the commit half of an inline edit, opposite the cross that discards it |
| **Old pattern** | The save button ending an edit postback |
| **Used in** | `FormaDataGrid`'s inline editing beside a cancel control, and editor footers |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the check glyph. Pairs with the cross button in a row's edit state, which is what `FormaDataGrid`'s `SaveEditAsync`/`CancelEditAsync` already expect a caller to wire up from a `TemplateColumn` |

### `FormaViewButton`

| | |
|---|---|
| **Glyph** | **Layout lines** — stacked rows for a list, a square of cells for a tile grid |
| **Old pattern** | None — Coi's image library had one fixed thumbnail grid |
| **Used in** | `FormaExplorer` and `FormaFileManager`, switching between a dense list and thumbnails |
| **Parameters** | `ViewMode Mode`, `EventCallback<ViewMode> ModeChanged`, `string Label` (required), `ButtonSize Size`, `bool Disabled` |
| **Blazor approach** | Stateful like `FormaEyeButton`, drawing the mode it will switch **to** and naming that mode in the label, since an icon-only toggle that draws its current state cannot say which it is. **To settle when built:** whether a two-option segmented control showing both modes at once beats a toggle, which is guesswork until the managers exist |
