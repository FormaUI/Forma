# Component Inventory

> Identified from the old Coi admin codebase, which is where the component set was derived
> from — the library itself is general-purpose. Each component maps an old Web Forms + jQuery
> pattern to a Blazor component using **only** built-in Blazor components, native HTML elements,
> and browser APIs — no external JS libraries.
>
> Target location: `Forma.UI.Blazor/Components/`
>
> **Naming convention:** All components use the `Forma` prefix for their tag names (e.g., `<FormaModal>`,
> `<FormaTabs>`) to avoid clashes with third-party libraries. CSS classes use the `forma-` prefix.
> C# types (enums, services) rely on namespaces for disambiguation and don't carry the prefix.

---

## File Structure

```
Forma.UI.Blazor/Components/
├── FormaGlyph.razor / .razor.cs / .razor.css  # draws a Forma.UI.Style.Glyphs.Glyph as inline SVG
├── ✅ FormaField.razor / .razor.cs / .razor.css
├── FieldLabelMode.cs                     # enum: Floating, Pinned, Above, Inline
├── (4 / 4) Layout/
│   ├── BreadcrumbItem.cs                # record with Label + Url?, constructor-validated
│   ├── ✅ FormaPageHeader.razor / .razor.cs
│   ├── ✅ FormaToolbar.razor / .razor.cs
│   ├── ✅ FormaContextToolbar.razor / .razor.cs
│   ├── ✅ FormaTabs.razor / .razor.cs
│   └── ✅ FormaTab.razor / .razor.cs
├── (3 / 3) Feedback/
│   ├── ✅ FormaModal.razor / .razor.cs / .razor.js
│   ├── ✅ FormaModal.razor.css
│   ├── ModalSize.cs
│   ├── ✅ FormaConfirmDialog.razor / .razor.cs
│   ├── ✅ FormaConfirmDialog.razor.css
│   ├── ✅ FormaToastContainer.razor / .razor.cs
│   └── ✅ FormaToastContainer.razor.css
├── (2 / 2) Data/
│   ├── ✅ FormaDataGrid.razor
│   └── ✅ FormaPagination.razor
├── (1 / 2) Indicators/
│   ├── ✅ FormaStatusBadge.razor
│   └── FormaCapacityIndicator.razor
├── (3 / 13) Buttons/
│   ├── ButtonSize.cs
│   ├── ButtonType.cs
│   ├── ButtonVariant.cs
│   ├── FormaButtonOptions.cs
│   ├── ✅ FormaButton.razor
│   ├── ✅ FormaCloseButton.razor
│   ├── ✅ FormaEyeButton.razor
│   ├── FormaDeleteButton.razor
│   ├── FormaUpdateButton.razor
│   ├── FormaEditButton.razor
│   ├── FormaAddButton.razor
│   ├── FormaDuplicateButton.razor
│   ├── FormaRefreshButton.razor
│   ├── FormaShareButton.razor
│   ├── FormaPublishButton.razor
│   ├── FormaViewButton.razor
│   └── FormaExportButton.razor
├── (10 / 11) Inputs/
│   ├── SelectOption.cs
│   ├── PasswordStrength.cs
│   ├── ✅ FormaCheckBox.razor
│   ├── ✅ FormaInput.razor
│   ├── ✅ FormaTextArea.razor
│   ├── ✅ FormaToggleSwitch.razor
│   ├── ✅ FormaPasswordBox.razor
│   ├── FormaCurrencyInput.razor
│   ├── ✅ FormaSlider.razor
│   ├── ✅ FormaDatePicker.razor
│   ├── ✅ FormaSelect.razor
│   ├── ✅ FormaColorPicker.razor
│   └── ✅ FormaMultiSelect.razor
├── (2 / 6) Interactive/
│   ├── ✅ FormaSortableList.razor
│   ├── FormaCategorySelector.razor
│   ├── ✅ FormaDropDownMenu.razor
│   ├── ✅ FormaDropDownMenuItem.razor
│   ├── ✅ FormaDropDownMenuDivider.razor
│   ├── FormaOverflowMenu.razor
│   ├── FormaContextMenu.razor
│   └── FormaSortMenu.razor
├── (3 / 6) Files/
│   ├── ✅ FormaFilePicker.razor / .razor.cs / .razor.css / .razor.js
│   ├── RejectedFile.cs                   # record: Name + FileRejectionReason
│   ├── FileRejectionReason.cs            # enum: DisallowedType, TooLarge, TooManyFiles
│   ├── ✅ FormaFileUploader.razor / .razor.cs / .razor.css
│   ├── FormaFileUploaderOptions.cs       # record: how the set runs + which measurements show
│   ├── UploadState.cs                    # enum: Pending, Uploading, Completed, Failed, Skipped, RolledBack, Cancelled
│   ├── UploadFailure.cs                  # record: Name + Error
│   ├── UploadFileStream.cs               # counts bytes read to drive per-file progress
│   ├── FormaImagePicker.razor
│   ├── FormaMediaPicker.razor
│   ├── FormaFileManager.razor
│   └── ✅ FormaFileViewer.razor          # dispatches to Media/ for the type it recognises
├── (1 / 1) Explorer/
│   ├── ✅ FormaExplorer.razor / .razor.cs / .razor.css / .razor.js / .he.json / .es.json
│   ├── FormaExplorerOptions.cs           # record: presentation — dialog, resizable, sidebar, columns, paging
│   ├── FormaExplorerFileOptions.cs       # record: which mutating capabilities the caller allows
│   ├── FormaExplorerConstraints.cs       # record: the ceilings a refusal is measured against
│   ├── FormaExplorerSidebar.razor        # folder tree, resizable through a sash
│   ├── FormaExplorerFolderNode.razor     # one branch of that tree
│   ├── FormaExplorerAddressBar.razor     # back/forward/up plus the breadcrumb menu
│   ├── FormaExplorerToolbar.razor        # selection, upload, view switch, sort
│   ├── FormaExplorerThumbnailsView.razor
│   ├── FormaExplorerDetailsView.razor
│   ├── FormaExplorerListView.razor
│   ├── FormaExplorerThumbnail.razor      # media depiction shared by all three views
│   ├── FormaExplorerActionBar.razor      # per-item operations as an overflow menu
│   ├── ExplorerContext.cs                # cascaded state and per-item actions
│   ├── ExplorerSelection.cs
│   ├── ExplorerAction.cs
│   ├── ExplorerFolderTree.cs             # the folder list indexed once for tree and breadcrumb
│   ├── ExplorerEnums.cs                  # enums: ViewMode, SortField, SortDirection, ThumbSize, DetailsColumns, InfiniteScrollMode
│   ├── ExplorerRequests.cs               # records: the rename, delete, drop, folder mutations and rejection reported as intent
│   ├── ExplorerRejectionReason.cs        # enum: why a resource was refused
│   ├── ExplorerResourceFilter.cs         # which resources are accepted, read from a pattern
│   ├── ExplorerResourceExtensions.cs
│   ├── ExplorerResourceQuery.cs          # record: the window asked for
│   ├── ExplorerResourcePage.cs           # record: the window returned, TotalCount optional
│   └── IExplorerResourcesProvider.cs     # one window at a time, for collections too large to hold
├── (1 / 4) Media/
│   ├── FormaImageEditor.razor / .razor.js
│   ├── ✅ FormaVideoPlayer.razor
│   ├── FormaDesignPicker.razor
│   └── FormaFontPicker.razor
├── (0 / 1) Composer/                         # see docs/composer-design.md
│   ├── FormaComposer.razor                   # engine shell, plugin discovery, editor switcher
│   ├── FormaBasicRichTextEditor.razor        # preset over the same engine
│   ├── FormaLiteRichTextEditor.razor         # preset over the same engine
│   ├── RichTextEditor/                       # RichTextEditorPlugin + feature plugins + dialogs
│   ├── MarkdownEditor/                       # MarkdownEditorPlugin
│   ├── CodeEditor/                           # CodeEditorPlugin
│   └── PageDesigner/                         # PageDesignerPlugin + palette, canvas, widgets
└── (0 / 4) Editors/
    ├── FormaNotesEditor.razor
    ├── FormaPermissionMatrix.razor
    ├── FormaProductOptionsEditor.razor
    └── FormaFilterEditor.razor
```

```
Forma.UI.Style/
├── Css/
│   └── CssColor.cs
└── Glyphs/
    └── Glyph.cs

Forma.Framework.Resources.Abstractions/
├── IResourceContentReader.cs
├── IResourceProvider.cs 
├── IFileStore.cs
├── IVisualResource.cs
├── ITimedResource.cs
├── FileContent.cs
├── ResourceInfo.cs
├── FolderResourceInfo.cs
├── FileResourceInfo.cs
├── ImageResourceInfo.cs
├── VideoResourceInfo.cs
├── AudioResourceInfo.cs
├── DocumentResourceInfo.cs
├── ArchiveResourceInfo.cs
└── MarkupResourceInfo.cs

Forma.Framework.Resources/
└── ResourceFormatter.cs

Forma.UI.Features.Abstractions/
├── INotifyStateChanged.cs
├── Feedback/
│   ├── IToastService.cs
│   ├── IConfirmDialogService.cs
│   ├── ToastMessage.cs
│   ├── ToastType.cs
│   └── ConfirmDialogRequest.cs
├── Indicators/
│   ├── IStatusBadgeCatalog.cs
│   └── StatusBadgeStyle.cs
└── Media/
    ├── IPlayerProvider.cs
    ├── PlayerFrame.cs
    └── PlayerOptions.cs

Forma.UI.Features/
├── StateNotifier.cs
├── Feedback/
│   ├── ToastService.cs
│   └── ConfirmDialogService.cs
└── Indicators/
    └── StatusBadgeCatalog.cs
```

---

## Tier 1 — Core Layout & Infrastructure

These components form the shell of a page. Build first.

### 1. `FormaPageHeader` ✅

| | |
|---|---|
| **Old pattern** | `<h1 class="page-title">` + `<ol class="breadcrumb">` + optional capacity label |
| **Used in** | Every admin page (pages, products, orders, leads, comments, users, coupons, settings, design) |
| **Parameters** | `string Title` (required, validated), `IReadOnlyList<BreadcrumbItem>? Breadcrumbs` (`BreadcrumbItem` record with `Label` + `Url?`, constructor-validated), `RenderFragment? Actions` |
| **Blazor approach** | Pure Razor markup, no interactivity needed (static SSR). Code-behind in `.razor.cs` |

### 2. `FormaToolbar` ✅

| | |
|---|---|
| **Old pattern** | `<div class="card-body">` with search textbox + filter dropdowns + action buttons, wrapping content in a flex row |
| **Used in** | pages, products, orders, leads, comments, users, coupons, filters |
| **Parameters** | `RenderFragment? ChildContent`, `EventCallback<string> OnSearch`, `string? SearchPlaceholder`, `TimeSpan SearchDelay` (default 300ms, validated ≥ 0) |
| **Blazor approach** | Sealed `IDisposable` component with flex layout. Debounced search via `CancellationTokenSource` + `CancelAsync()` + `ContinueWith` pattern. Code-behind in `.razor.cs` |

### 3. `FormaTabs` ✅

| | |
|---|---|
| **Old pattern** | jQuery Tools Tabs: `<ul class="tabs"><li>` + `<div class="panes"><div class="panc">`. Click tab → show corresponding pane |
| **Used in** | page editor (5 tabs), product editor (7 tabs), settings (5 tabs), store-settings (7 tabs), design (5 tabs) |
| **Parameters** | `RenderFragment ChildContent` containing `FormaTab` children. Each `FormaTab` has `string Title` (required, validated), `Glyph? Icon` (drawn by `FormaGlyph`), `RenderFragment ChildContent` |
| **Blazor approach** | Interactive component. `FormaTabs` provides `CascadingValue` of itself; `FormaTab` children register/unregister via `CascadingParameter`. WAI-ARIA `tablist`/`tab`/`tabpanel` roles, `aria-selected`, `aria-controls`. Show/hide panes with CSS `::deep`. No JS needed. Code-behind in `.razor.cs` |

### 4. `FormaModal` ✅

| | |
|---|---|
| **Old pattern** | `Modal('#divId')` / `CloseModal()` — shows a `<div>` overlay with content from hidden divs. Stacking z-index, Escape key to close |
| **Used in** | Every admin page — new page modal, help modals, notes modals, folder dialogs, invoice modals, options editor |
| **Parameters** | `bool Opened`, `EventCallback OnClose`, `string? Title`, `ModalSize Size` (Small/Medium/Large/ExtraLarge/Full, default Medium), `RenderFragment? Body`, `RenderFragment? Footer`, `string? ConfirmText`, `string? CancelText`, `EventCallback OnConfirm`, `EventCallback OnCancel`, `FormaModalOptions Options` (`CloseOnBackdropClick`, `ShowCancelAction`, `ShowConfirmAction`, `CloseOnCancel`, `CloseOnConfirm` — all default `true`) |
| **Blazor approach** | Uses native `<dialog>` element with minimal JS interop (`FormaModal.razor.js`). Supports custom footer content, built-in localized footer actions, or both together. Uses logical CSS so footer/action alignment works in both RTL and LTR. Sealed `IAsyncDisposable` component with code-behind in `.razor.cs` |

### 5. `FormaConfirmDialog` ✅

| | |
|---|---|
| **Old pattern** | `AreYouSure(this, 'message')` — custom confirm that returns bool. Used on delete buttons, destructive actions |
| **Used in** | Every page with delete/remove actions (list pages bulk delete, individual row delete, design change, template swap) |
| **Parameters** | Component is service-driven (no public message/label parameters). Dialog requests are sent via `IConfirmDialogService.ConfirmAsync(string message, string confirmText, string cancelText)` — all arguments validated non-empty |
| **Blazor approach** | Built on `FormaModal` with explicit confirm/cancel actions supplied by the confirm component (modal default actions disabled for this case). Uses `IConfirmDialogService : INotifyStateChanged` (in `Forma.UI.Features`) / `ConfirmDialogService` (in `Forma.UI.Features`), registered via DI, returning `Task<bool>`. Service composes `StateNotifier` for async subscriber notification. Sealed `IDisposable` component with code-behind in `.razor.cs` |

### 6. `FormaToast` ✅

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |
| **Parameters** | Service-driven (no public component parameters). Toast requests sent via `IToastService.ShowAsync(string message, ToastType type = Success, TimeSpan? duration = null)` (default 3s). `ToastType` enum: `Success`, `Error`, `Info`, `Warning`. `IToastService` also exposes `IReadOnlyList<ToastMessage> Toasts`, `Task RemoveAsync(string id)`. Arguments validated (message non-empty, duration non-negative, id non-empty). |
| **Blazor approach** | `IToastService : INotifyStateChanged` (in `Forma.UI.Features`) / `ToastService` (in `Forma.UI.Features`), registered via DI. Service composes `StateNotifier` for async `Func<Task>` subscriber notification. A `FormaToastContainer` (sealed `IDisposable`, code-behind in `.razor.cs`) component at layout level subscribes and renders active toasts. Auto-dismissal via `System.Threading.Timer` per toast. CSS `@keyframes` slide-in animation, type-based color variants. No JS needed |

---

## Tier 2 — Data Display

### 7. `FormaDataGrid<TItem>` ✅

| | |
|---|---|
| **Old pattern** | `asp:GridView` with server-side paging, sortable columns, row click-through, checkbox column for mass select, action icon columns (view/edit/delete), status badges, responsive hide classes |
| **Used in** | pages, products, orders, leads, comments, users, coupons (all 7 main list pages) |
| **Parameters** | `IQueryable<TGridItem> Items` *or* `GridItemsProvider<TGridItem> ItemsProvider` (exactly one), `RenderFragment Columns`, `RenderFragment Status`, `bool EnableCheckedSelection`, `bool EnableRowSelection`, `int? MinimumSelection`, `int? MaximumSelection`, `CheckedSelectionCaption? CheckedSelectionCaption`, `EventCallback<SelectionRejected> OnSelectionRejected`, `IReadOnlyList<TGridItem> SelectedItems` / `EventCallback<IReadOnlyList<TGridItem>> SelectedItemsChanged`, `EventCallback<TGridItem> OnRowClick`, `Func<TGridItem, string?>? RowClass`, `Func<TGridItem, object> ItemKey`, `string QueryParameterNamePrefix`, `bool EnablePagination` (default `true`), `FormaPaginationOptions PaginationOptions`, `bool Virtualize`, `float ItemSize`, `int OverscanCount`, `int PageSize` (drives an internal `PaginationState`; required only while paginating). Public method: `RefreshDataAsync()`. Public read-only properties: `IsMaximumSelectionReached`, `IsMinimumSelectionSatisfied` |
| **Columns** | A plain `RenderFragment` — callers author columns with QuickGrid's own `PropertyColumn`/`TemplateColumn` directly (no custom column wrapper type); `FormaDataGrid` cascades its item type via `[CascadingTypeParameter]` so those tags infer their generic argument without the caller specifying it |
| **QuickGrid passthrough** | Every *column*-level feature works untouched (`Sortable`, `SortBy`, `Align`, `Format`, `HeaderTemplate`, `ColumnOptions`, `PlaceholderTemplate`, `InitialSortDirection`, `GridSort<T>`). Of the *grid*-level parameters, `Items`, `ItemsProvider`, `ItemKey`, `RowClass`, `OnRowClick`, `QueryParameterNamePrefix`, `Virtualize`, `ItemSize` and `OverscanCount` are forwarded; `Pagination` is owned internally and bound only while `EnablePagination` is on, so an unpaginated grid lets virtualization size its scroll range from the real row count instead of a page. Not forwarded: `Class`/`AdditionalAttributes` on the `<table>` (they land on the wrapping `<div>`), `Theme` (conflicts with the design tokens), and `AnchorMode` |
| **Notes** | The grid owns a footer holding the page controls at the inline start and caller-supplied `Status` content (`role="status"`) at the inline end; the footer is skipped entirely when there is no status and only one page, and the page controls specifically are hidden whenever `EnablePagination` is off or everything fits on one page — even if `Status` still has the footer showing. `QueryParameterNamePrefix` defaults to empty, matching QuickGrid. Two grids on one page that share a column title **must** be given distinct prefixes or they share `?sort=`/`?order=` and sort each other. `EnableCheckedSelection`/`EnableRowSelection` require `Items`: select-all and range selection name the whole set, which an `ItemsProvider` could only produce by fetching every page. `PaginationOptions` defaults to the numbered page links **plus** the page-number box, which is a wider default than a bare `FormaPagination` shows on its own. `EnableRowSelection` adds Explorer/Finder-style mouse (plain click replaces, Ctrl/Cmd toggles, Shift ranges, Alt is a no-op) and keyboard (Arrow/Home/End roving focus, same modifiers) selection independent of the checkbox column; the two flags can be combined or used alone. `MaximumSelection` disables further selection once reached (rows visually muted, `OnSelectionRejected` fires on a blocked attempt) but a full deselect is always allowed; the master control (checkbox or `CheckedSelectionCaption`) is disabled only when nothing is selected and selecting everything would exceed the maximum. `MinimumSelection` blocks unchecking a row directly below the floor but does not block a plain-click/range-select replacement or the master "deselect all" — those are reset gestures, not incremental removals, so the floor only binds (and only mutes rows) while `EnableCheckedSelection` is on. `OnSelectionRejected` reports which `SelectionBoundary` blocked the attempt and its limit. `CheckedSelectionCaption` swaps the header checkbox for `SelectAllText`/`DeselectAllText`, either as a static label (only one set) or an interactive toggle button (both set); `CheckedSelectionCaption.Hidden` renders nothing in the header |
| **Sizing** | The rows live in their own scroll region with the footer outside it, and the grid caps itself at `max-block-size: 100%`. That percentage resolves to `none` against an auto-height ancestor, so an ordinary page keeps the plain shrink-to-content layout; inside a height-bounded shell the cap engages, the rows scroll, the column headers stay stuck to the top of the scroll region and the footer stays pinned below it. **The grid takes no height parameter** — a fixed row count or pixel height is a guess about space the component can't see, which wastes room on a large viewport and forces nested scrollbars on a small one. `Virtualize` is meant for that bounded case with `EnablePagination="false"`; in an unbounded page it simply renders everything rather than misbehaving, and `ItemSize` should match the real row height or the scroll range is wrong |
| **Actions & editing** | Row actions are a caller-side pattern over `TemplateColumn` — the grid ships no action-button components, since in Coi those icons navigate to a full editor or raise a confirm dialog rather than editing in place, and a wrapper would be strictly less capable than the `TemplateColumn` it wraps. **Inline editing is built in.** `EnableInlineEditing` + `CloneForEdit` let one row at a time be edited: the grid copies the row, cascades a `RowEditSession` its columns read, validates the copy with a `DataAnnotationsValidator` over its own `EditContext`, and raises `OnRowSave` only once valid — so `CancelEditAsync()` discards and `SaveEditAsync()` commits. Public API: `BeginEdit(item)`, `SaveEditAsync()` (returns whether it was accepted), `CancelEditAsync()`, `IsEditing(item)`, `EditingItem`. Callers drive them from a `TemplateColumn` via `@ref` (null-conditional — the ref is unset on the grid's first render). The edited row drops its selection marker, which takes it out of every mouse and keyboard selection gesture for the duration, so no JS change was needed. Both the editor and the validator sit under a **fixed, keyed** `CascadingValue<EditContext>`: `InputBase` and `DataAnnotationsValidator` both refuse a context swap, so each edit gets its own pair rather than re-parameterising the last |
| **`FormaPropertyColumn<TGridItem, TProp>`** | A `ColumnBase<TGridItem>`-derived column — QuickGrid's column base is public and externally derivable, so this extends the passthrough rather than replacing it, and sits in the same `Columns` fragment as `PropertyColumn`/`TemplateColumn`. Shows `Property`, auto-titles from `[Display]`/`[DisplayName]`/the member name, auto-sorts (a caller-supplied `SortBy` wins), and formats via `Format` against `CurrentCulture`. While its row is edited it swaps to an editor chosen from `TProp` at compile time — `InputText` (string), `InputCheckbox` (bool), `InputDate<T>` (date/time types), `InputNumber<T>` (numeric); anything else, including enums, needs an `EditTemplate`. `Editable="false"` opts a column out, and a property with no setter is never editable. Validation targets the *draft*: the column rewrites `item => item.Member` into `() => draft.Member` for `ValueExpression`, so the `EditContext` identifies the copy's field rather than the untouched row's |
| **Blazor approach** | Extend/wrap `Microsoft.AspNetCore.Components.QuickGrid`, reusing its native `Sortable` columns, `PropertyColumn`/`TemplateColumn`, `RowClass` (per-row CSS) and `OnRowClick` (row click-through) rather than reinventing them. `FormaDataGrid` keeps its own `PaginationState` to drive QuickGrid's slicing but renders `FormaPagination` — not QuickGrid's `Paginator` — for the page controls, translating `PaginationState` to `FormaPagination`'s plain parameters; the grid adapts to the paginator, never the reverse. Add: checkbox column (mass-select, pairs with `FormaContextToolbar`) with a tri-state select-all in its header that spans the whole `Items` set rather than the visible page — QuickGrid owns the sort and the page slice, so the rows on screen are not a set the component can name. Action-icon column. Responsive column hiding is optional, not required parity — Coi itself only wraps the table in a scrolling `.table-responsive` container, it never hides columns |

### 8. `FormaContextToolbar` ✅

| | |
|---|---|
| **Old pattern** | "Select all" checkbox in header + per-row checkboxes. When items selected, show action bar (delete selected, change status, export). `InitTableEvents()` / `$('.cbx').prop('checked', ...)` |
| **Used in** | pages, products, orders, leads, comments (5 of 7 list pages) |
| **Parameters** | `int Count`, `string? Caption` (the localized selected-item count when omitted), `RenderFragment ChildContent` (action buttons), `EventCallback OnClear` |
| **Blazor approach** | Pairs with `FormaDataGrid`'s checked selection, taking only the count so it stays non-generic. Collapses through `grid-template-rows: 0fr → 1fr` rather than an absolutely positioned cross-fade, so no wrapper positioning is imposed on the caller. `inert` sits on the controls alone, leaving the caption's live region in the accessibility tree so the first count change is announced |
| **Notes** | The conditional sibling of `FormaToolbar`. Neither sets `role="toolbar"` — an ARIA toolbar owes arrow-key roving focus between its controls, which neither implements, so both stay plain button groups |

### 38. `FormaField` ✅

| | |
|---|---|
| **Old pattern** | `<label class="lblForm">` over a `.form-control`, floating on focus or value, with `<span class="mand">` for required and the error text below |
| **Used in** | Chrome for every labelled form component — `FormaInput`, `FormaTextArea`, `FormaPasswordBox`, `FormaSelect`, `FormaMultiSelect`, `FormaDatePicker`, `FormaColorPicker`, `FormaSlider` and `FormaToggleSwitch` |
| **Parameters** | `string? Label`, `string? TargetId`, `bool Required`, `bool Multiline`, `FieldLabelMode LabelMode`, `RenderFragment ChildContent` (the input element), `RenderFragment? Validation` |
| **Blazor approach** | Renders the wrapper, label and validation slot, and styles the input element through `::deep .forma-field-input` — the one mechanism that reaches markup a *consuming* component rendered, since CSS isolation otherwise scopes each component's sheet to its own output. Non-generic: the consumer passes its own `<ValidationMessage For="...">` into `Validation` rather than the chrome taking a typed expression. `Multiline` anchors the label to the top, because an input element taller than one line has no vertical centre to sit against |
| **Notes** | Exists so the floating label, border, focus ring and validation styling live in one file. Without it each labelled form component reimplements ~110 lines and they drift apart |

### 39. `FormaTextArea` ✅

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="MultiLine">` — the same `.form-control` class as a single-line field, plus `textarea { resize: vertical }` and a label offset (`.row textarea ~ label.lblForm { top: 13px }`) |
| **Used in** | SEO head, meta description, store details — and in Épica, article excerpts and summaries |
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `int Rows` (default 3), `int? MaxLength`, `bool ShowCharacterCount` |
| **Blazor approach** | `InputBase<string?>` rendering a `<textarea>` into `FormaField` with `Multiline`. Owns only the multi-line deltas — `resize: vertical` and a minimum height — so it cannot drift from `FormaInput`. No JS: autofill does not apply to a textarea |
| **Notes** | `ShowCharacterCount` floats a running count in the field's end corner, tracking the resizable edge from normal flow rather than absolute positioning. The count is isolated as `<bdi dir="ltr">` so an RTL line cannot render "5 / 280" as "280 / 5", and the glyphs are hidden from assistive technology in favour of a visually hidden sentence the field is described by |

### 9. `FormaStatusBadge` ✅

| | |
|---|---|
| **Old pattern** | Colored div/span with status text, different background per status (orders: 17 statuses with distinct colors; leads: 7 statuses) |
| **Used in** | orders list/detail, leads list/detail |
| **Parameters** | `string Text`, `string Color` (or `StatusType` enum mapping) |
| **Blazor approach** | Simple span with CSS class or inline `style` for background color. Static SSR is sufficient |

### 10. `FormaCapacityIndicator`

| | |
|---|---|
| **Old pattern** | `<b>קיבולת:</b> <span dir="ltr">X / Y</span>` — shows used vs. allowed count |
| **Used in** | pages, products, images, orders, leads, users (6 pages) |
| **Parameters** | `int Used`, `int Max`, `string? Label` |
| **Blazor approach** | Static SSR. Optionally add a progress bar visual. Change color when approaching limit |

### 11. `FormaPagination` ✅

| | |
|---|---|
| **Old pattern** | Server-rendered page links: `<a class="page">1</a> <a class="page">2</a>...`, custom paging in code-behind with `PageSize`, `PageIndex`, total count |
| **Used in** | All list pages, image library |
| **Parameters** | Standalone, no knowledge of QuickGrid or any other component: `int TotalItems`, `int PageSize`, `int CurrentPage`, `EventCallback<int> OnPageChanged`, `FormaPaginationOptions Options` |
| **Options** | Which parts are shown — page buttons, the page-number box, the Go button — travel together in a `FormaPaginationOptions` record rather than one parameter each. A host that composes the pager therefore forwards a single parameter, and a new option later costs the record plus this component and touches no host |
| **Blazor approach** | A self-contained paginator, usable on its own (e.g. the image library) or composed into `FormaDataGrid`. Dependency direction is one-way: larger components adapt to `FormaPagination`, it never adapts to them |

---

## Tier 3 — Form Components

### 12. `FormaInput` ✅

| | |
|---|---|
| **Old pattern** | `<asp:TextBox>` + `<label class="lblForm">` positioned over input, animates up on focus/has-value. Validation error shown below. Optional mandatory indicator `<span class="mand">` |
| **Used in** | category popup, user popup, filters, import, contact module, and all form pages |
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `string? Placeholder`, `string Type = "text"` |
| **Blazor approach** | `InputBase<string?>` rendering an `<input>` into `FormaField`, which owns the floating label, required marker and validation message. Autofill is detected in a small module that adds `.has-value`, because an autofilled field does not fire `input` and `:placeholder-shown` alone would leave the label sitting over text |

### 13. `FormaToggleSwitch` ✅

| | |
|---|---|
| **Old pattern** | jQuery Toggles plugin — styled checkbox as iOS-style toggle with on/off labels, CSS animated slider |
| **Used in** | page editor (SEO, comments, password), product (active, featured), settings (multiple), store-settings (multiple), coupons (active), user (active), order (paid) |
| **Parameters** | `@bind-Value` (bool), `string? OnLabel`, `string? OffLabel`, `bool Disabled` |
| **Blazor approach** | Pure CSS toggle (hidden checkbox + styled `<label>` with `::before`/`::after` pseudo-elements for the sliding knob). No JS needed |

### 14. `FormaColorPicker` ✅

| | |
|---|---|
| **Old pattern** | `tcp.js` custom color picker — click swatch → popup with gradient/hue selectors, hex input, opacity slider (`no-alpha` class disables) |
| **Used in** | design (bg, primary, secondary, text, sidebar, link colors), settings (popup bg), store-settings (e-commerce accent), slider (bg, arrow, video filter), forms (text, button bg/text), product (badge) |
| **Parameters** | `@bind-Value` (string hex), `bool ShowAlpha = false`, `string? Label` |
| **Blazor approach** | Use native `<input type="color">` for the basic picker. For the full experience (hex input, swatch preview, optional alpha), build a small interactive component. Clicking the swatch opens a popup with the native color input + hex `InputText`. No external JS |

### 15. `FormaDatePicker` ✅

| | |
|---|---|
| **Old pattern** | EasePick CDN library with Hebrew locale, year dropdown, reset button, min-date lock |
| **Used in** | coupons (start/end dates) |
| **Parameters** | `@bind-Value` (DateOnly?), `DateOnly? MinDate`, `string? Label` |
| **Popup placement** | The calendar is a `popover`, so it renders in the **top layer** and no scrolling or clipping ancestor can cut it off — it works inside a grid's scroll region, a modal body, or any `overflow: auto` box. The top layer keeps no layout tie to the trigger, so the shared `wwwroot/scripts/AnchoredPopup.js` module measures and applies the position: it hangs from the trigger's inline-start edge (mirrored in RTL), flips above when there is no room below, clamps into the viewport, and follows scroll and resize. One positioning path for every browser rather than a CSS/JS split. That module is deliberately component-agnostic — `FormaSelect`, `FormaMultiSelect` and `FormaDropDownMenu` are the same trigger-plus-anchored-panel shape and should import it rather than re-solve the clipping. `popover="manual"` keeps the existing backdrop as the dismissal mechanism, which also sidesteps the iOS 17–18.2 bug where a popover ignores a tap outside it |
| **Blazor approach** | Use Blazor `InputDate` which renders native `<input type="date">`. Browser provides locale-aware calendar. For reset, add a clear button. For min-date, use `min` HTML attribute |

### 16. `FormaSlider` ✅

| | |
|---|---|
| **Old pattern** | `<input type="range">` + hidden field + live value display label |
| **Used in** | forms (border radius), slider (duration), design/edit (font size, site width) |
| **Parameters** | `@bind-Value` (any `INumber<TValue>`), `string Label`, `TValue Min`, `TValue Max`, `TValue Step = TValue.One`, `string? Unit` ("px", "%", "s"), `bool Disabled` |
| **Blazor approach** | `InputBase<TValue>` rendering a native `<input type="range">`, generic over any `INumber<T>` so an `int` and a `double` slider share one component. The current value is shown beside the label rather than in a second field. Tints the native track and thumb with `accent-color` rather than hand-painting a filled track, so the browser's own RTL mirroring stays correct with no direction-aware calculation of its own to keep in step with it |
| **Notes** | The displayed value is `aria-hidden`; a screen reader instead hears `aria-valuetext`, set only when `Unit` is given — without one the native `valuenow` already says the number, and setting it anyway would repeat the same words the input already announces on its own. The value's colour deliberately is not the muted caption token: at its font size that measures under 4.5:1, so it stays at full text contrast and is set apart by size alone, the same trade the strength meter in `FormaPasswordBox` makes |

### 17. `FormaPasswordBox` ✅

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="Password">` + `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` toggles between password/text |
| **Used in** | user editor, settings (API keys), admin login |
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `bool ShowToggle = true`, `string AutoComplete = "current-password"`, `bool ShowStrengthMeter`, `Func<string, PasswordStrength>? StrengthEvaluator`, `bool ShowStrengthAdvice`, `Func<string, IEnumerable<string>>? StrengthAdvisor`, `IEnumerable<string?>? ForbiddenTerms` |
| **Blazor approach** | Composes `FormaField` for the label, border and validation chrome. `InputBase<string?>` with a dynamic `type` toggled by a button click in C#; the value stays `dir="ltr"` regardless of page direction so a revealed password never reorders, with the gutter, alignment and Edge's own injected `::-ms-reveal` handled per direction in CSS. No JS beyond the existing autofill-detection module `FormaInput.razor.js` already uses |
| **Notes** | The strength meter rates length (discounted for a repeating unit), case mix, digits and symbols into a five-step `PasswordStrength` enum, shown as filled segments plus a translated phrase — never colour alone. `ShowStrengthAdvice` lists the specific criteria a value still lacks, sharing its checks with the rater so the two cannot disagree. `ForbiddenTerms` rejects a password that draws on another field's value (email, phone) as a whole or by any alphanumeric fragment of at least four characters, through a `ValidationMessageStore` rather than a private error, so it participates in the form's own validation summary |

### 18. `FormaCurrencyInput`

| | |
|---|---|
| **Old pattern** | `.form-addon` wrapping input with currency symbol overlay |
| **Used in** | product (price, sale price, cost), store-settings (min order, shipping), coupons (discount amount) |
| **Parameters** | `@bind-Value` (decimal), `string Symbol = "₪"`, `string? Label` |
| **Blazor approach** | `InputNumber` inside a flex container with the currency symbol as an adjacent `<span>`. CSS for visual grouping |

### 19. `FormaMultiSelect` ✅

| | |
|---|---|
| **Old pattern** | `.multi-input` with `.tags` + `.multi-dropdown` list. `$('#el').multi()` plugin. Tags display selected items, click X to remove. Dropdown with checkboxes. Optional "add new" button |
| **Used in** | page editor (categories), product editor (categories), coupons (categories) |
| **Parameters** | `@bind-Value` (List<int>), `IEnumerable<SelectOption> Options`, `string Placeholder`, `bool AllowAdd = false`, `EventCallback<string> OnAddNew` |
| **Blazor approach** | Interactive component. Clicked → show dropdown with checkboxes. Selected items shown as removable tags above/inside the input area. Search/filter within dropdown. No external JS |

### 20. `FormaSelect` ✅

| | |
|---|---|
| **Old pattern** | JS replaces native `<select>` with styled div: `.custom-select > .cs-val` (display) + `.cs-options > .cs-option` (dropdown). Click to open, click option to select, click outside to close |
| **Used in** | Multiple admin pages for styled dropdowns |
| **Parameters** | `@bind-Value` (any `TValue`, nullable to allow no selection), `IReadOnlyList<SelectOption<TValue>> Options`, `string? Label`, `string? Placeholder`, `bool Required`, `bool Disabled` |
| **Blazor approach** | A `<button>` trigger plus an anchored, keyboard-navigable listbox popup, sharing `AnchoredPopup.js` with `FormaDatePicker` and `FormaMultiSelect` so the list escapes any clipping ancestor. `InputBase<TValue>` directly, matching `InputDate<TValue>`/`InputNumber<TValue>` — the caller supplies a nullable `TValue` (`int?`, `string`, `MyEnum?`) when "nothing selected" needs representing, rather than the component forcing that shape itself |
| **Notes** | Composes `FormaField` in `Pinned` mode, as `FormaMultiSelect` and `FormaDatePicker` do: the trigger always shows content of its own, so the label stays floated rather than sitting over it. Opens focused on the current selection, the way a native `<select>` does. The selected option is marked by a checkmark plus bold weight, not colour alone |

---

## Tier 4 — Interactive Widgets

### 21. `FormaSortableList<TItem>` ✅

| | |
|---|---|
| **Old pattern** | jQuery Dragsort / HTML5 sortable — drag items to reorder. On drop, serialize new order and POST to `sorting.asmx/Sort`. Used for menus, galleries, sliders, categories, product options, delivery methods, form fields, filters |
| **Used in** | menu sort page, gallery module, slider editor, category module, product options, store-settings delivery, contact form builder, filters editor |
| **Parameters** | `@bind-Items` (List<TItem>), `RenderFragment<TItem> ItemTemplate`, `string? DragHandleClass`, `EventCallback<List<TItem>> OnReorder` |
| **Blazor approach** | Uses native HTML5 Drag and Drop API (`draggable`, `@ondragstart`, `@ondragover`, `@ondrop`). Track drag source/target index, reorder the list in C#, re-render. Minimal JS interop (only if `preventDefault` is needed on `dragover`) |

### 22. `FormaImagePicker` ✅

| | |
|---|---|
| **Old pattern** | The browse button in `pops/images.aspx`'s upload panel, filtered to image types |
| **Used in** | Anywhere a *new* image comes off the user's device — page editor (cover image, OG image), product editor (images, gallery), design (logo, bg, top image), slider (slide bg, elements) |
| **Parameters** | The same set as `FormaFilePicker` (#34) minus `Accept`, which it pins |
| **Blazor approach** | A thin skin over `FormaFilePicker` that pins `Accept` to image types and renders a thumbnail preview per accepted file. Carries no dialog, drop, validation or transfer logic of its own |
| **Notes** | Picks from the **device**, not the library. Choosing an image that is already on the server is `FormaExplorer` (#53) — the two are different questions and answering both with one component is what made the old naming wrong |

### 58. `FormaMediaPicker`

| | |
|---|---|
| **Old pattern** | None — Coi's library was images-only, and video arrived as a pasted YouTube URL |
| **Used in** | Surfaces taking either an image or a video off the device — Épica's Videos module, article media, and `FormaComposer` (#36) |
| **Parameters** | The same set as `FormaFilePicker` (#34) minus `Accept`, which it pins |
| **Blazor approach** | The same thin skin as `FormaImagePicker` with a wider pin: image, video and audio types. Preview per accepted file goes through `FormaFileViewer` (#56), so a picked video shows as a player rather than a filename |
| **Notes** | Exists so "media" is one decision made once, rather than each caller writing its own `accept` string and drifting. **To settle when built:** whether audio belongs in the same pin as image and video, or earns its own skin |

### 23. `FormaCategorySelector`

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |
| **Parameters** | `@bind-SelectedIds` (List<int>), `IEnumerable<Category> AvailableCategories` |
| **Blazor approach** | Two `<ul>` lists side by side. Click item → move between lists (C# list manipulation + re-render). CSS animation via `transition` on list items. No external JS |

### 24. `FormaDropDownMenu` ✅

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` → shows a `dropdown-menu dropdown-menu-arrow` positioned below a button. Click outside to close |
| **Used in** | order detail (options: print, notes, invoice), and various action menus |
| **Parameters** | `RenderFragment Trigger` (the trigger's content), `RenderFragment ChildContent` (menu items), `FormaButtonOptions TriggerOptions`. `FormaDropDownMenuItem` takes `string? Href`, `bool Disabled`, `EventCallback OnClick`; `FormaDropDownMenuDivider` takes none |
| **Blazor approach** | The component renders its own trigger as a `FormaButton` rather than accepting a whole button from the caller, so it owns `aria-haspopup`, `aria-expanded` and `aria-controls` instead of hoping a supplied fragment carries them; a wrapper span gives the popup something to measure and catches the keydown that a component tag cannot. The panel is a `popover="manual"` in the top layer positioned by the shared `AnchoredPopup.js`, with a backdrop owning dismissal. Items register with the menu through a `CascadingValue`, as `FormaTab` does with `FormaTabs`, so arrow, Home and End navigation works over whatever the caller nested. A colocated `.razor.js` cancels only the four navigation keys, since Blazor fixes `preventDefault` at render time and a blanket one would also block Enter, Space and Tab |
| **Notes** | An item's activation closes the menu and returns focus to the trigger, which needs an explicit `StateHasChanged` because the state being changed belongs to the parent, not the child whose handler ran. Which item to focus on open is resolved after the render that creates them — indexing the list beforehand reads an empty one |

---

## Tier 5 — Specialized / Page-Specific Components

These are more complex, possibly page-specific, but still benefit from being components.

### 25. `FormaNotesEditor`

| | |
|---|---|
| **Old pattern** | Modal with `<textarea>` for admin notes on orders/leads, save button, timestamp display |
| **Used in** | order detail, lead detail |
| **Parameters** | `string? Notes`, `EventCallback<string> OnSave` |
| **Blazor approach** | `FormaModal` containing `FormaTextArea` + save button. Simple composition |

### 26. `FormaPermissionMatrix`

| | |
|---|---|
| **Old pattern** | Radio (FullAdmin vs Custom) + 10 per-section dropdowns with 5 access levels each |
| **Used in** | user editor |
| **Parameters** | `@bind-Value` (UserPermissions model) |
| **Blazor approach** | `EditForm` with `InputRadioGroup` for role type, conditionally rendered grid of `InputSelect` dropdowns for each permission section |

### 27. `FormaProductOptionsEditor`

| | |
|---|---|
| **Old pattern** | `options.html` — modal with type picker (8 option types: dropdown, checkbox, radio, cubes, images, color swatches, textbox, textarea), then option editor with name/price/image/color/stock rows per value. Sortable rows |
| **Used in** | product editor |
| **Parameters** | `@bind-Options` (List<ProductOption>) |
| **Blazor approach** | Complex interactive component. Type selection grid → option value editor form with `FormaSortableList`. Each row has contextual fields based on type |

### 28. `FormaFilterEditor`

| | |
|---|---|
| **Old pattern** | Card-based filter groups, each with a title and sortable option values inside. Add/remove groups and values, search, dropdown filter by group type |
| **Used in** | filters popup |
| **Parameters** | `@bind-Filters` (List<Filter>) |
| **Blazor approach** | Interactive card grid. Each card is a filter group with `FormaSortableList` of values inside. Reuses `FormaInput` for inputs |

### 29. `FormaExportButton`

| | |
|---|---|
| **Old pattern** | Confirm dialog, then `WaitForDownload` navigates straight to a per-entity `.aspx` under `admin/services` that builds a workbook with EPPlus, saves it to a public `up/tmp` path and redirects to it. Generation is synchronous inside that request; a `dwait` cookie set alongside the redirect tells a 500 ms poll to hide the spinner, so nothing waits for readiness before downloading. Columns, headers and the query are hardcoded per entity, and the export always covers every row |
| **Used in** | orders, leads |
| **Parameters** | `string ExportUrl`, `string Label = "ייצוא"` |
| **Blazor approach** | Button triggers server-side export. Use `NavigationManager` or JS interop `window.location.href` to initiate download. Consider `StreamRendering` for progress indication. Stream the file from the authorized response rather than parking it under `wwwroot`, which exposed personal data at a guessable URL |

### 30. `FormaDesignPicker`

| | |
|---|---|
| **Old pattern** | Grid of design template thumbnails (desktop + mobile preview overlay on hover). Click → confirmation dialog → apply design |
| **Used in** | designs popup, welcome wizard |
| **Parameters** | `IEnumerable<DesignTemplate> Templates`, `EventCallback<int> OnSelect` |
| **Blazor approach** | CSS grid of cards with image + hover overlay. Confirmation via `FormaConfirmDialog` before applying |

### 31. `FormaFontPicker`

| | |
|---|---|
| **Old pattern** | Google Fonts grid with Hebrew sample text preview, click-to-select |
| **Used in** | design settings, page editor |
| **Parameters** | `@bind-Value` (string font name), `IEnumerable<FontInfo> Fonts` |
| **Blazor approach** | Searchable grid inside `FormaModal`. Each cell shows font name + sample text styled with that font. Load font CSS on-demand |

### 32. `FormaButton` ✅

| | |
|---|---|
| **Old pattern** | `.btn`, `.btn-primary`, `.btn-sec`, `.btn-thrd`, `.btn-mini`, `.btn-load` (fa-3.6.5.css) — the shared button vocabulary every admin page's action bar, modal footer, and grid row built on top of |
| **Used in** | Single source of truth for buttons across the library — `FormaDataGrid` row/inline-edit actions, and the retrofit target for `FormaModal`, `FormaConfirmDialog`, `FormaPagination`, and `FormaDatePicker` |
| **Parameters** | `ButtonVariant Variant` (Secondary/Primary/Tertiary/Danger/Ghost, default Secondary), `ButtonSize Size` (Normal/Mini, default Normal), `ButtonType Type` (Button/Submit/Reset, default Button), `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, `RenderFragment? ChildContent` |
| **Blazor approach** | Plain `<button>` with a computed CSS class per variant/size. `Busy` announces itself via `aria-busy`/`aria-disabled` and blocks the click handler instead of using the native `disabled` attribute, so a busy button stays focusable and keeps its place in the tab order. Sealed component with code-behind in `.razor.cs` |

### 33. `FormaCheckBox` ✅

| | |
|---|---|
| **Old pattern** | Plain `<input type="checkbox">` styled by `fa-3.6.5.css` — no shared component, each admin page restated its own markup |
| **Used in** | Single source of truth for checkboxes across the library — the retrofit target for `FormaDataGrid`'s header/row selection checkboxes and `FormaMultiSelect`'s option checkboxes |
| **Parameters** | `bool Checked`, `EventCallback<bool> CheckedChanged`, `bool Indeterminate`, `bool Disabled`, `bool PreventDefault`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | Plain `<input type="checkbox">`, not `InputBase<bool>` — callers that need form integration wrap it themselves. `Indeterminate` is IDL-only with no content attribute, so it is applied through a small JS interop module, and only when a checkbox actually becomes indeterminate. `PreventDefault` suppresses the native toggle so a caller that computes its own next state — `FormaDataGrid`'s tri-state select-all, which can reject a click outright — owns the checked state entirely; `OnClick` stays a plain notification that changes no behaviour on its own. Sealed component with code-behind in `.razor.cs` |

### 34. `FormaFilePicker` ✅

| | |
|---|---|
| **Old pattern** | The file input in `pops/images.aspx`'s upload panel — the browse button that opened the OS dialog, before anything had been posted anywhere |
| **Used in** | Every route by which a file leaves the user's machine: `FormaFileUploader` (#35) consumes its selection, and `FormaImagePicker` (#22) and `FormaMediaPicker` (#58) are type-restricted skins over it |
| **Parameters** | `EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected` (required), `EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected`, `string? Accept`, `bool AllowMultipleFiles = true`, `long? MaxFileSize`, `int? MaxSelectedFiles`, `string? Prompt` |
| **Blazor approach** | `InputFile` behind a styled drop zone, the input nested inside the `<label>` so a click anywhere opens the dialog with no script at all. The DOM exposes file drag-and-drop only through its own `drop` event, so a colocated module assigns the dropped `DataTransfer.files` onto the input and dispatches `change` — a drop and a click therefore converge on one code path and nothing downstream needs to know which happened. Size, count and type are validated in the component, which **reports rejects rather than throwing**, and judges each file on its own: a five-file drop against a cap of two yields two accepted and three rejected rather than losing the batch. `Accept` is re-checked in C# because a drop never consults the dialog's own filter |
| **Notes** | Picks from the **user's device**. It opens an OS dialog or takes a drop and yields `IBrowserFile`s the server has never seen — it is not a browser of stored files, which is what `FormaExplorer` (#53) and `FormaFileManager` (#54) are. Owning no transfer, it deliberately shows no progress: a progress bar measures bytes moving, and moving them is `FormaFileUploader`'s job |

### 35. `FormaFileUploader` ✅

| | |
|---|---|
| **Old pattern** | The handler `pops/images.aspx` posted to — the half that actually wrote the bytes into the media folder and drove the progress bar |
| **Used in** | Behind every surface that accepts a new file: `FormaExplorer` (#53), `FormaFileManager` (#54), `FormaComposer` (#36), and any editor taking a file directly rather than through a library |
| **Parameters** | `IReadOnlyList<IBrowserFile>? Files`, `IFileStore? Store`, `FormaFileUploaderOptions Options`, `EventCallback<IReadOnlyList<ResourceInfo>> OnCompleted`, `EventCallback<UploadFailure> OnFailed`. Public API: `StartAsync()`, `Cancel()`, `IsUploading` |
| **Options** | How the set runs and what it reports travel together in a `FormaFileUploaderOptions` record rather than one parameter each: `AutoStart` (default `true`), `StopOnFailure`, `RollbackOnFailure`, and the measurements `ShowByteCount`, `ShowElapsed`, `ShowTransferRate`, `ShowEstimate`, `ShowTotalMetrics`, `ShowSummary`. `Default` shows the progress bar and state alone; `Detailed` turns every measurement on |
| **Blazor approach** | Takes the files a `FormaFilePicker` produced and moves them one at a time, owning the entire transfer: opening each stream with the file's own size as the ceiling, pumping it, counting bytes through a wrapping stream to drive a per-file progress bar, disposing it, and cancelling the set. **Where the bytes land stays the host's**, supplied as an `IFileStore` — `SaveAsync` takes a file's content and returns the `ResourceInfo` it became, and `DeleteAsync` reports whether it removed one. The store comes from the `Store` parameter or, when that is unset, from the host's own registration, so one registration can serve every instance. Progress arrives on whichever thread the store's read continuation resumed on, so both the list mutation and the render are marshalled back through `InvokeAsync` |
| **Notes** | Returning `ResourceInfo` is what makes the chain close: it is exactly what `FormaExplorer` and `FormaFileManager` list, so an upload feeds the browsing surface with no host-side translation. `IFileStore` and `FileContent` live in `Forma.Framework.Resources.Abstractions` beside `ResourceInfo`, which carries no ASP.NET Core reference, so a host implements a store from a plain class library. A failed file leaves the rest of the set running unless `StopOnFailure` marks the remainder skipped, and `RollbackOnFailure` removes what already landed — a store that reports it did **not** remove a file is itself a failure rather than a silent success. **Settled when built:** it composes beside a picker rather than rendering one, and retrying a single failed file is not in the initial release |

### 36. `FormaComposer`

> **[docs/composer-design.md](docs/composer-design.md) is authoritative for this component.** The rows below are the summary; the design document owns the plugin model, the assembly split, and the phases.

| | |
|---|---|
| **Old pattern** | **Two Coi surfaces, not one.** The rich text editor was CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px; custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`; `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction, and `allowedContent = true` — no content filtering whatsoever. The larger surface was the **page designer**, `admin/pops/template.aspx` over `Controls/Edit/`: a drag-and-drop page builder with a side palette, a live canvas, per-element delete/style/edit chrome, jquery-sortable reordering, column and element resizing, and a settings editor per element kind in `elm.js`. One entry point, `OpenTemplate(type, title)`, drove seven targets by numeric type — product page templates, the product tile, headings, rich content, popups and the custom dropdown menu |
| **Used in** | page editor, product description, product and category templates, popups, custom menus — and in Épica, the News article body |
| **Toolbar (from Coi's `Website` preset)** | Source · Preview · Templates — Cut/Copy/Paste/PasteText/PasteFromWord · Undo/Redo · SelectAll · RemoveFormat — Smiley · SpecialChar · LineHeight · Maximize — Bold/Italic/Underline/Strike — Numbered & bulleted lists — Justify right/center/left/block · BidiRtl/BidiLtr — Link/Unlink/Anchor — YouTube embed · Image · Table · HorizontalRule — Format · Font · FontSize — TextColor — image library. Subscript/Superscript were explicitly removed; Forma keeps them available but outside the default preset |
| **Designer palette (from `template.aspx`)** | Text, image, Facebook, contact form, table/cells, horizontal rule, button, image slider, add-ons, image gallery, YouTube/video, map, spacer, code/script — plus host-domain token elements swapped in per target: product name, summary, images, price, details, body, options, buy button and quantity; the mini-product tile variants; and page name, summary, image and read-more for a category tile |
| **Parameters** | `@bind-Value` (`ComposerDocument` — content plus the format it is written in), `FormaComposerOptions Options`, the registered plugin set, and `EventCallback`s for media requests and document change. Presets are separate components rather than a toolbar enum: `FormaComposer` (everything), `FormaBasicRichTextEditor`, `FormaLiteRichTextEditor` |
| **Blazor approach** | **Forma's own editor, and an engine rather than a single component.** Every capability is a plugin, *including each editing surface* — `RichTextEditorPlugin`, `MarkdownEditorPlugin`, `CodeEditorPlugin` and `PageDesignerPlugin` implement the same `IComposerEditorPlugin`, so there is no closed set of modes and a host can add its own. The designer adds a second extension point, `IComposerDesignerPlugin` contributing `ComposerWidget`s, because half Coi's palette was host-domain and Épica's would be too. **A text widget hosts `FormaRichTextEditor`**, which is the recursion Coi had through `CKEDITOR.inline` and the reason the component is called a composer at all. Chrome composes the library: `FormaSelect` for format/font/size, `FormaDropDownMenu` for overflow, `FormaColorPicker` for colour, `FormaExplorer` for stored media, `FormaFilePicker` plus `FormaFileUploader` for uploads, `FormaVideoPlayer` for embeds. Contracts live in `Forma.UI.Features.Composer.Abstractions` so a host adopting the composer does not inherit the whole feature-abstractions surface, and satellites (`.Markdown`, `.TextMate`, `.KaTeX`, `.HtmlSanitizer`) carry the external dependencies one package each. Undo/redo, selection, clipboard, conversion, sanitisation and asset loading are engine concerns shared by every editor — so an undo inside a text widget and a widget reorder sit on one stack |
| **Designer document** | A **`ComposerWidgetTree` — JSON, with HTML as output rather than storage.** Each node carries an `InstanceId`, the `WidgetId` of its kind, canvas-edited `Content`, panel-edited `Settings` as an opaque `JsonElement`, and `Children`; columns are nodes rather than an array on the row. Coi saved HTML plus a scoped `<style>` block, which froze rendering decisions at save time — a baked `<img src>` and `style="width:60%"` that a CDN move or theme change could never reach — and never escaped the tree anyway, since recovering a widget needs `data-widget` and `data-settings` attributes read back with an HTML parser. `FormaWidgetRenderer` walks the tree for public pages under static SSR. Document metadata — identity, author, timestamps, publish state, access — stays on the host's row, not least because a client-authored blob cannot be trusted with authorization |
| **Conversion** | Markdown → HTML is always available. **HTML → Markdown is offered only when the document is within the subset Markdown can represent**, and is refused with the blocking features named otherwise — Markdown cannot carry font family, size, colour, highlight, line height, underline, alignment or per-block `dir`, every one of which this component offers. Silently discarding an author's formatting on a switch is worse than declining the switch |
| **Security** | Coi's `allowedContent = true` made the editor a stored-XSS pipe. Sanitisation through `IComposerHtmlSanitizer` is mandatory and runs on load, on paste, on leaving the code editor and on raising content to the host; a host may swap the built-in allowlist for a hardened implementation. `IComposerSyntaxHighlighter` returns **tokens, not markup**, so a highlighter cannot introduce script. `qform`'s `<script src>` injection is replaced by a declarative host-resolved embed |
| **External JS** | This component carries the library's standing carve-out from the JS-free rule, granted by category rather than per library: an editor hosts capabilities — Markdown rendering, maths typesetting, syntax highlighting — that are solved problems nobody should reimplement. Each lives in its own `Forma.UI.Features.Composer.{Capability}` satellite behind a plugin contract, ships as static assets under `_content/…` with no bundler, stays in a supporting role, and degrades to absent. Markdig, KaTeX and TextMateSharp all qualify — TextMate because `.tmLanguage.json` needs an Oniguruma-compatible engine that `System.Text.RegularExpressions` is not. The editing surface, selection, undo stack and toolbar stay Forma's, in C# |

### 37. `FormaCloseButton` ✅

| | |
|---|---|
| **Old pattern** | The `×` dismiss control repeated across `admin/pops/*.aspx` modal headers and notification strips |
| **Used in** | Single source of truth for dismiss controls — `FormaModal`'s header, `FormaToastContainer`'s toasts, `FormaContextToolbar`'s clear-selection control |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: close, dismiss, clear selection), `EventCallback<MouseEventArgs> OnClick`, `bool Disabled` |
| **Blazor approach** | A thin wrapper over `<FormaButton Variant="Ghost" IconOnly>` supplying the one X glyph, so it owns no colours of its own and inherits the ghost variant's treatment and the icon-only square sizing. `Label` is required rather than defaulted because a wrong accessible name is worse than an obvious compile error, and the three call sites genuinely differ. Ships no `.razor.css` — every visual decision belongs to `FormaButton` |
| **Notes** | Before this existed the three call sites had each reimplemented `.forma-button-ghost` verbatim and drifted: two drew a 14×14 SVG while the toast used the `&times;` entity, and the focus ring was `--focus-color` against `FormaButton`'s `--main-color`. Retinting for a saturated surface (the toast) goes through `--forma-button-ghost-*-color` knobs rather than a fork |

### 40. `FormaEyeButton` ✅

| | |
|---|---|
| **Old pattern** | `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` beside a password field — a clickable span, not a button |
| **Used in** | Single source of truth for reveal controls — `FormaPasswordBox`'s toggle, and any later show/hide or preview affordance |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: show password, hide value, preview), `bool Revealed`, `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A thin wrapper over `<FormaButton Variant="Ghost" IconOnly>` supplying one eye glyph whose strike-through path is the only thing that changes, so the two states cannot drift apart. Ships no `.razor.css` — every visual decision belongs to `FormaButton`. `Revealed` and `Label` are both caller-owned: the component never assumes it is guarding a password |
| **Notes** | `FormaPasswordBox` had reimplemented the ghost variant by hand — its own muted colour, hover pair, focus ring and coarse-pointer square — which is the same drift that produced `FormaCloseButton`. Because the button element now belongs to `FormaButton`, CSS isolation puts it out of reach of the field's own stylesheet, so the field positions a wrapper span instead of the control |

### 41. `FormaDeleteButton`

| | |
|---|---|
| **Glyph** | A **trash can**. Not the cross — a cross reads as *dismiss this from view*, and borrowing it for an irreversible action is how a user deletes something they meant to close |
| **Old pattern** | The delete icon in each list page's action column, guarded by `AreYouSure(this, 'message')` before the postback |
| **Used in** | Row actions on every list page, and the destructive action in an editor's footer |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | The same thin wrapper over `FormaButton` as `FormaCloseButton` and `FormaEyeButton`, supplying the trash glyph and no CSS of its own. Confirmation stays the caller's job through `IConfirmDialogService` — a button that confirms on its own cannot be composed into a bulk action that confirms once. **To settle when built:** whether it pins `Danger`, which is right for a lone destructive button but loud repeated down every grid row, or stays `Ghost` and leaves emphasis to the confirm step |

### 42. `FormaUpdateButton`

| | |
|---|---|
| **Glyph** | A **check mark** — the commit half of an inline edit, opposite the cross that discards it |
| **Old pattern** | The save button ending an edit postback |
| **Used in** | `FormaDataGrid`'s inline editing beside a cancel control, and editor footers |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the check glyph. Pairs with the cross button in a row's edit state, which is what `FormaDataGrid`'s `SaveEditAsync`/`CancelEditAsync` already expect a caller to wire up from a `TemplateColumn` |

### 43. `FormaEditButton`

| | |
|---|---|
| **Glyph** | A **pencil** |
| **Old pattern** | The edit icon in each action column, linking through to the full editor page |
| **Used in** | Row actions on every list page; starts `FormaDataGrid`'s inline edit where the row is edited in place |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the pencil glyph, and nothing else — whether the click navigates or calls `BeginEdit` is the caller's decision |

### 44. `FormaAddButton`

| | |
|---|---|
| **Glyph** | A **plus** |
| **Old pattern** | The "new" button in each page's toolbar, opening the new-item modal |
| **Used in** | `FormaToolbar` actions, `FormaMultiSelect`'s add-new affordance, and any editor that appends a row |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, `RenderFragment? ChildContent` |
| **Blazor approach** | A wrapper over `FormaButton` supplying the plus glyph. The one of the set that is **not** always icon-only: a primary "add" is usually labelled in place ("New article"), so it takes optional `ChildContent` and drops `IconOnly` when given it, where the others stay square |

### 45. `FormaOverflowMenu`

| | |
|---|---|
| **Glyph** | An **ellipsis** — `⋯` horizontal in a toolbar, `⋮` vertical in a grid row, where a column is narrow and a row is tall |
| **Old pattern** | `Info('divId', '#buttonId', 's')` opening a `dropdown-menu` from a row's options button |
| **Used in** | Row actions once a row carries more than two, which is the threshold `FormaDropDownMenu` already names |
| **Parameters** | `RenderFragment ChildContent` (the same `FormaDropDownMenuItem`/`Divider` children), `MenuOrientation Orientation`, `string Label` (required) |
| **Blazor approach** | A preset over `FormaDropDownMenu` (#24) supplying the ellipsis trigger and the accessible name, owning no popup, positioning or keyboard logic of its own. It exists so every row's overflow affordance is the same glyph and the same target size, not to add behaviour |

### 46. `FormaContextMenu`

| | |
|---|---|
| **Old pattern** | None — Coi had no right-click menu; this is new capability rather than parity |
| **Used in** | Grid rows and media tiles, as a shortcut to the actions the overflow menu already lists |
| **Parameters** | `RenderFragment ChildContent` (menu items), `RenderFragment Target` (what right-clicking opens it), `bool Disabled` |
| **Blazor approach** | Suppresses the native menu on `@oncontextmenu` and opens `FormaDropDownMenu`'s panel at the pointer through the shared `AnchoredPopup.js`, so the same clipping, flipping and viewport clamping apply. Must answer the context-menu key and `Shift+F10` as well as the pointer, and a touch user has no right-click at all — so it stays a **shortcut** to actions reachable elsewhere, never the only route to one |

### 47. `FormaDuplicateButton`

| | |
|---|---|
| **Glyph** | **Two overlapping sheets** — the copy glyph. Deliberately not a second pencil, so "make another one" can never be misread as "change this one" |
| **Old pattern** | None in Coi's action columns |
| **Used in** | Row actions on list pages — Épica names duplicate in the same breath as edit, publish, delete and view |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | The same thin wrapper over `FormaButton` as the rest of the set. What duplication means — deep copy, draft status, title suffix — belongs entirely to the caller |

### 48. `FormaRefreshButton`

| | |
|---|---|
| **Glyph** | A **circular arrow** |
| **Old pattern** | None — Coi reloaded the whole page |
| **Used in** | `FormaDataGrid`'s `RefreshDataAsync()`, which is public API today with no affordance of its own, and any list fed by a provider that can go stale |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A wrapper over `FormaButton` that forwards `Busy`, so a slow refresh spins and announces `aria-busy` rather than looking dead while it waits. The only one of the set where `Busy` is the point rather than an afterthought |

### 49. `FormaSortMenu`

| | |
|---|---|
| **Glyph** | **Two opposed arrows** — `↑↓` |
| **Old pattern** | Coi sorted by clicking a column header, and had no standalone sort control. Header sorting is **not** what this replaces — `FormaDataGrid` already ships it by passing QuickGrid's `Sortable`, `SortBy`, `InitialSortDirection` and `GridSort<T>` straight through |
| **Used in** | Layouts with no header row to click — the tile grids in `FormaExplorer` (#53) and `FormaFileManager` (#54), and any card list |
| **Parameters** | `RenderFragment ChildContent` (the sort options as menu items), `string Label` (required) |
| **Blazor approach** | A preset over `FormaDropDownMenu` like `FormaOverflowMenu`, because sorting offers a choice rather than performing one action — which is why it is a `Menu` and not a `Button`, and why it sits with the menus rather than in `Buttons/`. A grid that already has sortable column headers should keep using them — two controls over one sort state is a synchronisation problem with no upside |

### 50. `FormaViewButton`

| | |
|---|---|
| **Glyph** | **Layout lines** — stacked rows for a list, a square of cells for a tile grid |
| **Old pattern** | None — Coi's image library had one fixed thumbnail grid |
| **Used in** | `FormaExplorer` (#53) and `FormaFileManager` (#54), switching between a dense list and thumbnails |
| **Parameters** | `ViewMode Mode`, `EventCallback<ViewMode> ModeChanged`, `string Label` (required), `ButtonSize Size`, `bool Disabled` |
| **Blazor approach** | Stateful like `FormaEyeButton`, drawing the mode it will switch **to** and naming that mode in the label, since an icon-only toggle that draws its current state cannot say which it is. **To settle when built:** whether a two-option segmented control showing both modes at once beats a toggle, which is guesswork until the managers exist |

### 51. `FormaShareButton`

| | |
|---|---|
| **Glyph** | **Three connected nodes**. Deliberately *not* the arrow leaving a box, which is the external-link glyph — Épica already lists "view" as a row action meaning *open on the public site*, and two arrows-out-of-a-box in one row would be indistinguishable |
| **Old pattern** | None |
| **Used in** | Copying or sending an item's public link, from its editor or its row |
| **Parameters** | `string Label` (required), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A thin wrapper over `FormaButton` that raises `OnClick` and nothing more. It deliberately does **not** own `navigator.share()`: that API needs transient activation and a secure context, is absent on desktop Firefox, and therefore needs a copy-to-clipboard fallback — which is a product decision about what "share" means here, not something a glyph should choose for the host |

### 52. `FormaPublishButton`

| | |
|---|---|
| **Glyph** | A **globe**, struck through when the action is to unpublish — reusing the strike-through convention `FormaEyeButton` established, so "make this public" and "take it down" read as one control in two states rather than two unrelated icons |
| **Old pattern** | Coi's per-item active/publish toggle |
| **Used in** | Editor footers and row actions — Épica names publish beside edit, duplicate, delete and view, and `FormaStatusBadge` already renders the draft/scheduled/published state it changes |
| **Parameters** | `bool Published`, `string Label` (required), `RenderFragment? ChildContent`, `ButtonSize Size`, `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | Stateful like `FormaEyeButton` and usually labelled like `FormaAddButton` — a primary "Publish" in an editor footer is text, the same action in a dense row is the glyph alone. **Scheduling stays out of it:** draft/scheduled/published is three states and a button offers two, so "Schedule…" belongs in a menu beside it rather than turning this control into a date picker |

### 53. `FormaExplorer` ✅

| | |
|---|---|
| **Old pattern** | The image half of `pops/images.aspx` — a grid of thumbnails with folders, multi-select, upload, move, delete and a capacity indicator |
| **Used in** | The media library as a surface in its own right, and as the "choose an existing image or video" dialog for article covers, OG images, product images and `FormaComposer` (#36) |
| **Parameters** | `IReadOnlyList<ResourceInfo>? Resources` or `IExplorerResourcesProvider? ResourcesProvider` (exactly one), `IReadOnlyList<FolderResourceInfo>? Folders`, `FolderResourceInfo? CurrentFolder`, `ExplorerViewMode ViewMode`, `ExplorerSortField SortField`, `ExplorerSortDirection SortDirection`, `int MaxSelection = 0` (0 = unlimited), `IReadOnlyList<ResourceInfo> Selected`, `int CurrentPage`, `int PageSize`, `int? TotalCount`, `string? SearchPlaceholder`, `FormaExplorerOptions Options`, `FormaExplorerFileOptions FileOptions`, `IFileStore? Store`, plus `EventCallback`s for selection, sort, view mode, paging, upload, rename, delete, drag-drop, refresh and every folder operation |
| **Blazor approach** | A manager for media **already on the server**, in the shape of a resource explorer: a resizable folder-tree sidebar, an address bar with back/forward/up and a breadcrumb menu, thumbnail/details/list views, drag-and-drop between folders, and enlargement through `FormaFileViewer` (#56) rather than a viewer of its own. New media arrives through `FormaFilePicker` (#34) plus `FormaFileUploader` (#35) in its toolbar, so it neither opens an OS dialog nor moves bytes itself. It owns no storage — every mutation is reported as intent through a callback. Configuration splits in two: `FormaExplorerFileOptions` gates every mutating capability per-caller (`AllowDelete`, `AllowRename`, `AllowUpload`, the three `AllowFolder*` flags), while `FormaExplorerOptions` covers presentation — dialog and resizable modes, sidebar and breadcrumb-menu visibility, the optional details columns, and whether the collection is paged or scrolls infinitely. The media scrolls inside its own panel so the toolbar, folder tree and pager stay put; an `ExplorerContext` cascades selection, sort, and per-item actions to the view components so adding an action or a column is a one-file change |
| **Data** | Two ways in, exactly one of which must be supplied, following `FormaDataGrid`'s `Items`/`ItemsProvider` split. `Resources` hands over a collection the explorer holds and filters, sorts and pages itself — right for a few hundred items. `ResourcesProvider` answers one `ExplorerResourceQuery` at a time and the explorer holds only the window it is showing, so a collection far too large to enumerate stays browsable. **The query carries only what the explorer owns** — the window, its search box and its sort — because the folder is the host's own `CurrentFolder` parameter and the implementation already has it; the same closure supplies any other filter the explorer never needs to learn about. `ExplorerResourcePage.TotalCount` is optional, so a store that cannot count cheaply returns null and the explorer drops its pager rather than demanding a number. Typing is debounced (`Options.SearchDebounce`), a superseded query is cancelled rather than merely ignored, and a mutation re-reads the window |
| **Initial release** | Browse, select, enlarge, upload, rename, delete, folder create/rename/delete/drag-move, sort by name/size/kind/date added/date modified, and either paginate or scroll infinitely. Sharing, tagging, search beyond the name, and bulk edit come later |
| **Notes** | The distinction from `FormaFileManager` (#54) is subject, not mechanism: this one is *media*, so it leads with thumbnails and previews, where the file manager is type-agnostic and leads with names, sizes and dates. Both browse the server; neither picks from the device. **Built standalone rather than composing Phase 17's `FormaSortMenu`/`FormaViewButton`** — the sort control is a native `<select>` and the view switcher three plain toggle buttons, because neither primitive existed yet and the explorer's own shape turned out simple enough not to need them. The Phase 18 plan to extract a shared browsing half with `FormaFileManager` also did not happen: `FormaFileManager` remains unbuilt, so `FormaExplorer` shipped alone rather than block on a surface with no consumer yet |

### 54. `FormaFileManager`

| | |
|---|---|
| **Old pattern** | `pops/images.aspx` again — its *management* half: create, rename and delete folders, move files between them, delete in bulk, watch the capacity indicator |
| **Used in** | The document library and any administrative view over stored files that is not specifically media |
| **Parameters** | `IReadOnlyList<ResourceInfo> Resources`, folder navigation callbacks, `EventCallback<ResourceInfo> OnRename`, `EventCallback<IReadOnlyList<ResourceInfo>> OnDelete`, `EventCallback<FolderMove> OnMove`, `EventCallback OnFolderCreate`, `IFileStore? Store` |
| **Blazor approach** | A general file manager over what is **already on the server**, in the shape of Explorer or Finder: a breadcrumb, a folder tree, a list or tile view of resources with name, size and date, selection, and `FormaPagination`. Type-agnostic, so it previews whatever a resource turns out to be through `FormaFileViewer` (#56). New files arrive through `FormaFilePicker` (#34) plus `FormaFileUploader` (#35) in its toolbar. It owns none of the storage, reporting every mutation as intent through a callback |
| **Initial release** | Browse, navigate folders, select, preview, upload, rename, delete, create folder. Move, bulk operations and search come later |
| **Notes** | It shares its browsing half with `FormaExplorer` (#53) — breadcrumb, resource grid, selection, pagination and the picker-plus-uploader toolbar. `FormaExplorer`'s `IExplorerResourcesProvider` is the shape to copy when this is built, but it deliberately lives in the explorer rather than in `Forma.Framework.Resources.Abstractions`: a browsing contract was moved down there once and immediately leaked folders into an assembly that models resources alone. **To settle when built:** whether a shared contract is extracted once there are genuinely two consumers, or the two surfaces keep their own |

### 55. `FormaVideoPlayer` ✅

| | |
|---|---|
| **Old pattern** | CKEditor's **YouTube embed** toolbar button (#36) and the raw `<iframe>` markup it pasted into the body |
| **Used in** | The most-demanded component on this list — Épica wants it on four separate surfaces: YouTube-by-URL embeds in articles (6.5), the auto-embedded live broadcast player (9.2), the Videos module's listings and featured video (P10), and the home page's featured video block (11.4). `FormaFileViewer` (#56) delegates its video branch here, and `FormaComposer` inserts it |
| **Parameters** | `string Source` (required), `string Title` (required — the player's accessible name), `bool Autoplay`, `bool DeferLoading` (default `true`), `string? Placeholder`, `IReadOnlyList<VideoCaptionTrack>? Captions`, plus captured pass-through attributes |
| **Blazor approach** | Resolve the address through an ordered chain of `IPlayerProvider`s (`IPlayerProvider`, `PlayerOptions` and `PlayerFrame` live in `Forma.UI.Features`), so a watch link, a share link and an embed link all work without the caller naming the service. Providers the host registered are tried first, so an application can teach the player an address shape this library never knew — or take over one it would otherwise claim — the same way a host extends `FormaFileViewer`; the eight built-ins follow, with `FilePlayerProvider` last because it accepts any fetchable address. YouTube, Vimeo and Twitch return an embed address the player loads in an `<iframe>`; TikTok, Instagram, Facebook and X publish markup plus a script instead, which a colocated JS module loads once and asks to scan, each service sizing its own player; any other fetchable address falls back to the browser's own `<video>`, with optional `<track>` captions. Size with the `aspect-ratio` property rather than a padding hack. **External JS is confined to the four services whose player *is* a script** — they publish no embed address, and the iframe providers need none |
| **Privacy and weight** | Nothing is fetched from a hosting service until a reader presses play: `DeferLoading` holds both the embed address and the service script behind a play control, which `Autoplay` overrides and a self-hosted file never needs. Embed from `youtube-nocookie.com`, and ask for do-not-track wherever a service offers it — `dnt=1` on Vimeo, `data-dnt="true"` on X. A third-party frame that loads with every article page is both a tracking surface a public news site has to account for and a large fixed cost on a mobile connection. **The still is the host's to supply:** `Placeholder` takes an image the host serves from its own origin, and stands in for a self-hosted file too. Deriving one from the service instead — YouTube publishes a thumbnail per video id — would send the service the very request the play control exists to withhold, so the component never does it |
| **Notes** | `Title` is required because an unnamed `<iframe>` is an unlabelled stop in the tab order. **Autoplay is not what Épica's 9.2 asks for** — "auto-embed live player" means the player appears without an editor placing it, not that it starts playing. Autoplay is forced muted through the JS property, because the `muted` attribute no longer reaches the element once Blazor has created it, and WCAG 1.4.2 requires a control over anything that plays by itself. A service script that never runs — blocked, or simply slow — leaves a link to the video in the player's place |

### 56. `FormaFileViewer` ✅

| | |
|---|---|
| **Old pattern** | The preview pane of `pops/images.aspx` — clicking a library entry to actually look at it |
| **Used in** | The promise #35 already makes and does not own: showing whatever a chosen resource turns out to be. That belongs here, so the picker, the manager (#54) and the explorer's enlarge (#53) show one preview implementation rather than three |
| **Parameters** | `string Source` (required), `IReadOnlyList<string>? Siblings`, `EventCallback<string> OnNavigate`, `bool ShowMetadata`, `RenderFragment<string>? UnsupportedTemplate`. The caller passes an address, not a resolved resource — a component handed a `ResourceInfo` would need every caller to resolve one first, which is the provider's job. `ResourceInfo` (`Source`, `Name`, `Size`, `CreatedAt`, `ModifiedAt`) and `IResourceProvider` live in `Forma.Framework.Resources.Abstractions`, which carries no ASP.NET Core reference — so a converting package stays a plain class library. Sizes and durations are written for a reader by `ResourceFormatter`, which a view calls when it displays one rather than the model carrying a preformatted string. What a resource **is** is carried by its type rather than by an enum: `FolderResourceInfo` and `FileResourceInfo` split container from content, and `ImageResourceInfo`, `VideoResourceInfo`, `AudioResourceInfo`, `DocumentResourceInfo` and `ArchiveResourceInfo` refine the file branch, each owning the detail it alone has. `IVisualResource` and `ITimedResource` cross the branch for consumers that care only about pixels or running time. Named for resources rather than media because the viewer renders documents too |
| **Blazor approach** | An ordered chain of `IResourceProvider`s resolves the address into a `ResourceInfo`, mirroring `FormaVideoPlayer`'s provider chain: the first to claim it decides what the resource is, and the viewer owns the markup for every kind, so a converting package never ships Razor or CSS. Resolution runs once per address rather than once per render. Providers the host registered are tried first; `BrowserResourceProvider` comes last and claims only what a browser renders unaided, mapping the media type onto the matching resource type, so **an address no provider claims degrades to a type glyph, the name, the size when one is known, and a download link** rather than a broken embed. A `MarkupResourceInfo` is rendered as trusted markup, so a converting provider must sanitise its output. A `DocumentResourceInfo` whose content type is `text/*` is framed with `sandbox=""` — an opaque origin, withholding scripts, forms and navigation — because uploaded HTML is untrusted; anything else goes to `<object>` with a new-tab link as fallback content. With `Siblings` supplied the viewer steps through the set, resolving the horizontal arrow keys through `IsRightToLeft`; the host modal keeps `Escape` |
| **Converting packages** | `Forma.Framework.Resources.Markdown` (Markdig) and `Forma.Framework.Resources.Office` (OfficeIMO) ship separately and return `MarkupResourceInfo`, so neither Markdig nor OfficeIMO lands in the package every consumer downloads. Each reads its bytes through an `IResourceContentReader` the host implements, so a converting package never assumes where a file lives, and sanitises its output with `HtmlSanitizer` before returning it. A host registers them like any other service — `AddScoped<IResourceProvider, …>()` — and without them a `DocumentResourceInfo` still works for whatever the browser handles natively |
| **Zoom and pan** | Settled: the frame is a plain scrollable region (`overflow: auto`) rather than a transform-driven canvas. Touch and pen pan it natively, and `touch-action: pan-x pan-y pinch-zoom` hands pinch-to-zoom to the platform itself — the exact case CSS was expected to handle worst turned out not to need JS at all. The one gap native scrolling doesn't cover is a mouse click-drag, so a small colocated `.razor.js` adds that, gated to `pointerType === 'mouse'` so it never fights the browser's own touch scrolling. Zoom level moves in five fixed steps (100–400%) through on-screen +/− buttons, with no wheel or double-click zoom, since the scroll wheel already has a native job on this element and hijacking it would fight the reader's own scrolling |
| **Notes** | **Viewing only — crop, rotate and resize are editing** and belong to `FormaImageEditor` (#57), even though Coi's `jcrop` widget sat on this same preview. A viewer that quietly mutates the file is a different component with a different contract, and folding it in here is how this one would stop being safe to open on anything |

### 57. `FormaImageEditor`

| | |
|---|---|
| **Old pattern** | Two surfaces, not one. `jcrop` in `slider.aspx` did a **constrained crop-to-fit**: aspect ratio and minimum size both pinned to the destination slot, the image scaled into a fixed 590×350 box, an initial selection set for the editor, and `tellSelect()` posting `x/y/w/h` plus the target dimensions to `crop.asmx`. Separately, `pops/pixie.aspx` hosted the commercial **Pixie** editor in an `<iframe>`, launched per-entry from the image library |
| **Used in** | Making an arbitrary upload fit a fixed slot — article covers and OG images, video thumbnails, sponsor logos, gallery tiles. Launched from `FormaFileViewer` (#56), the manager (#54) and the picker (#35), the same way Coi launched it from its library |
| **Parameters** | `string Source` (required), `double? AspectRatio`, `IReadOnlyList<AspectRatioPreset>? Presets`, `int? MinWidth`, `int? MinHeight`, `int? OutputWidth`, `int? OutputHeight`, `EventCallback<ImageEdit> OnApply`, `EventCallback OnCancel` |
| **Scope** | **In:** crop with free or locked ratio and named presets, rotate in 90° steps, flip horizontal and vertical, resize to target dimensions, and zoom for precision. **Out:** filters, text, stickers, drawing, shapes, gradients and layers — that is Pixie's tier, it is a canvas compositing app rather than a control, and reaching it by pulling in a library would spend the one external-JS exception this repo has deliberately not spent twice |
| **Blazor approach** | Follow Coi's actual division of labour, which already matches this library's boundary: **the component describes the edit, the host performs it.** `OnApply` emits an `ImageEdit` — crop rectangle, rotation, flips, target size — and the host renders it with ImageSharp or SkiaSharp, exactly as `FormaFileUploader` leaves the destination to an `IFileStore` and the managers leave every mutation to a callback. A colocated `.razor.js` drives the handles and the live preview through Pointer Events and Canvas 2D; that is hand-written interop for behaviour only script can provide, not an external library |
| **Why not export the canvas** | Returning the finished bitmap from the browser looks simpler and is the wrong trade under Interactive Server: a base64 canvas export of a multi-megabyte photo crosses the SignalR circuit, where the default 32 KB receive limit rejects it outright, and re-encoding through the browser discards EXIF, ICC colour profiles and the original's quality. A rectangle is a few numbers |
| **The two bugs to design out** | **Coordinate space** — report the rectangle in the image's *natural* pixels, never the displayed CSS pixels, because the editor scales the image to fit its box just as Coi's 590×350 did, and a crop that forgets the scale factor is wrong by exactly that ratio. **EXIF orientation** — a phone photo displays upright through `image-orientation` while its stored pixels are rotated, so coordinates taken from the display do not address the bytes the host will cut |
| **Notes** | Neither jcrop nor Pixie is reachable without a mouse, and this repo does not ship that: the selection needs arrow-key nudging with a modifier to resize, numeric width/height fields as an exact path, and a live region announcing the rectangle as it changes. The handles are also the hardest touch target in the library — 44×44 under a coarse pointer around a rectangle whose corners may sit at the image edge, needing press-and-hold to start a drag so an ordinary swipe still scrolls. **To settle when built:** whether straightening by an arbitrary angle joins the 90° steps, since it forces canvas resampling and a bounds recalculation that quarter-turns avoid entirely |

---

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
| **CKEditor** | Now in scope as `FormaComposer` (#36) rather than excluded — Forma builds its own editor over `contenteditable` with its own controls, taking Coi's feature set as the floor. External libraries appear only as isolated capability satellites, never as the editor itself |

---

## Suggested Build Order

Components are ordered by dependency and reuse frequency:

| Phase | Components | Rationale |
|---|---|---|
| **Phase 1** | `FormaModal` ✅, `FormaConfirmDialog` ✅, `FormaToast` ✅ | Infrastructure — everything depends on modals and notifications |
| **Phase 2** | `FormaPageHeader` ✅, `FormaToolbar` ✅, `FormaTabs` ✅ | Layout — the shell of a page |
| **Phase 3** | `FormaInput` ✅, `FormaToggleSwitch` ✅, `FormaPasswordBox` ✅, `FormaCurrencyInput`, `FormaSlider` ✅, `FormaDatePicker` ✅, `FormaSelect` ✅ | Form inputs — needed before building any edit forms |
| **Phase 4** | `FormaDataGrid<TItem>` ✅, `FormaContextToolbar` ✅, `FormaStatusBadge` ✅, `FormaCapacityIndicator`, `FormaPagination` ✅ | Data display — needed for list pages |
| **Phase 5** | `FormaSortableList<TItem>` ✅, `FormaMultiSelect`  ✅, `FormaCategorySelector`, `FormaDropDownMenu` ✅, `FormaColorPicker` ✅ | Interactive widgets — needed for editor pages |
| **Phase 6** | `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built |
| **Phase 7** | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors — built alongside their specific pages |
| **Phase 8** | `FormaButton` ✅ | Shared button primitive — built after the fact and retrofitted onto `FormaDataGrid`, `FormaModal`, `FormaConfirmDialog`, `FormaPagination`, `FormaDatePicker` as the single source of truth for buttons |
| **Phase 9** | `FormaCheckBox` ✅ | Shared checkbox primitive — built after the fact and retrofitted onto `FormaDataGrid` and `FormaMultiSelect` as the single source of truth for checkboxes |
| **Phase 10** | `FormaCloseButton` ✅ | Shared dismiss primitive — built after the fact and retrofitted onto `FormaModal`, `FormaToastContainer` and `FormaContextToolbar` as the single source of truth for the `×` control |
| **Phase 11** | `FormaField` ✅, `FormaTextArea` ✅ | Shared field chrome — extracted from `FormaInput` so the floating label, focus ring and validation styling live once, then `FormaTextArea` built on it. `FormaPasswordBox` ✅ composes it; `FormaCurrencyInput` will too rather than restating it |
| **Phase 12** | `FormaEyeButton` ✅ | Shared reveal primitive — built after the fact and retrofitted onto `FormaPasswordBox`, which had hand-rolled the ghost icon button it now uses |
| **Phase 13** | `FormaDeleteButton`, `FormaUpdateButton`, `FormaEditButton`, `FormaAddButton`, `FormaDuplicateButton`, `FormaRefreshButton`, `FormaShareButton`, `FormaPublishButton` | The rest of the icon-button vocabulary — the same thin wrapper over `FormaButton`, one glyph each, so a row's actions stop being ad-hoc markup per page |
| **Phase 14** | `FormaOverflowMenu`, `FormaContextMenu` | Menu presets over `FormaDropDownMenu` — the ellipsis trigger first, then the same panel opened by right-click, once there is a menu worth opening that way |
| **Phase 15** | `FormaVideoPlayer` ✅, `FormaFileViewer` ✅ | Media rendering — the player first because it stands alone and four Épica surfaces want it, then the viewer that delegates its video branch to it. Both come before the picker chain, which previews through the viewer |
| **Phase 16** | `FormaFilePicker` ✅, `FormaFileUploader` ✅, `FormaImagePicker` ✅, `FormaMediaPicker` | The intake chain, in that order: the device picker, then the uploader that moves what it picked, then the two type-restricted skins over the picker |
| **Phase 17** | `FormaSortMenu`, `FormaViewButton` | The two controls that exist for the managers' tile grids — a layout with no column headers to sort by and two layouts to switch between. Neither has a surface to serve until Phase 18 exists |
| **Phase 18** | `FormaExplorer` ✅, `FormaFileManager` | The server-side browsing surfaces — planned to compose the picker, uploader and viewer with a shared browsing half extracted between them. `FormaExplorer` shipped standalone instead, since `FormaFileManager` has no surface yet to justify the extraction; revisit sharing if and when `FormaFileManager` is built |
| **Phase 19** | `FormaComposer` | The largest component, and last of the composing ones — an editor engine rather than a single control, with every capability and every editing surface arriving as a plugin. It replaces both halves of Coi's authoring stack: the CKEditor rich text field *and* the `template.aspx` page designer. Its chrome is `FormaSelect`, `FormaDropDownMenu` and `FormaColorPicker`, it delegates image insertion to `FormaExplorer`, uploads through `FormaFilePicker` and `FormaFileUploader`, and inserts `FormaVideoPlayer`. It runs to eight phases of its own — see [docs/composer-design.md](docs/composer-design.md) |
| **Phase 20** | `FormaImageEditor` | The heaviest media component and the last — it needs the surfaces above to launch it, and its pointer handles, coordinate mapping and keyboard path are worth building once the rest of the media flow is settled |
