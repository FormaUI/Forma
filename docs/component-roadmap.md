# Forma Component Roadmap

**25 components not yet implemented.** Each moves to [the component catalog](component-catalog.md) when it is approved; the structure lists the ones not yet started, and one in progress stands in the catalog's structure until then.

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
├── (1) Files/
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

src/
└── (1) Forma.Blazor.Gallery/
    └── FormaGallery
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
| 9 | `FormaSortMenu`, `FormaViewButton` | The two controls that exist for the managers' tile grids — a layout with no column headers to sort by and two layouts to switch between. The explorer composes them in place of the sort control and view switch it drew for itself |
| 10 | `FormaLaunchBar` | The bar a workspace is launched from, hosting `FormaRestArea` beside its launchers, so the windows and the way back to them stand in one place |
| 11 | `FormaGallery` | A media-only surface of its own over the items package the explorer shares, built from the library's components with nothing of the explorer's, so it comes after any chrome both need has been abstracted into `Forma.Blazor` and before the editor that its tiles would launch — see [the gallery design](gallery-design.md) |
| 12 | `FormaImageEditor` | The heaviest media component and the last — it needs the surfaces above to launch it, and its pointer handles, coordinate mapping and keyboard path are worth building once the rest of the media flow is settled |

## Components

### `FormaAddButton`

An icon button for adding, labeled in place when the action is primary. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The "new" button in each page's toolbar, opening the new-item modal |
| **Used in** | `FormaToolbar` actions, `FormaMultiSelect`'s add-new affordance, and any editor that appends a row |
| **Glyph** | A **plus** |

### `FormaCapacityIndicator`

A readout of how much of a limit is used, optionally as a progress bar, changing tone as the limit nears.

| | |
|---|---|
| **Old pattern** | `<b>קיבולת:</b> <span dir="ltr">X / Y</span>` — shows used vs. allowed count |
| **Used in** | pages, products, images, orders, leads, users (6 pages) |

### `FormaCategorySelector`

Two lists side by side, the reader moving items between them to choose a set.

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |

### `FormaContextMenu`

The drop-down menu's panel opened by right-click, the context-menu key or Shift+F10, as a shortcut to actions reachable elsewhere.

| | |
|---|---|
| **Old pattern** | None — Coi had no right-click menu; this is new capability rather than parity |
| **Used in** | Grid rows and media tiles, as a shortcut to the actions the overflow menu already lists |

### `FormaCurrencyInput`

A number input with the currency symbol beside it.

| | |
|---|---|
| **Old pattern** | `.form-addon` wrapping input with currency symbol overlay |
| **Used in** | product (price, sale price, cost), store-settings (min order, shipping), coupons (discount amount) |

### `FormaDeleteButton`

An icon button for deleting; confirmation stays the caller's. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The delete icon in each list page's action column, guarded by `AreYouSure(this, 'message')` before the postback |
| **Used in** | Row actions on every list page, and the destructive action in an editor's footer |
| **Glyph** | A **trash can** |

### `FormaDesignPicker`

A grid of design cards with a preview image, applied after confirmation.

| | |
|---|---|
| **Old pattern** | Grid of design template thumbnails (desktop + mobile preview overlay on hover). Click → confirmation dialog → apply design |
| **Used in** | designs popup, welcome wizard |

### `FormaDuplicateButton`

An icon button for making another one; what duplication means stays the caller's. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None in Coi's action columns |
| **Used in** | Row actions on list pages — Épica names duplicate in the same breath as edit, publish, delete and view |
| **Glyph** | **Two overlapping sheets** |

### `FormaEditButton`

An icon button for editing; whether the click navigates or begins an inline edit stays the caller's. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The edit icon in each action column, linking through to the full editor page |
| **Used in** | Row actions on every list page; starts `FormaDataGrid`'s inline edit where the row is edited in place |
| **Glyph** | A **pencil** |

### `FormaExportButton`

A button that starts a server-side export and streams the file from the authorized response.

| | |
|---|---|
| **Old pattern** | Confirm dialog, then `WaitForDownload` navigates straight to a per-entity `.aspx` under `admin/services` that builds a workbook with EPPlus, saves it to a public `up/tmp` path and redirects to it. Generation is synchronous inside that request; a `dwait` cookie set alongside the redirect tells a 500 ms poll to hide the spinner, so nothing waits for readiness before downloading. Columns, headers and the query are hardcoded per entity, and the export always covers every row |
| **Used in** | orders, leads |

### `FormaFilterEditor`

A grid of filter-group cards, each holding a sortable list of values.

| | |
|---|---|
| **Old pattern** | Card-based filter groups, each with a title and sortable option values inside. Add/remove groups and values, search, dropdown filter by group type |
| **Used in** | filters popup |

### `FormaFontPicker`

A searchable grid of fonts in a dialog, each cell set in the font it names.

| | |
|---|---|
| **Old pattern** | Google Fonts grid with Hebrew sample text preview, click-to-select |
| **Used in** | design settings, page editor |

### `FormaGallery`

A media-only browsing surface: every image and video as a date-grouped thumbnail, and folders as date-labeled collections with favorites and categories.

| | |
|---|---|
| **Old pattern** | The image library's tile grid in Coi, which listed every upload as a thumbnail with no grouping and no way to mark or categorize one |
| **Used in** | The media module's own view of its library, beside the explorer that manages it; the picker a cover or a gallery tile is chosen from |

### `FormaImageEditor`

Crops, rotates, flips and resizes an image, describing the edit for the host to perform. Its design is [image-editor-design.md](image-editor-design.md).

| | |
|---|---|
| **Old pattern** | Two surfaces, not one. `jcrop` in `slider.aspx` did a **constrained crop-to-fit**: aspect ratio and minimum size both pinned to the destination slot, the image scaled into a fixed 590×350 box, an initial selection set for the editor, and `tellSelect()` posting `x/y/w/h` plus the target dimensions to `crop.asmx`. Separately, `pops/pixie.aspx` hosted the commercial **Pixie** editor in an `<iframe>`, launched per-entry from the image library |
| **Used in** | Making an arbitrary upload fit a fixed slot — article covers and OG images, video thumbnails, sponsor logos, gallery tiles. Launched from `FormaFileViewer`, the manager and the picker, the same way Coi launched it from its library |

### `FormaLaunchBar`

The bar a workspace's applications are launched from, hosting the rest area beside its launchers.

| | |
|---|---|
| **Old pattern** | None — Coi opened each tool as its own page; launching applications into a workspace of windows is new capability rather than parity |
| **Used in** | The Admin Console's shell, as the bar its applications are launched from |

### `FormaMediaPicker`

A file picker pinned to image, video and audio types, previewing each accepted file through the file viewer. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | None — Coi's library was images-only, and video arrived as a pasted YouTube URL |
| **Used in** | Surfaces taking either an image or a video off the device — Épica's Videos module, article media, and `FormaComposer` |
| **Open** | Whether audio belongs in the same pin as image and video, or earns its own skin |

### `FormaNotesEditor`

A dialog holding a text area and a save button.

| | |
|---|---|
| **Old pattern** | Modal with `<textarea>` for admin notes on orders/leads, save button, timestamp display |
| **Used in** | order detail, lead detail |

### `FormaPermissionMatrix`

A form choosing a role type and, per permission section, its level.

| | |
|---|---|
| **Old pattern** | Radio (FullAdmin vs Custom) + 10 per-section dropdowns with 5 access levels each |
| **Used in** | user editor |

### `FormaProductOptionsEditor`

An editor for a product's option types and their values, each row's fields following the type.

| | |
|---|---|
| **Old pattern** | `options.html` — modal with type picker (8 option types: dropdown, checkbox, radio, cubes, images, color swatches, textbox, textarea), then option editor with name/price/image/color/stock rows per value. Sortable rows |
| **Used in** | product editor |

### `FormaPublishButton`

A stateful icon button for publishing and unpublishing, labeled where primary; scheduling stays outside it. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | Coi's per-item active/publish toggle |
| **Used in** | Editor footers and row actions — Épica names publish beside edit, duplicate, delete and view, and `FormaStatusBadge` already renders the draft/scheduled/published state it changes |
| **Glyph** | A **globe**, struck through when the action is to unpublish |

### `FormaRefreshButton`

An icon button for refreshing that shows and announces its busy state. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None — Coi reloaded the whole page |
| **Used in** | `FormaDataGrid`'s `RefreshDataAsync()`, which is public API today with no affordance of its own, and any list fed by a provider that can go stale |
| **Glyph** | A **circular arrow** |

### `FormaShareButton`

An icon button that raises a share click and nothing more; what sharing means stays the caller's. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | Copying or sending an item's public link, from its editor or its row |
| **Glyph** | **Three connected nodes** |

### `FormaSortMenu`

A drop-down menu of sort choices for a layout with no column headers to sort by.

| | |
|---|---|
| **Old pattern** | Coi sorted by clicking a column header, and had no standalone sort control. Header sorting is **not** what this replaces — `FormaDataGrid` already ships it by passing QuickGrid's `Sortable`, `SortBy`, `InitialSortDirection` and `GridSort<T>` straight through |
| **Used in** | Layouts with no header row to click — the explorer's tile grid, and any card list |
| **Glyph** | **Two opposed arrows** |

### `FormaUpdateButton`

An icon button committing an inline edit, opposite the cross that discards it. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The save button ending an edit postback |
| **Used in** | `FormaDataGrid`'s inline editing beside a cancel control, and editor footers |
| **Glyph** | A **check mark** |

### `FormaViewButton`

A stateful icon button switching between a list and a tile grid, drawing and naming the mode it switches to. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None — Coi's image library had one fixed thumbnail grid |
| **Used in** | `FormaExplorer`, switching between a dense list and thumbnails |
| **Glyph** | **Layout lines**, stacked rows for a list and a square of cells for a tile grid |

