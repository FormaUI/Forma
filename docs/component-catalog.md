# Forma Component Catalog

The components Forma ships and those in progress, each with what it is, the pattern it replaces in the legacy application, and where it is used. The components still to build, and the order they are built in, are in [the component roadmap](component-roadmap.md).

**67 components implemented, 7 in progress.** In the structure, ✅ marks an implemented component and ◐ one in progress.

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
├── (1 / 1) Collections/
│   └── ✅ FormaSortableList
├── (1 / 1) Debugging/InputInspector/
│   └── ✅ FormaInputInspector
├── (7 / 7) Feedback/
│   ├── ✅ FormaCapacityIndicator
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
├── (16 / 16) Inputs/
│   ├── ✅ FormaCheckBox
│   ├── ✅ FormaColorPicker
│   ├── ✅ FormaCurrencyInput
│   ├── ✅ FormaDatePicker
│   ├── ✅ FormaDateRangePicker
│   ├── ✅ FormaDateTimePicker
│   ├── ✅ FormaMultiSelect
│   ├── ✅ FormaNumberInput
│   ├── ✅ FormaPasswordBox
│   ├── ✅ FormaSearchBox
│   ├── ✅ FormaSelect
│   ├── ✅ FormaSlider
│   ├── ✅ FormaTextArea
│   ├── ✅ FormaTextBox
│   ├── ✅ FormaTimePicker
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
├── (1 / 1) Page/
│   └── ✅ FormaPageTitle
├── (3 / 3) Symbols/
│   ├── ✅ FormaEmoji
│   ├── ✅ FormaEmojiPicker
│   └── ✅ FormaIcon
├── (1 / 1) Virtualization/
│   └── ✅ FormaVirtualize
└── (2 / 2) Windows/
    ├── ✅ FormaRestArea
    └── ✅ FormaWindow

src/Blazor/
├── (1 / 1) Forma.Blazor.Composer/
│   └── ✅ FormaComposer
├── (2 / 2) Forma.Blazor.Composer/RichTextEditor/
│   ├── ✅ FormaCompactRichTextEditor
│   └── ✅ FormaStandardRichTextEditor
├── (0 / 7) Forma.Blazor.DataView/
│   ├── ◐ FormaDataCards
│   ├── ◐ FormaDataGrid
│   ├── ◐ FormaDataList
│   ├── ◐ FormaDataSortMenu
│   ├── ◐ FormaDataTree
│   ├── ◐ FormaDataView
│   └── ◐ FormaDataViewSwitch
├── (1 / 1) Forma.Blazor.Explorer/
│   └── ✅ FormaExplorer
└── (1 / 1) Forma.Blazor.Server/Components/Page/
    └── ✅ FormaPageHead
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
| **Old pattern** | `<ol class="breadcrumb">` of links under each page's title |
| **Used in** | Any trail of segments: a page header's site navigation, and `FormaExplorer`'s folder path through `FormaExplorerBreadcrumb` and `FormaPageTitle` |

### `FormaButton`

The library's button: one scale of variants and sizes, a busy state, and the hover, focus and disabled treatment every other control takes from it.

| | |
|---|---|
| **Old pattern** | `.btn`, `.btn-primary`, `.btn-sec`, `.btn-thrd`, `.btn-mini`, `.btn-load` (fa-3.6.5.css) — the shared button vocabulary every admin page's action bar, modal footer, and grid row built on top of |
| **Used in** | Single source of truth for buttons across the library — `FormaDataGrid` row/inline-edit actions, and the button `FormaDialog`, `FormaConfirmDialog`, `FormaPagination` and `FormaDatePicker` compose |

### `FormaCapacityIndicator`

A readout of how much of a limit is used, as the count against the limit and optionally a bar, marking it nearly full, full or over.

| | |
|---|---|
| **Old pattern** | `<b>קיבולת:</b> <span dir="ltr">X / Y</span>` — shows used vs. allowed count, red at or past the limit and orange above 80% with fewer than 60 left |
| **Used in** | pages, products, images, orders, leads, users (6 pages) |

### `FormaCheckBox`

A form checkbox with an indeterminate state, usable bare as a control.

| | |
|---|---|
| **Old pattern** | Plain `<input type="checkbox">` styled by `fa-3.6.5.css` — no shared component, each admin page restated its own markup |
| **Used in** | Single source of truth for checkboxes across the library — `FormaDataGrid`'s header/row selection checkboxes and `FormaMultiSelect`'s option checkboxes |

### `FormaChrome`

A frame whose drag region is a title bar carrying the icon, the title and the host's commands, with header and footer bands around the body. Its design is [window-design.md](window-design.md).

| | |
|---|---|
| **Old pattern** | Each panel drawing its own title strip, header band and footer band |
| **Used in** | `FormaWindow` as the chrome it floats; the Showcase's chrome page; any host that wants a titled region in a layout, or a titled panel it can drag |

### `FormaCloseButton`

The one dismiss control: an icon-only button with the cross glyph. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The `×` dismiss control repeated across `admin/pops/*.aspx` modal headers and notification strips |
| **Used in** | Single source of truth for dismiss controls — `FormaDialog`'s header, `FormaToastContainer`'s toasts, `FormaSelectionToolbar`'s clear-selection control |

### `FormaColorPicker`

A hex color field with the platform's own color dialog behind the swatch beside it.

| | |
|---|---|
| **Old pattern** | `tcp.js` custom color picker — click swatch → popup with gradient/hue selectors, hex input, opacity slider (`no-alpha` class disables) |
| **Used in** | design (bg, primary, secondary, text, sidebar, link colors), settings (popup bg), store-settings (e-commerce accent), slider (bg, arrow, video filter), forms (text, button bg/text), product (badge) |

### `FormaCommandArea` / `FormaCommandBar`

The area is the surface that stacks command bars; each bar is a track of commands that scrolls when they cannot fit.

| | |
|---|---|
| **Old pattern** | Each page's strip of actions above its content, drawn per page |
| **Used in** | The region above any page's content: `FormaExplorer`'s header, a list page's action strip, an editor's publish bar |

### `FormaCompactRichTextEditor`

`FormaComposer` with typography, basic formatting, alignment and direction, and links, for a blurb or a summary. Its design is [composer-design.md](composer-design.md#45-presets-are-components-not-subclasses).

| | |
|---|---|
| **Old pattern** | CKEditor 4's `Mini` preset |
| **Used in** | A blurb or a summary a form takes beside its other fields |

### `FormaComposer`

An authoring surface for rich text, Markdown, code and page composition, every capability and every editing surface arriving as a plugin. Its design is [composer-design.md](composer-design.md).

| | |
|---|---|
| **Old pattern** | Two legacy surfaces: CKEditor 4 in three presets with custom plugins for images, video, emoji, line height and forms, and the drag-and-drop page designer over `admin/pops/template.aspx` |
| **Used in** | page editor, product description, product and category templates, popups, custom menus, and a news article's body |

### `FormaConfirmDialog`

A yes-or-no dialog answering the caller with the reader's choice.

| | |
|---|---|
| **Old pattern** | `AreYouSure(this, 'message')` — custom confirm that returns bool. Used on delete buttons, destructive actions |
| **Used in** | Every page with delete/remove actions (list pages bulk delete, individual row delete, design change, template swap) |

### `FormaCurrencyInput`

A form input for an amount of money in a currency the application names, typed as the reader's culture writes a number beside the symbol it writes the currency with. Its design is [currency-input-design.md](currency-input-design.md).

| | |
|---|---|
| **Old pattern** | `.form-addon` wrapping input with currency symbol overlay |
| **Used in** | product (price, sale price, cost), store-settings (min order, shipping), coupons (discount amount) |

### `FormaDatePicker`

A form input for one date: a trigger opening an anchored calendar of one month, with month and year selectors.

| | |
|---|---|
| **Old pattern** | EasePick CDN library with Hebrew locale, year dropdown, reset button, min-date lock |
| **Used in** | coupons (start/end dates) |

### `FormaDateRangePicker`

A form input for a span of days: a trigger opening a calendar of two months side by side, with month and year selectors and presets.

| | |
|---|---|
| **Old pattern** | The range mode of `FormaDatePicker`, bound through two separate dates |
| **Used in** | coupons (start/end dates), reports (a period), anywhere a span of days is one value |

### `FormaDateTimePicker`

A form input for a date with its time of day, the date picked from a calendar and the time typed or chosen from a list of suggested times, under one label. Its design is [time-inputs-design.md](time-inputs-design.md).

| | |
|---|---|
| **Old pattern** | None — the legacy dates carried no time |
| **Used in** | Forms taking a moment, and `FormaPropertyColumn`'s editor for a member holding one |

### `FormaDialog`

A modal dialog with built-in localized actions or the caller's own footer.

| | |
|---|---|
| **Old pattern** | `Modal('#divId')` / `CloseModal()` — shows a `<div>` overlay with content from hidden divs. Stacking z-index, Escape key to close |
| **Used in** | Every admin page — new page modal, help modals, notes modals, folder dialogs, invoice modals, options editor |

### `FormaDropDownMenu`

A button that opens an anchored menu of items, groups and dividers.

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` → shows a `dropdown-menu dropdown-menu-arrow` positioned below a button. Click outside to close |
| **Used in** | order detail (options: print, notes, invoice), and various action menus |

### `FormaEmoji`

Draws an emoji as artwork from the provider the host registered.

| | |
|---|---|
| **Old pattern** | CKEditor's `smiley` plugin, drawing emoji from an image sprite |
| **Used in** | `FormaEmojiPicker`, and any host surface showing an emoji as artwork rather than text |

### `FormaEmojiPicker`

A categorized emoji palette with search, recently used entries and a tab per category.

| | |
|---|---|
| **Old pattern** | CKEditor's `smiley` palette |
| **Used in** | `FormaComposerEmojiButton` |

### `FormaExplorer`

A file manager for what is already on the server: a folder tree, an address bar, thumbnail, details and list views over any kind of resource, and upload. Its design is [explorer-design.md](explorer-design.md).

| | |
|---|---|
| **Old pattern** | The image half of `pops/images.aspx` — a grid of thumbnails with folders, multi-select, upload, move, delete and a capacity indicator |
| **Used in** | The media library as a surface in its own right, and as the "choose an existing image or video" dialog for article covers, OG images, product images and `FormaComposer` |

### `FormaEyeButton`

An icon-only button showing an eye, struck through while what it guards is hidden. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` beside a password field — a clickable span, not a button |
| **Used in** | Single source of truth for reveal controls — `FormaPasswordBox`'s toggle |

### `FormaField`

The chrome every labeled input shares: the floating label, the box, the focus ring and the validation messages, styling the input its consumer renders.

| | |
|---|---|
| **Old pattern** | `<label class="lblForm">` over a `.form-control`, floating on focus or value, with `<span class="mand">` for required and the error text below |
| **Used in** | Chrome for every labeled form component — `FormaTextBox`, `FormaTextArea`, `FormaPasswordBox`, `FormaSelect`, `FormaMultiSelect`, `FormaDatePicker`, `FormaDateRangePicker`, `FormaColorPicker`, `FormaSlider`, `FormaToggleSwitch`, `FormaNumberInput`, `FormaSearchBox`, `FormaTimePicker` and `FormaDateTimePicker` |

### `FormaFilePicker`

A drop zone and file dialog yielding the files the reader picked from their device. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The file input in `pops/images.aspx`'s upload panel — the browse button that opened the OS dialog, before anything had been posted anywhere |
| **Used in** | Every route by which a file leaves the user's machine: `FormaFileUploader` consumes its selection, and `FormaImagePicker` is a type-restricted skin over it |

### `FormaFileUploader`

Moves the files a picker produced into the host's store, with progress and cancellation. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The handler `pops/images.aspx` posted to — the half that actually wrote the bytes into the media folder and drove the progress bar |
| **Used in** | Behind every surface that accepts a new file: `FormaExplorer`, `FormaComposer`, and any editor taking a file directly rather than through a library |

### `FormaFileViewer`

Shows a stored resource for what it is, image, video, audio, document or markup, stepping through its siblings. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | The preview pane of `pops/images.aspx` — clicking a library entry to actually look at it |
| **Used in** | `FormaExplorer`'s preview of a chosen resource, and any surface showing whatever a chosen resource turns out to be |

### `FormaFrame`

A raised backdrop with a box of its own, which the reader can move, resize and maximize, or which drives its parent's box. Its design is [window-design.md](window-design.md).

| | |
|---|---|
| **Old pattern** | Every panel that could be dragged or resized rolling its own pointer handling, and none that could be moved from the keyboard |
| **Used in** | `FormaChrome` as the box under its chrome, and through it `FormaWindow`; the Showcase's frame page; any host that wants a movable, resizable box of its own |

### `FormaFreezePane`

A scroll box along one axis whose frozen parts stay at the start, moved by the scrollbar or by an arrow at each end.

| | |
|---|---|
| **Old pattern** | The command bar's own track, arrows and measuring, written into the bar |
| **Used in** | `FormaCommandBar` for its track, `FormaExplorer`'s details view, and any surface whose content outgrows its box along one axis while part of it stays put: a table with frozen columns or a frozen header, a strip of chips, a tab list |

### `FormaIcon`

Draws an icon: a glyph, a brand mark or a character.

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

A panel showing the input router's latest decisions and the active ordering, live. Its design is [input-router-design.md](input-router-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | The Showcase's `FormaInputInspector` page under Debugging, and any host debugging why an input went where it did |

### `FormaInputScope`

An element registered with the input router as a scope, declared from markup. Its design is [input-router-design.md](input-router-design.md).

| | |
|---|---|
| **Old pattern** | None — input ownership was decided per component by whichever listener ran first |
| **Used in** | A host declaring an input scope in markup: a region that routes its own keys, a surface held active while it is open, a modal barrier |

### `FormaMultiSelect`

A form input for several values: a dropdown of checkboxes with search, the chosen items shown as removable tags.

| | |
|---|---|
| **Old pattern** | `.multi-input` with `.tags` + `.multi-dropdown` list. `$('#el').multi()` plugin. Tags display selected items, click X to remove. Dropdown with checkboxes. Optional "add new" button |
| **Used in** | page editor (categories), product editor (categories), coupons (categories) |

### `FormaNumberInput`

A form input for a number, typed as the formatting culture writes it and stepped with the vertical arrows.

| | |
|---|---|
| **Old pattern** | `asp:TextBox` in `TextMode="Phone"`, left to right and capped by length, standing in for a number |
| **Used in** | product (stock), coupons (usage limit), `FormaCurrencyInput`, and `FormaPropertyColumn`'s editor for a numeric member |

### `FormaOverflowMenu`

A drop-down menu behind an ellipsis trigger. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` opening a `dropdown-menu` from a row's options button |
| **Used in** | Row actions once a row carries more than two; `FormaExplorer`'s item, folder and action-bar menus, and `FormaComposer`'s plugins bar |

### `FormaPageHead`

The document head a host renders once, with Forma's stylesheets in their cascade order.

| | |
|---|---|
| **Old pattern** | Each host hand-writing the same `<head>`: charset, viewport, base address, and Forma's stylesheets linked one by one in an order nothing enforced |
| **Used in** | Every host's `App.razor` |

### `FormaPageTitle`

A page's heading with its breadcrumb trail and an actions slot.

| | |
|---|---|
| **Old pattern** | `<h1 class="page-title">` + `<ol class="breadcrumb">` + optional capacity label |
| **Used in** | Every admin page (pages, products, orders, leads, comments, users, coupons, settings, design) |

### `FormaPagination`

A paginator usable on its own or composed into a grid.

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

A panel in the top layer anchored to its trigger, for every dropdown, menu and picker. Its design is [popover-design.md](popover-design.md).

| | |
|---|---|
| **Old pattern** | Each dropdown, menu and picker positioning a hidden `<div>` of its own below its button |
| **Used in** | Every anchored panel in the library: the select and multi-select lists, the date picker's calendar, the drop-down menu and the composer's emoji picker, and any panel a host hangs from a control |

### `FormaRestArea`

One persistent handle per window in the workspace, grouped by origin or instance, as the way back to any window whatever its state. Its design is [window-design.md](window-design.md).

| | |
|---|---|
| **Old pattern** | None — the legacy application had no way back to a panel once it was out of sight |
| **Used in** | The Showcase's window page; a host's shell |

### `FormaSearchBox`

A search field with a clear control.

| | |
|---|---|
| **Old pattern** | The search textbox in each list page's action strip |
| **Used in** | Search and filter fields, and `FormaExplorerSearchBox` |

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
| **Used in** | Anywhere a subject leaves the application: an article shared to a network, a file downloaded, a document printed |

### `FormaSlider`

A form input choosing a number from a range, showing its value beside the label.

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
| **Old pattern** | The legacy `btn-load` class and its loading indicator |
| **Used in** | `FormaEmojiPicker` while a sheet loads; a host surface waiting on something |

### `FormaSplitter`

A divider standing in the seam between two backdrops, dragged or driven by arrow keys to resize what it separates, invisible at rest.

| | |
|---|---|
| **Old pattern** | Two hand-rolled sashes in the explorer — the vertical one resizing the folders panel and the horizontal one resizing the folder list — each with its own pointer plumbing, key handling and clamping |
| **Used in** | Any pair of panes a reader can resize, such as `FormaExplorer`'s sidebar and its folder tree |

### `FormaStackLayout`

Backdrops along one axis, a seam apart, vertical or horizontal.

| | |
|---|---|
| **Old pattern** | A flex row or column written into each host with its own gap |
| **Used in** | `FormaExplorer` for its vertical stack of header and body and its horizontal stack of folder tree and content, the Showcase's backdrop and layout pages, and any host stacking backdrops along one axis |

### `FormaStandardRichTextEditor`

`FormaComposer` with paragraph formats, typography, basic formatting, lists, alignment and direction, links, images, tables and horizontal rules. Its design is [composer-design.md](composer-design.md#45-presets-are-components-not-subclasses).

| | |
|---|---|
| **Old pattern** | CKEditor 4's `Basic` preset |
| **Used in** | A rich text a form takes beside its other fields, where the composer's whole plugin set is more than the text needs |

### `FormaStatusBadge`

A tinted label for a status the host's catalog defines.

| | |
|---|---|
| **Old pattern** | Colored div/span with status text, different background per status (orders: 17 statuses with distinct colors; leads: 7 statuses) |
| **Used in** | orders list/detail, leads list/detail |

### `FormaStatusStrip`

A toolbar of readouts and commands along the bottom of a surface. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | `FormaComposerStatusStrip`, which composes it |

### `FormaTabs`

A tab list over tab panels.

| | |
|---|---|
| **Old pattern** | jQuery Tools Tabs: `<ul class="tabs"><li>` + `<div class="panes"><div class="panc">`. Click tab → show corresponding pane |
| **Used in** | page editor (5 tabs), product editor (7 tabs), settings (5 tabs), store-settings (7 tabs), design (5 tabs) |

### `FormaTextArea`

A multi-line text input in a field, resizable downward.

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="MultiLine">` styled as a single-line field and resizable downward |
| **Used in** | SEO head, meta description, store details, and an article's excerpt and summary |

### `FormaTextBox`

A single-line text input, bare or in a field with a floating label.

| | |
|---|---|
| **Old pattern** | `<input type="text" class="form-control">` without floating labels or dedicated label wrappers |
| **Used in** | Every text field: labeled form fields, compact inline forms, inline grid filters, dialog fields, toolbars and filter rows |

### `FormaTimePicker`

A form input for a time of day, typed as the formatting culture writes it or chosen from a list of suggested times. Its design is [time-inputs-design.md](time-inputs-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | Forms taking a time of day, and `FormaPropertyColumn`'s editor for a member holding one |

### `FormaToastContainer`

The live region at layout level that shows toasts.

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |

### `FormaToggleButton`

A ghost button for a toolbar or a bar of tools: a glyph, a label or both, and for a toggle whether it is on. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | The legacy toolbar buttons, each styled by hand |
| **Used in** | `FormaToolbar`, `FormaRestArea`, `FormaExplorerToolbar` and `FormaExplorerSelectionBar` |

### `FormaToggleSwitch`

A form input for a Boolean, drawn as a sliding switch.

| | |
|---|---|
| **Old pattern** | jQuery Toggles plugin — styled checkbox as iOS-style toggle with on/off labels, CSS animated slider |
| **Used in** | page editor (SEO, comments, password), product (active, featured), settings (multiple), store-settings (multiple), coupons (active), user (active), order (paid) |

### `FormaToolbar`

A row of commands, toggles, menus, readouts and custom items. Its design is [toolbar-design.md](toolbar-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | Any bar of commands driven by data: `FormaSearchToolbar`, `FormaSelectionToolbar`, `FormaStatusStrip`, `FormaComposerCommandBar` and `FormaChrome`'s title bar |

### `FormaVideoPlayer`

Plays a video by its address, from the services its providers know. Its design is [media-design.md](media-design.md).

| | |
|---|---|
| **Old pattern** | CKEditor's **YouTube embed** toolbar button and the raw `<iframe>` markup it pasted into the body |
| **Used in** | Video embeds in articles, a live broadcast player, video listings and a home page's featured video; `FormaFileViewer` for a video |

### `FormaVirtualize`

Forma's virtualization: the part of a long collection around the viewport, and the items its owner pins. Its design is [virtualization-design.md](virtualization-design.md).

| | |
|---|---|
| **Old pattern** | The framework's list virtualization, which keeps no item rendered outside its range, so an item holding the focus is removed as it scrolls away |
| **Used in** | The data view's tree, grid, list and cards, and the list of times `FormaTimePicker` and `FormaDateTimePicker` open |

### `FormaVisibility`

Collapses its content to nothing.

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | `FormaCommandBar`, `FormaStatusStrip`, `FormaShareMenu`, `FormaComposerPluginsBar` and `FormaToolbar` |

### `FormaWindow`

A chrome floated into the top layer as a member of the workspace: activated, stacked, minimized, locked, modal and closed under the manager. Its design is [window-design.md](window-design.md).

| | |
|---|---|
| **Old pattern** | Every floating box — the composer maximized, a dialog — inventing its own chrome |
| **Used in** | The Showcase's window page; a host's floating panels |
