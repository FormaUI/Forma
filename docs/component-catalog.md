# Forma Component Catalog

> Identified from the old Coi admin codebase, which is where the component set was derived
> from — the library itself is general-purpose. Each component maps an old Web Forms + jQuery
> pattern to a Blazor component using **only** built-in Blazor components, native HTML elements,
> and browser APIs — no external JS libraries.
>
> Target location: `Forma.Blazor/Components/`, or its own project under `src/` for a component large enough to grow sub-components
>
> **Naming convention:** All components use the `Forma` prefix for their tag names (e.g., `<FormaDialog>`,
> `<FormaTabs>`) to avoid clashes with third-party libraries. CSS classes use the `forma-` prefix.
> C# types (enums, services) rely on namespaces for disambiguation and don't carry the prefix.
>
> The components still to build, and the order they are built in, are in [the component roadmap](component-roadmap.md).

**61 components implemented.** In the structure, ✅ marks an implemented component and ◐ one in progress.

## Components Structure

```text
Forma.Blazor/Components/
├── ✅ FormaField
├── ✅ FormaVisibility
├── (4 / 4) Bars/
│   ├── ✅ FormaSearchToolbar
│   ├── ✅ FormaSelectionToolbar
│   ├── ✅ FormaStatusStrip
│   └── ✅ FormaToolbar
├── (4 / 4) Buttons/
│   ├── ✅ FormaButton
│   ├── ✅ FormaCloseButton
│   ├── ✅ FormaEyeButton
│   └── ✅ FormaToggleButton
├── (2 / 2) Collections/
│   ├── ✅ FormaDataGrid
│   └── ✅ FormaSortableList
├── (1 / 1) Debugging/InputInspector/
│   └── ✅ FormaInputInspector
├── (6 / 6) Feedback/
│   ├── ✅ FormaConfirmDialog
│   ├── ✅ FormaDialog
│   ├── ✅ FormaPopover
│   ├── ✅ FormaSpinner
│   ├── ✅ FormaStatusBadge
│   └── ✅ FormaToastContainer
├── (4 / 4) Files/
│   ├── ✅ FormaFilePicker
│   ├── ✅ FormaFileUploader
│   ├── ✅ FormaFileViewer
│   └── ✅ FormaImagePicker
├── (13 / 13) Inputs/
│   ├── ✅ FormaCalendar
│   ├── ✅ FormaCheckBox
│   ├── ✅ FormaColorPicker
│   ├── ✅ FormaDatePicker
│   ├── ✅ FormaDateRangePicker
│   ├── ✅ FormaMultiSelect
│   ├── ✅ FormaPasswordBox
│   ├── ✅ FormaSearchBox
│   ├── ✅ FormaSelect
│   ├── ✅ FormaSlider
│   ├── ✅ FormaTextArea
│   ├── ✅ FormaTextBox
│   └── ✅ FormaToggleSwitch
├── (1 / 1) Interaction/
│   └── ✅ FormaInputScope
├── (9 / 9) Layout/
│   ├── ✅ FormaBackdrop
│   ├── ✅ FormaChrome
│   ├── ✅ FormaCommandArea
│   ├── ✅ FormaCommandBar
│   ├── ✅ FormaFrame
│   ├── ✅ FormaFreezePane
│   ├── ✅ FormaSplitter
│   ├── ✅ FormaStackLayout
│   └── ✅ FormaTabs
├── (1 / 1) Media/
│   └── ✅ FormaVideoPlayer
├── (3 / 3) Menus/
│   ├── ✅ FormaDropDownMenu
│   ├── ✅ FormaOverflowMenu
│   └── ✅ FormaShareMenu
├── (2 / 2) Navigation/
│   ├── ✅ FormaBreadcrumb
│   └── ✅ FormaPagination
├── (2 / 2) Page/
│   ├── ✅ FormaPageHead
│   └── ✅ FormaPageTitle
├── (3 / 3) Symbols/
│   ├── ✅ FormaEmoji
│   ├── ✅ FormaEmojiPicker
│   └── ✅ FormaIcon
└── (2 / 2) Windows/
    ├── ✅ FormaRestArea
    └── ✅ FormaWindow

src/
├── (1 / 1) Forma.Blazor.Composer/
│   └── ✅ FormaComposer
└── (1 / 1) Forma.Blazor.Explorer/
    └── ✅ FormaExplorer
```

## Components

### `FormaBackdrop`

A region of the page in one of four looks, flat, raised, inset or canvas, told apart from the backdrop beside it by tone alone.

| | |
|---|---|
| **Old pattern** | Every panel drawing its own fill, border, corner and shadow, and the page background behind them |
| **Used in** | `FormaExplorer` for its canvas and its header, folder tree and content cards, the Showcase's backdrop and layout pages, and any region of a page |

### `FormaBreadcrumb`

A trail of segments to the current page, each a link, a button or plain text, with a menu of alternatives beside a segment that has children.

| | |
|---|---|
| **Old pattern** | The hand-rolled breadcrumb section of `FormaExplorerAddressBar` — root icon, chevron separators, a per-segment dropdown of alternatives, and drop-target handlers, all written directly into that component. `FormaPageTitle`'s own plain `<ol>` of links predates it too |
| **Used in** | Any trail of segments: a page header's site navigation as well as an explorer's folder path. `FormaExplorerBreadcrumb` and `FormaPageTitle` both compose it now rather than drawing their own |

### `FormaButton`

The library's button: one variant and size scale, a busy state that stays focusable, and the source of every other control's hover, focus and disabled treatment.

| | |
|---|---|
| **Old pattern** | `.btn`, `.btn-primary`, `.btn-sec`, `.btn-thrd`, `.btn-mini`, `.btn-load` (fa-3.6.5.css) — the shared button vocabulary every admin page's action bar, modal footer, and grid row built on top of |
| **Used in** | Single source of truth for buttons across the library — `FormaDataGrid` row/inline-edit actions, and the button `FormaDialog`, `FormaConfirmDialog`, `FormaPagination` and `FormaDatePicker` compose |

### `FormaCalendar`

One or two months of days with month and year selectors, drawing whatever highlight it is given and reporting the day picked or hovered.

| | |
|---|---|
| **Old pattern** | The month panels inside `FormaDatePicker` |
| **Used in** | `FormaDatePicker` with one month, `FormaDateRangePicker` with two |

### `FormaCheckBox`

A form checkbox with an indeterminate state, usable bare as a control that names its field through `ValueExpression`.

| | |
|---|---|
| **Old pattern** | Plain `<input type="checkbox">` styled by `fa-3.6.5.css` — no shared component, each admin page restated its own markup |
| **Used in** | Single source of truth for checkboxes across the library — `FormaDataGrid`'s header/row selection checkboxes and `FormaMultiSelect`'s option checkboxes |

### `FormaChrome`

A frame whose drag region is a title bar carrying the icon, the title and the host's commands, with optional header and footer bands around the body.

| | |
|---|---|
| **Old pattern** | Each panel drawing its own title strip, header band and footer band |
| **Used in** | `FormaWindow` as the chrome it floats; the Showcase's chrome page; the Admin Console's panels; any host that wants a titled region in a layout, or a titled panel it can drag |

### `FormaCloseButton`

The one dismiss control: a ghost icon-only button with the cross glyph and a required accessible name.

| | |
|---|---|
| **Old pattern** | The `×` dismiss control repeated across `admin/pops/*.aspx` modal headers and notification strips |
| **Used in** | Single source of truth for dismiss controls — `FormaDialog`'s header, `FormaToastContainer`'s toasts, `FormaSelectionToolbar`'s clear-selection control |
| **Glyph** | A **cross** |

### `FormaColorPicker`

A hex color field with the platform's own color dialog behind the swatch beside it, optionally with alpha.

| | |
|---|---|
| **Old pattern** | `tcp.js` custom color picker — click swatch → popup with gradient/hue selectors, hex input, opacity slider (`no-alpha` class disables) |
| **Used in** | design (bg, primary, secondary, text, sidebar, link colors), settings (popup bg), store-settings (e-commerce accent), slider (bg, arrow, video filter), forms (text, button bg/text), product (badge) |

### `FormaCommandArea` / `FormaCommandBar`

The area is the surface that stacks command bars; each bar lays its commands on a scrolling track with arrows at whichever end they continue past.

| | |
|---|---|
| **Old pattern** | The header region of `FormaExplorer` — the surface, the rule beneath it and the stacked bars — written into the explorer's own stylesheet |
| **Used in** | The region above any page's content: an explorer header, a list page's action strip, an editor's publish bar. `FormaExplorer` composes the area for its own header, and lays its toolbar and its selection bar on command bars, the view controls and the selection count standing past the track |

### `FormaComposer`

An authoring surface for rich text, Markdown, code and page composition, every capability and every editing surface arriving as a plugin; the rich text editor is complete, and the Markdown and code editors and the satellite capabilities still follow [the composer implementation plan](composer-implementation-plan.md). Its design is [composer-design.md](composer-design.md).

| | |
|---|---|
| **Old pattern** | **Two Coi surfaces, not one.** The rich text editor was CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px; custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`; `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction, and `allowedContent = true` — no content filtering whatsoever. The larger surface was the **page designer**, `admin/pops/template.aspx` over `Controls/Edit/`: a drag-and-drop page builder with a side palette, a live canvas, per-element delete/style/edit chrome, jquery-sortable reordering, column and element resizing, and a settings editor per element kind in `elm.js`. One entry point, `OpenTemplate(type, title)`, drove seven targets by numeric type — product page templates, the product tile, headings, rich content, popups and the custom dropdown menu |
| **Used in** | page editor, product description, product and category templates, popups, custom menus — and in Épica, the News article body |

### `FormaConfirmDialog`

A yes-or-no dialog opened through `IConfirmDialogService`, answering the caller with the reader's choice.

| | |
|---|---|
| **Old pattern** | `AreYouSure(this, 'message')` — custom confirm that returns bool. Used on delete buttons, destructive actions |
| **Used in** | Every page with delete/remove actions (list pages bulk delete, individual row delete, design change, template swap) |

### `FormaDataGrid<TItem>`

A sortable, pageable table over QuickGrid with checked and row selection, inline editing and a footer of its own. Its design is [data-grid-design.md](data-grid-design.md).

| | |
|---|---|
| **Old pattern** | `asp:GridView` with server-side paging, sortable columns, row click-through, checkbox column for mass select, action icon columns (view/edit/delete), status badges, responsive hide classes |
| **Used in** | pages, products, orders, leads, comments, users, coupons (all 7 main list pages) |

### `FormaDatePicker`

A form input for one date: a trigger opening an anchored calendar, with the culture's own months on request.

| | |
|---|---|
| **Old pattern** | EasePick CDN library with Hebrew locale, year dropdown, reset button, min-date lock |
| **Used in** | coupons (start/end dates) |

### `FormaDateRangePicker`

A form input for a `DateRange`: a trigger opening a two-month calendar with presets, committing the span once both ends are picked.

| | |
|---|---|
| **Old pattern** | The range mode of `FormaDatePicker`, bound through two separate dates |
| **Used in** | coupons (start/end dates), reports (a period), anywhere a span of days is one value |

### `FormaDialog`

A modal dialog over the native `<dialog>`, with built-in localized actions or the caller's own footer, dismissed through the input router.

| | |
|---|---|
| **Old pattern** | `Modal('#divId')` / `CloseModal()` — shows a `<div>` overlay with content from hidden divs. Stacking z-index, Escape key to close |
| **Used in** | Every admin page — new page modal, help modals, notes modals, folder dialogs, invoice modals, options editor |

### `FormaDropDownMenu`

A button that opens an anchored menu of items, groups and dividers, navigated by arrow keys and dismissed through the input router.

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` → shows a `dropdown-menu dropdown-menu-arrow` positioned below a button. Click outside to close |
| **Used in** | order detail (options: print, notes, invoice), and various action menus |

### `FormaEmoji`

Draws an emoji as inline artwork from the provider the host registered, falling back to the reader's platform font.

| | |
|---|---|
| **Old pattern** | CKEditor's `smiley` plugin, drawing emoji from an image sprite |
| **Used in** | `FormaEmojiPicker`, and any host surface showing an emoji as artwork rather than text |

### `FormaEmojiPicker`

A categorized emoji palette with search, recently used entries and a tab per category, browsing the set an `IEmojiCatalog` describes.

| | |
|---|---|
| **Old pattern** | CKEditor's `smiley` palette |
| **Used in** | `FormaComposerEmojiButton` |

### `FormaExplorer`

A file manager for what is already on the server: a folder tree, an address bar, thumbnail, details and list views over any kind of resource, upload, and every mutation reported to the host as intent. Its design is [explorer-design.md](explorer-design.md).

| | |
|---|---|
| **Old pattern** | The image half of `pops/images.aspx` — a grid of thumbnails with folders, multi-select, upload, move, delete and a capacity indicator |
| **Used in** | The media library as a surface in its own right, and as the "choose an existing image or video" dialog for article covers, OG images, product images and `FormaComposer` |

### `FormaEyeButton`

A ghost icon-only button showing an eye, struck through while what it guards is hidden.

| | |
|---|---|
| **Old pattern** | `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` beside a password field — a clickable span, not a button |
| **Used in** | Single source of truth for reveal controls — `FormaPasswordBox`'s toggle, and any later show/hide or preview affordance |
| **Glyph** | An **eye**, struck through when hidden |

### `FormaField`

The chrome every labeled input shares: the floating label, the box, the focus ring and the validation slot, styling the input its consumer renders.

| | |
|---|---|
| **Old pattern** | `<label class="lblForm">` over a `.form-control`, floating on focus or value, with `<span class="mand">` for required and the error text below |
| **Used in** | Chrome for every labeled form component — `FormaTextBox`, `FormaTextArea`, `FormaPasswordBox`, `FormaSelect`, `FormaMultiSelect`, `FormaDatePicker`, `FormaDateRangePicker`, `FormaColorPicker`, `FormaSlider` and `FormaToggleSwitch` |

### `FormaFilePicker`

A drop zone and file dialog yielding the browser files the reader picked from their device, validated one by one and never transferred. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The file input in `pops/images.aspx`'s upload panel — the browse button that opened the OS dialog, before anything had been posted anywhere |
| **Used in** | Every route by which a file leaves the user's machine: `FormaFileUploader` consumes its selection, and `FormaImagePicker` is a type-restricted skin over it |

### `FormaFileUploader`

Moves the files a picker produced into the host's `IFileStore`, one at a time, with progress, cancellation and the resources they became. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The handler `pops/images.aspx` posted to — the half that actually wrote the bytes into the media folder and drove the progress bar |
| **Used in** | Behind every surface that accepts a new file: `FormaExplorer`, `FormaComposer`, and any editor taking a file directly rather than through a library |

### `FormaFileViewer`

Shows a stored resource for what it is, image, video, audio, document or markup, through a chain of resource providers, stepping through siblings. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The preview pane of `pops/images.aspx` — clicking a library entry to actually look at it |
| **Used in** | `FormaExplorer`'s preview of a chosen resource, and any surface showing whatever a chosen resource turns out to be |

### `FormaFrame`

A raised backdrop with a box of its own: placed, it is moved by its drag region, resized at its edges and maximized to its bounds; presented, it drives its parent's box.

| | |
|---|---|
| **Old pattern** | Every panel that could be dragged or resized rolling its own pointer handling, and none that could be moved from the keyboard |
| **Used in** | `FormaChrome` as the box under its chrome, and through it `FormaWindow`; the Showcase's frame page; any host that wants a movable, resizable box of its own |

### `FormaFreezePane`

A scroll box along one axis whose frozen parts stay at the start, moved by the scrollbar or by an arrow at each end.

| | |
|---|---|
| **Old pattern** | The command bar's own track, arrows and measuring, written into the bar |
| **Used in** | `FormaCommandBar` for its track, `FormaExplorerDetailsView` for its columns with the actions and the name frozen, and any surface whose content outgrows its box along one axis while part of it must stay put: a table with frozen columns or a frozen header, a strip of chips, a tab list |

### `FormaIcon`

Draws an icon, a glyph, a brand mark or a character, as inline SVG or symbol-font text, so no icon font is loaded.

| | |
|---|---|
| **Old pattern** | Font Awesome icon classes from `fa-3.6.5.css` |
| **Used in** | Every component that draws a glyph, from `FormaCloseButton` to `FormaExplorer`; a host page drawing one of the library's glyphs |

### `FormaImagePicker`

A file picker pinned to image types, previewing each accepted file as a thumbnail. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The browse button in `pops/images.aspx`'s upload panel, filtered to image types |
| **Used in** | Anywhere a *new* image comes off the user's device — page editor (cover image, OG image), product editor (images, gallery), design (logo, bg, top image), slider (slide bg, elements) |

### `FormaInputInspector`

A panel showing the input router's latest decisions and the active ordering, live.

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | The Showcase's `FormaInputInspector` page under Debugging, and any host debugging why an input went where it did |

### `FormaInputScope`

An element registered with the input router as a scope, declared from markup with a profile of routes and dismissal behavior.

| | |
|---|---|
| **Old pattern** | None — input ownership was decided per component by whichever listener ran first |
| **Used in** | A host or a component that declares an input scope declaratively: a region that routes its own keys, a surface held active while it is open, a modal barrier. The library's own components register with the router from their code-behind and do not need it |

### `FormaMultiSelect`

A form input for several values: a dropdown of checkboxes with search, the chosen items shown as removable tags.

| | |
|---|---|
| **Old pattern** | `.multi-input` with `.tags` + `.multi-dropdown` list. `$('#el').multi()` plugin. Tags display selected items, click X to remove. Dropdown with checkboxes. Optional "add new" button |
| **Used in** | page editor (categories), product editor (categories), coupons (categories) |

### `FormaOverflowMenu`

A drop-down menu behind the ellipsis trigger, so every row's overflow affordance is the same glyph and target.

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` opening a `dropdown-menu` from a row's options button |
| **Used in** | Row actions once a row carries more than two; `FormaExplorer`'s item, folder and action-bar menus, and `FormaComposer`'s plugins bar |
| **Glyph** | An **ellipsis**, horizontal or vertical as the host orients it |

### `FormaPageHead`

The document head a host renders once: charset, viewport, title, base and icon, Forma's stylesheets in cascade order, and the framework's own outlets closing it.

| | |
|---|---|
| **Old pattern** | Each host hand-writing the same `<head>`: charset, viewport, base address, and Forma's stylesheets linked one by one in an order nothing enforced |
| **Used in** | Every host's `App.razor`. The Showcase composes it, and any consuming application does the same |

### `FormaPageTitle`

A page's heading with its breadcrumb trail and an actions slot.

| | |
|---|---|
| **Old pattern** | `<h1 class="page-title">` + `<ol class="breadcrumb">` + optional capacity label |
| **Used in** | Every admin page (pages, products, orders, leads, comments, users, coupons, settings, design) |

### `FormaPagination`

A paginator usable on its own or composed into a grid, with numbered links, a page-number box and a Go button as options.

| | |
|---|---|
| **Old pattern** | Server-rendered page links: `<a class="page">1</a> <a class="page">2</a>...`, custom paging in code-behind with `PageSize`, `PageIndex`, total count |
| **Used in** | All list pages, image library |

### `FormaPasswordBox`

A password field with a reveal toggle, a strength meter, advice on what a value lacks, and rejection of terms drawn from other fields.

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="Password">` + `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` toggles between password/text |
| **Used in** | user editor, settings (API keys), admin login |

### `FormaPopover`

A manual popover in the top layer anchored to its trigger, owning placement, focus return and dismissal for every dropdown, menu and picker. Its design is [popover-design.md](popover-design.md).

| | |
|---|---|
| **Old pattern** | None as a component — each of `FormaSelect`, `FormaMultiSelect`, `FormaDatePicker`, `FormaDropDownMenu` and the composer's emoji button carried its own copy of the open flag, the backdrop, the popover element and the `AnchoredPopover` wiring, and two of the five had no Escape handling at all |
| **Used in** | Every anchored panel in the library: the select and multi-select lists, the date picker's calendar, the drop-down menu and the composer's emoji picker. A host reaches for it directly for any panel that hangs from a control and is not a modal — a modal is `FormaDialog` |

### `FormaRestArea`

One persistent handle per window in the workspace, grouped by origin or instance, as the way back to any window whatever its state.

| | |
|---|---|
| **Old pattern** | None — Coi had no way back to a panel once it was out of sight |
| **Used in** | The Showcase's window page; the Admin Console's shell, and `FormaLaunchBar` once it exists |

### `FormaSearchBox`

A search field reporting its value on every keystroke and the committed term on Enter, clear or a pause in typing.

| | |
|---|---|
| **Old pattern** | The hand-rolled search box in `FormaExplorerAddressBar` — a magnifying glass glyph, a `type="search"` input, and a clear button, all written directly into that component |
| **Used in** | General search and filter fields anywhere a caller wants a `type="search"` box rather than a plain `FormaTextBox`. Now the box `FormaExplorerSearchBox` composes rather than draws itself |

### `FormaSearchToolbar`

A search box beside a toolbar of labeled list actions, at the top of a list page. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | `<div class="card-body">` with search textbox + filter dropdowns + action buttons, wrapping content in a flex row |
| **Used in** | pages, products, orders, leads, comments, users, coupons, filters |

### `FormaSelect`

A form input choosing one value from a keyboard-navigable list in an anchored popover.

| | |
|---|---|
| **Old pattern** | JS replaces native `<select>` with styled div: `.custom-select > .cs-val` (display) + `.cs-options > .cs-option` (dropdown). Click to open, click option to select, click outside to close |
| **Used in** | Multiple admin pages for styled dropdowns |

### `FormaSelectionToolbar`

A bar that appears while rows are checked, showing the count, the bulk actions and a clear control. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | "Select all" checkbox in header + per-row checkboxes. When items selected, show action bar (delete selected, change status, export). `InitTableEvents()` / `$('.cbx').prop('checked', ...)` |
| **Used in** | pages, products, orders, leads, comments (5 of 7 list pages) |

### `FormaShareMenu`

A menu of everything the app can do with an item: the share networks and destinations registered with it, the native share sheet, and the built-in commands. Its design is [share-design.md](share-design.md).

| | |
|---|---|
| **Old pattern** | None — loose Download/Print/Copy buttons scattered per surface, and no sharing at all |
| **Used in** | Anywhere a subject leaves the application: an article shared to a network, a file downloaded, a document printed. Planned over the composer's Preview and Source outputs and `FormaExplorer`'s download actions |

### `FormaSlider`

A form input over a native range control, generic over any number, showing its value beside the label.

| | |
|---|---|
| **Old pattern** | `<input type="range">` + hidden field + live value display label |
| **Used in** | forms (border radius), slider (duration), design/edit (font size, site width) |

### `FormaSortableList<TItem>`

A list the reader reorders by drag and drop or from the keyboard, reporting the new order.

| | |
|---|---|
| **Old pattern** | jQuery Dragsort / HTML5 sortable — drag items to reorder. On drop, serialize new order and POST to `sorting.asmx/Sort`. Used for menus, galleries, sliders, categories, product options, delivery methods, form fields, filters |
| **Used in** | menu sort page, gallery module, slider editor, category module, product options, store-settings delivery, contact form builder, filters editor |

### `FormaSpinner`

An indeterminate rotating ring announcing that something is loading.

| | |
|---|---|
| **Old pattern** | Coi's `btn-load` class and its loading indicator |
| **Used in** | `FormaEmojiPicker` while a sheet loads; a host surface waiting on something |

### `FormaSplitter`

A divider standing in the seam between two backdrops, dragged or driven by arrow keys to resize what it separates, invisible at rest.

| | |
|---|---|
| **Old pattern** | Two hand-rolled sashes in the explorer — the vertical one resizing the folders panel and the horizontal one resizing the folder list — each with its own pointer plumbing, key handling and clamping |
| **Used in** | Any pair of panes a reader can resize. `FormaExplorer` composes it for its sidebar splitter and `FormaExplorerSidebar` for its tree sash, which is what proved both orientations |

### `FormaStackLayout`

Backdrops along one axis, a seam apart, vertical or horizontal.

| | |
|---|---|
| **Old pattern** | A flex row or column written into each host with its own gap |
| **Used in** | `FormaExplorer` for its vertical stack of header and body and its horizontal stack of folder tree and content, the Showcase's backdrop and layout pages, and any host stacking backdrops along one axis |

### `FormaStatusBadge`

A tinted label for a status the host's catalog defines.

| | |
|---|---|
| **Old pattern** | Colored div/span with status text, different background per status (orders: 17 statuses with distinct colors; leads: 7 statuses) |
| **Used in** | orders list/detail, leads list/detail |

### `FormaStatusStrip`

A toolbar of readouts and commands along the bottom of a surface, announcing a readout when asked. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | None — extracted from `FormaComposerStatusStrip`'s shape into a generic component, so any host can show a quiet strip of readouts and commands without pulling in the composer. |
| **Used in** | `FormaComposerStatusStrip`, which composes it |

### `FormaTabs`

A tab list over tab panels, walked by the arrow keys, Home and End, with the active tab bound or controlled by the host.

| | |
|---|---|
| **Old pattern** | jQuery Tools Tabs: `<ul class="tabs"><li>` + `<div class="panes"><div class="panc">`. Click tab → show corresponding pane |
| **Used in** | page editor (5 tabs), product editor (7 tabs), settings (5 tabs), store-settings (7 tabs), design (5 tabs) |

### `FormaTextArea`

A multi-line text input in a field, resizable downward, with an optional running character count.

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="MultiLine">` — the same `.form-control` class as a single-line field, plus `textarea { resize: vertical }` and a label offset (`.row textarea ~ label.lblForm { top: 13px }`) |
| **Used in** | SEO head, meta description, store details — and in Épica, article excerpts and summaries |

### `FormaTextBox`

A single-line text input, bare or in a field with a floating label, typed as text, email, telephone, web address, a name or a one-time code.

| | |
|---|---|
| **Old pattern** | `<input type="text" class="form-control">` without floating labels or dedicated label wrappers |
| **Used in** | Every text field: labeled form fields, compact inline forms, inline grid filters, dialog fields, toolbars and filter rows |

### `FormaToastContainer`

The live region at layout level that renders the toasts raised through `IToastService` and dismisses each on its own timer.

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |

### `FormaToggleButton`

A ghost button for a toolbar or a bar of tools: a glyph, a label or both, and for a toggle whether it is on. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | Coi's toolbar buttons, each styled by hand |
| **Used in** | `FormaToolbar`, `FormaRestArea`, `FormaExplorerToolbar` and `FormaExplorerSelectionBar` |

### `FormaToggleSwitch`

A form input for a Boolean, drawn as a sliding switch.

| | |
|---|---|
| **Old pattern** | jQuery Toggles plugin — styled checkbox as iOS-style toggle with on/off labels, CSS animated slider |
| **Used in** | page editor (SEO, comments, password), product (active, featured), settings (multiple), store-settings (multiple), coupons (active), user (active), order (paid) |

### `FormaToolbar`

A row of commands, toggles, menus, readouts and custom items described as data, scrolling when it cannot fit, with shortcuts routed through the input router. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | None — a new, generic addition rather than a Coi-derived one, extracted from `FormaComposer`'s own toolbar to give any host a grouped, item-driven command bar without pulling in the composer |
| **Used in** | Anywhere a host needs a bar of commands driven by data rather than hand-written markup. Already the base for `FormaSearchToolbar`, `FormaSelectionToolbar`, `FormaStatusStrip` and `FormaComposerCommandBar`, and the title bar of `FormaChrome` |

### `FormaVideoPlayer`

Plays a video by its address, through a chain of player providers that know the services, loading nothing from a service until the reader presses play. Its design is [media-design.md](media-design.md).

| | |
|---|---|
| **Old pattern** | CKEditor's **YouTube embed** toolbar button and the raw `<iframe>` markup it pasted into the body |
| **Used in** | The most-demanded component on this list — Épica wants it on four separate surfaces: YouTube-by-URL embeds in articles (6.5), the auto-embedded live broadcast player (9.2), the Videos module's listings and featured video (P10), and the home page's featured video block (11.4). `FormaFileViewer` delegates its video branch here |

### `FormaVisibility`

Collapses its content to nothing without unmounting it, animated on request.

| | |
|---|---|
| **Old pattern** | None — extracted after `FormaCommandBar` and `FormaStatusStrip` each grew their own `Hidden`/`Animated` and hand-rolled an identical `grid-template-rows: 0fr → 1fr` collapse, down to the same three CSS rules copy-pasted under a different class prefix each time |
| **Used in** | `FormaCommandBar`, `FormaStatusStrip`, `FormaShareMenu` and `FormaComposerPluginsBar` wrap their own chrome in it. `FormaToolbar` forwards `Hidden`/`Animated` to the `FormaCommandBar` it is built on when it scrolls, and wraps its wrapping row in it when it does not |

### `FormaWindow`

A chrome floated into the top layer as a member of the workspace: activated, stacked, minimized, locked, modal and closed under the manager.

| | |
|---|---|
| **Old pattern** | Every floating box — the composer maximized, a dialog — inventing its own chrome |
| **Used in** | The Showcase's window page; the Admin Console's floating panels |

## Components NOT Needed (handled by built-ins or native HTML)

| Old Pattern | Replaced By |
|---|---|
| **MiniBar** (custom scrollbar) | CSS `scrollbar-width: thin` + `scrollbar-color` (Firefox) / `::-webkit-scrollbar` (Chromium). Or just let browser handle it |
| **UpdatePanel** (partial postback) | Blazor's built-in component re-rendering. Interactive Server mode handles partial updates natively |
| **RepositoryDataSource** | EF Core `IQueryable<T>` passed to components, or injected services |
| **ASMX web services** | Blazor component methods (Interactive Server) or Minimal API endpoints |
| **Hidden fields for state** | Blazor `@bind` and component parameters — state lives in C# |
| **Parent frame communication** | Blazor `EventCallback` between components — no iframes needed |
| **btn-load CSS class** | Blazor disable-during-async pattern + CSS loading indicator |
| **jQuery `$.ajax`** | `HttpClient` in Blazor, or direct C# calls in Interactive Server mode |
| **CKEditor** | Now in scope as `FormaComposer` rather than excluded — Forma builds its own editor over `contenteditable` with its own controls, taking Coi's feature set as the floor. External libraries appear only as isolated capability satellites, never as the editor itself |
