# Forma Component Roadmap

**29 components not yet implemented, 8 of them in progress.** Each moves to [the component catalog](component-catalog.md) when it is approved; the structure lists the 21 not yet started, and the 8 in progress stand in the catalog's structure until then.

## Components Structure

```text
Forma.Blazor/Components/
├── (9) Buttons/
│   ├── FormaAddButton
│   ├── FormaDeleteButton
│   ├── FormaDuplicateButton
│   ├── FormaEditButton
│   ├── FormaExportButton
│   ├── FormaPublishButton
│   ├── FormaRefreshButton
│   ├── FormaShareButton
│   └── FormaUpdateButton
├── (4) Editors/
│   ├── FormaFilterEditor
│   ├── FormaNotesEditor
│   ├── FormaPermissionMatrix
│   └── FormaProductOptionsEditor
├── (1) Files/
│   └── FormaMediaPicker
├── (3) Media/
│   ├── FormaDesignPicker
│   ├── FormaFontPicker
│   └── FormaImageEditor
├── (1) Menus/
│   └── FormaContextMenu
├── (1) Navigation/
│   └── FormaCategorySelector
└── (1) Windows/
    └── FormaLaunchBar

src/Blazor/
└── (1) Forma.Blazor.Gallery/
    └── FormaGallery
```

## Build order

The components still to build, in the order dependency and reuse suggest. What is in progress finishes before the next starts, and the data view group is finished by its plan's explorer step: its gallery step is built with `FormaGallery`, and stays open in the plan until the gallery starts.

| Order | Components | Rationale |
|---|---|---|
| 1 | `FormaDataView`, `FormaDataGrid`, `FormaDataList`, `FormaDataCards`, `FormaDataTree`, `FormaDataSortMenu`, `FormaDataViewSwitch`, `FormaDataViewFooter` | The engine every collection surface shares, with its views and controls; in progress — see [the data view design](data-view-design.md) |
| 2 | `FormaCategorySelector` | Interactive widgets, needed for editor pages |
| 3 | `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components, built with their feature pages |
| 4 | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors, built alongside their pages |
| 5 | `FormaDeleteButton`, `FormaUpdateButton`, `FormaEditButton`, `FormaAddButton`, `FormaDuplicateButton`, `FormaRefreshButton`, `FormaShareButton`, `FormaPublishButton` | The rest of the icon-button vocabulary, so a row's actions stop being markup of each page's own |
| 6 | `FormaContextMenu` | A shortcut to the overflow menu's actions, once there is a menu worth opening that way |
| 7 | `FormaMediaPicker` | The second type-restricted skin over `FormaFilePicker`, beside `FormaImagePicker` |
| 8 | `FormaLaunchBar` | The bar a workspace is launched from, beside the rest area |
| 9 | `FormaGallery` | A media-only surface built on the data view, so it comes after the data view and before the editor its tiles launch — see [the gallery design](gallery-design.md) |
| 10 | `FormaImageEditor` | The heaviest media component and the last, since it needs the surfaces above to launch it |

## Components

### `FormaAddButton`

An icon button for adding. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The "new" button in each page's toolbar, opening the new-item modal |
| **Used in** | `FormaToolbar` actions, `FormaMultiSelect`'s add-new affordance, and any editor that appends a row |

### `FormaCategorySelector`

Two lists side by side, the reader moving items between them to choose a set.

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |

### `FormaContextMenu`

The drop-down menu's panel opened as a context menu, a shortcut to actions reachable elsewhere.

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | Grid rows and media tiles, as a shortcut to the actions the overflow menu already lists |

### `FormaDataCards`

A view over the data view drawing each item as a card: its image, its name and what the caller places beneath. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The legacy image library's tile grid, and the explorer's thumbnails view, each drawing tiles of its own |
| **Used in** | The explorer's cards view, the gallery's browsing, and any page that shows records as tiles |

### `FormaDataGrid`

A view over the data view drawing its items as rows with aligned columns and headers. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | `asp:GridView` with server-side paging, sortable columns, row click-through, checkbox column for mass select, action icon columns (view/edit/delete), status badges, responsive hide classes |
| **Used in** | pages, products, orders, leads, comments, users, coupons (all 7 main list pages) |

### `FormaDataList`

A view over the data view drawing items down the height, a depiction, a name and the caller's secondary text per row. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The explorer's list view, which flows names on its own |
| **Used in** | The explorer's list view, and any page that shows records as a compact list |

### `FormaDataSortMenu`

A drop-down that sorts a data view by the sorts it offers, whichever view shows. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The explorer's sort menu, which sorts and groups its own content area |
| **Used in** | The explorer's toolbar, and any page that shows records as a list or cards |

### `FormaDataTree`

A view over the data view drawing the hierarchy as nodes under their parents. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The page tree in the legacy page editor, and the explorer's folder-tree sidebar, which draws a tree of its own |
| **Used in** | The explorer's sidebar, and any page that navigates a hierarchy |

### `FormaDataView`

The collection component: an engine that manages a set of items and the state around them, handing what is on screen to the view that presents it. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The same browsing built three times: `asp:GridView` over a repository data source on every list page, the explorer's content area, and the gallery's planned engine |
| **Used in** | Every list page under `FormaDataGrid`, the explorer's content area, and the gallery |

### `FormaDataViewFooter`

A data view's footer, its page navigation and its status, which a host arranging the data view itself places. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The explorer's footer beneath its content area, which pages and counts its own items |
| **Used in** | The explorer, beneath its content beside its sidebar, and any page that arranges a data view itself |

### `FormaDataViewSwitch`

A row of toggle buttons switching a data view between the views it registered. Its design is [data-view-design.md](data-view-design.md).

| | |
|---|---|
| **Old pattern** | The explorer's view-mode buttons, which switch its own three views |
| **Used in** | The explorer's toolbar, and any page that offers one set in several views |

### `FormaDeleteButton`

An icon button for deleting. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The delete icon in each list page's action column, guarded by `AreYouSure(this, 'message')` before the postback |
| **Used in** | Row actions on every list page, and the destructive action in an editor's footer |

### `FormaDesignPicker`

A grid of design cards with a preview image, applied after confirmation.

| | |
|---|---|
| **Old pattern** | Grid of design template thumbnails (desktop + mobile preview overlay on hover). Click → confirmation dialog → apply design |
| **Used in** | designs popup, welcome wizard |

### `FormaDuplicateButton`

An icon button for duplicating. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None in the legacy action columns |
| **Used in** | Row actions on list pages |

### `FormaEditButton`

An icon button for editing. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The edit icon in each action column, linking through to the full editor page |
| **Used in** | Row actions on every list page; starts `FormaDataGrid`'s inline edit where the row is edited in place |

### `FormaExportButton`

A button that exports a list to a file.

| | |
|---|---|
| **Old pattern** | A confirm dialog, then a per-entity `.aspx` under `admin/services` that built a workbook of every row and redirected to it |
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

A media-only browsing surface: every image and video as a thumbnail, and folders as collections. Its design is [gallery-design.md](gallery-design.md).

| | |
|---|---|
| **Old pattern** | The legacy image library's tile grid, which listed every upload as a thumbnail with no grouping and no way to mark or categorize one |
| **Used in** | The media module's own view of its library, beside the explorer that manages it; the picker a cover or a gallery tile is chosen from |

### `FormaImageEditor`

Crops, rotates, flips and resizes an image, describing the edit for the host to perform. Its design is [image-editor-design.md](image-editor-design.md).

| | |
|---|---|
| **Old pattern** | Two surfaces: `jcrop` in `slider.aspx` cropping an image to fit a fixed slot, and the commercial Pixie editor hosted in an `<iframe>` from the image library |
| **Used in** | Making an upload fit a fixed slot: article covers and OG images, video thumbnails, sponsor logos, gallery tiles, launched from `FormaFileViewer`, the explorer and the picker |

### `FormaLaunchBar`

The bar a workspace's applications are launched from, hosting the rest area beside its launchers. Its design is [window-design.md](window-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | A host's shell, as the bar its applications are launched from |

### `FormaMediaPicker`

A file picker pinned to image, video and audio types. Its design is [resources-design.md](resources-design.md).

| | |
|---|---|
| **Old pattern** | None — the legacy library was images-only, and video arrived as a pasted YouTube URL |
| **Used in** | Surfaces taking an image or a video off the device: a videos module, article media and `FormaComposer` |

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

A stateful icon button for publishing and unpublishing. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The legacy per-item active/publish toggle |
| **Used in** | Editor footers and row actions |

### `FormaRefreshButton`

An icon button for refreshing, with a busy state. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None — the legacy application reloaded the whole page |
| **Used in** | Any list fed by a provider that can go stale, such as a `FormaDataGrid` |

### `FormaShareButton`

An icon button for sharing. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | None |
| **Used in** | Copying or sending an item's public link, from its editor or its row |

### `FormaUpdateButton`

An icon button committing an inline edit. Its design is [icon-buttons-design.md](icon-buttons-design.md).

| | |
|---|---|
| **Old pattern** | The save button ending an edit postback |
| **Used in** | `FormaDataGrid`'s inline editing beside a cancel control, and editor footers |
