# Component Inventory

> Identified from the old Coi admin codebase, which is where the component set was derived
> from — the library itself is general-purpose. Each component maps an old Web Forms + jQuery
> pattern to a Blazor component using **only** built-in Blazor components, native HTML elements,
> and browser APIs — no external JS libraries.
>
> Target location: `Forma.Blazor/Components/`
>
> **Naming convention:** All components use the `Forma` prefix for their tag names (e.g., `<FormaDialog>`,
> `<FormaTabs>`) to avoid clashes with third-party libraries. CSS classes use the `forma-` prefix.
> C# types (enums, services) rely on namespaces for disambiguation and don't carry the prefix.

---

## File Structure

```
Forma.Blazor/Components/
├── ✅ FormaField.razor / .razor.cs / .razor.css
├── ✅ FormaVisibility.razor / .razor.cs / .razor.css  # collapses content to nothing without unmounting it
├── FieldLabelMode.cs                     # enum: Floating, Pinned, Above, Inline
├── (2 / 2) Symbols/
│   ├── ✅ FormaIcon.razor / .razor.cs / .razor.css   # draws an Icon — a glyph, a brand mark or a character — as inline SVG or symbol-font text
│   └── ✅ FormaEmoji.razor / .razor.cs / .razor.css  # draws a Forma.Framework.Emojis.Emoji from a host's provider
├── (6 / 6) Bars/
│   ├── ✅ FormaCommandArea.razor / .razor.cs / .razor.css
│   ├── ✅ FormaCommandBar.razor / .razor.cs / .razor.css
│   ├── ✅ FormaSearchToolbar.razor / .razor.cs / .razor.css
│   ├── ✅ FormaToolbar.razor / .razor.cs / .razor.css
│   ├── FormaToolbarItem.cs                    # abstract base: Id, Title, Tooltip?, Icon?, IsDisabled
│   ├── FormaToolbarGroup.cs                   # item holding items, drawn as a labelled cluster
│   ├── FormaToolbarAction.cs                  # abstract: adds Command and shortcuts
│   ├── FormaToolbarCommand.cs                 # action firing once
│   ├── FormaToolbarToggle.cs                  # action carrying IsActive
│   ├── FormaToolbarMenu.cs                    # item offering Options
│   ├── FormaToolbarReadout.cs                 # non-interactive Value
│   ├── FormaToolbarCustomItem.cs              # caller-supplied Component and Parameters
│   ├── FormaToolbarOption.cs
│   ├── ✅ FormaSelectionToolbar.razor / .razor.cs / .razor.css
│   └── ✅ FormaStatusStrip.razor / .razor.cs / .razor.css
├── (2 / 2) Layout/
│   ├── ✅ FormaSplitter.razor / .razor.cs / .razor.css / .razor.js
│   ├── SplitterOrientation.cs                 # enum: Vertical, Horizontal
│   ├── ✅ FormaTabs.razor / .razor.cs
│   └── ✅ FormaTab.razor / .razor.cs
├── (2 / 2) Page/
│   ├── ✅ FormaPageTitle.razor / .razor.cs
│   └── ✅ FormaPageHead.razor / .razor.cs
├── (5 / 6) Feedback/
│   ├── ✅ FormaDialog.razor / .razor.cs
│   ├── ✅ FormaDialog.razor.css
│   ├── DialogSize.cs
│   ├── ✅ FormaConfirmDialog.razor / .razor.cs
│   ├── ✅ FormaConfirmDialog.razor.css
│   ├── ✅ FormaToastContainer.razor / .razor.cs
│   ├── ✅ FormaToastContainer.razor.css
│   ├── ✅ FormaSpinner.razor / .razor.cs / .razor.css
│   ├── SpinnerSize.cs
│   ├── ✅ FormaStatusBadge.razor
│   └── FormaCapacityIndicator.razor
├── (2 / 2) Collections/
│   ├── ✅ FormaDataGrid.razor
│   └── ✅ FormaSortableList.razor
├── (3 / 13) Buttons/
│   ├── ButtonSize.cs
│   ├── ButtonType.cs
│   ├── ButtonVariant.cs
│   ├── ButtonOptions.cs
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
├── (11 / 12) Inputs/
│   ├── SelectOption.cs
│   ├── PasswordStrength.cs
│   ├── TextInputType.cs                        # enum: Text, Email, Tel, Url
│   ├── TextInputMode.cs                        # enum: the virtual keyboard to raise
│   ├── TextEnterKeyHint.cs                     # enum: what the keyboard's action key does
│   ├── TextAutoCapitalize.cs                   # enum: what the keyboard capitalizes
│   ├── ✅ FormaCheckBox.razor
│   ├── ✅ FormaTextBox.razor
│   ├── ✅ FormaSearchBox.razor
│   ├── ✅ FormaTextArea.razor
│   ├── ✅ FormaToggleSwitch.razor
│   ├── ✅ FormaPasswordBox.razor
│   ├── FormaCurrencyInput.razor
│   ├── ✅ FormaSlider.razor
│   ├── ✅ FormaDatePicker.razor
│   ├── ✅ FormaSelect.razor
│   ├── ✅ FormaColorPicker.razor
│   └── ✅ FormaMultiSelect.razor
├── (2 / 3) Navigation/
│   ├── BreadcrumbItem.cs                # generic record: Label, Url?, Value?, IsCurrent, Children?
│   ├── ✅ FormaBreadcrumb.razor / .razor.cs / .razor.css
│   ├── FormaCategorySelector.razor
│   └── ✅ FormaPagination.razor
├── (3 / 4) Menus/
│   ├── ✅ FormaDropDownMenu.razor
│   ├── ✅ FormaDropDownMenuItem.razor
│   ├── ✅ FormaDropDownMenuDivider.razor
│   ├── ✅ FormaOverflowMenu.razor
│   ├── FormaContextMenu.razor
│   └── FormaSortMenu.razor
├── (4 / 6) Files/                        # vocabulary in Forma.Features.Files
│   ├── ✅ FormaFilePicker.razor / .razor.cs / .razor.css / .razor.js
│   ├── ✅ FormaFileUploader.razor / .razor.cs / .razor.css
│   ├── UploadFileStream.cs               # counts bytes read to drive per-file progress
│   ├── ✅ FormaImagePicker.razor
│   ├── FormaMediaPicker.razor
│   ├── FormaFileManager.razor
│   └── ✅ FormaFileViewer.razor          # dispatches to Media/ for the type it recognises
├── (1 / 1) Explorer/
│   ├── ✅ FormaExplorer.razor / .razor.cs / .razor.css / .razor.js / .he.json / .es.json
│   ├── FormaExplorerSidebar.razor        # folder tree, resizable through a sash
│   ├── FormaExplorerFolderNode.razor     # one branch of that tree
│   ├── FormaExplorerAddressBar.razor     # back/forward/up plus the breadcrumb menu
│   ├── FormaExplorerToolbar.razor        # selection, upload, view switch, sort
│   ├── FormaExplorerThumbnailsView.razor
│   ├── FormaExplorerDetailsView.razor
│   ├── FormaExplorerListView.razor
│   ├── FormaExplorerThumbnail.razor      # media depiction shared by all three views
│   ├── FormaExplorerActionBar.razor      # per-item operations as an overflow menu
│   └── ExplorerResourceExtensions.cs     # the labels a view puts on a resource
│                                         # everything else the explorer speaks in — the view and
│                                         # interaction pair, options, actions, selection, enums —
│                                         # lives in Forma.Features.Explorer
├── (1 / 4) Media/
│   ├── FormaImageEditor.razor / .razor.js
│   ├── ✅ FormaVideoPlayer.razor
│   ├── FormaDesignPicker.razor
│   └── FormaFontPicker.razor
├── (0 / 1) Composer/                         # see docs/composer-design.md
│   ├── FormaComposer.razor                   # engine shell, plugin discovery, editor switcher
│   ├── FormaStandardRichTextEditor.razor     # preset over the same engine
│   ├── FormaCompactRichTextEditor.razor      # preset over the same engine
│   ├── Dialogs/                              # shared, plain-data dialogs (link, table, video, media, embed)
│   ├── RichTextEditor/                       # RichTextEditorPlugin + feature plugins
│   ├── MarkdownEditor/                       # MarkdownEditorPlugin
│   ├── CodeEditor/                           # CodeEditorPlugin
│   └── PageDesigner/                         # PageDesignerPlugin + palette, canvas, widgets
└── (0 / 4) Editors/
    ├── FormaNotesEditor.razor
    ├── FormaPermissionMatrix.razor
    ├── FormaProductOptionsEditor.razor
    └── FormaFilterEditor.razor
```

---

## Tier 1 — Core Layout & Infrastructure

These components form the shell of a page. Build first.

### 1. `FormaPageTitle` ✅

| | |
|---|---|
| **Old pattern** | `<h1 class="page-title">` + `<ol class="breadcrumb">` + optional capacity label |
| **Used in** | Every admin page (pages, products, orders, leads, comments, users, coupons, settings, design) |
| **Parameters** | `string Title` (required, validated), `IReadOnlyList<BreadcrumbItem<object?>>? Breadcrumbs`, `RenderFragment? Actions`. The last crumb without a `Url` is marked as the current page |
| **Blazor approach** | Pure Razor markup, no interactivity needed (static SSR). Code-behind in `.razor.cs` |

### 2. `FormaSearchToolbar` ✅

| | |
|---|---|
| **Old pattern** | `<div class="card-body">` with search textbox + filter dropdowns + action buttons, wrapping content in a flex row |
| **Used in** | pages, products, orders, leads, comments, users, coupons, filters |
| **Parameters** | `EventCallback<string> OnSearch` (required, validated), `SearchBoxOptions Search` (required; its placeholder validated), `IReadOnlyList<FormaToolbarItem> Items`, `string? Label` (required once `Items` is non-empty), `EventCallback<string> OnCommand` |
| **Blazor approach** | Sealed component pairing `FormaSearchBox` with a `FormaToolbar` of list actions, rendered with `ShowLabels` so each action reads as a named button. `Search` is handed to the search box whole, which owns the debounce through `IDebounceable` |
| **Notes** | Renamed from `FormaToolbar` once that name was needed for the generic, item-driven `FormaToolbar` (#65), then rebuilt on it. The search field is not optional — a search toolbar with only actions is just a toolbar, so `OnSearch` is required and the guard says so; `Search.Placeholder` is required too because it is the field's only accessible name |

### 3. `FormaTabs` ✅

| | |
|---|---|
| **Old pattern** | jQuery Tools Tabs: `<ul class="tabs"><li>` + `<div class="panes"><div class="panc">`. Click tab → show corresponding pane |
| **Used in** | page editor (5 tabs), product editor (7 tabs), settings (5 tabs), store-settings (7 tabs), design (5 tabs) |
| **Parameters** | `RenderFragment ChildContent` containing `FormaTab` children, `string? ActiveTabId` and `EventCallback<string> ActiveTabIdChanged` for caller-controlled selection, `string? Label`. Each `FormaTab` has `string Title` (required, validated), `Icon? Icon` (drawn by `FormaIcon`), `string? TabId` (generated when omitted), `bool Trailing` (sits the header at the far end of the strip, with every trailing tab after it), `RenderFragment ChildContent` |
| **Blazor approach** | Interactive component. `FormaTabs` provides `CascadingValue` of itself; `FormaTab` children register/unregister via `CascadingParameter`. WAI-ARIA `tablist`/`tab`/`tabpanel` roles, `aria-selected`, `aria-controls`. Show/hide panes with CSS `::deep`. No JS needed. Code-behind in `.razor.cs` |

### 4. `FormaDialog` ✅

| | |
|---|---|
| **Old pattern** | `Modal('#divId')` / `CloseModal()` — shows a `<div>` overlay with content from hidden divs. Stacking z-index, Escape key to close |
| **Used in** | Every admin page — new page modal, help modals, notes modals, folder dialogs, invoice modals, options editor |
| **Parameters** | `bool Opened` (required), `EventCallback OnClose` (required), `string? Title`, `DialogSize Size` (Small/Medium/Large/ExtraLarge/Full, default Medium), `RenderFragment? Body`, `RenderFragment? Footer`, `string? ConfirmText`, `string? CancelText`, `EventCallback OnConfirm`, `EventCallback OnCancel`, `DialogOptions Options` (`CloseOnBackdropClick`, `ShowCancelAction`, `ShowConfirmAction`, `CloseOnCancel`, `CloseOnConfirm` — all default `true`) |
| **Blazor approach** | Uses native `<dialog>` element with minimal JS interop (`Modal.js`). Supports custom footer content, built-in localized footer actions, or both together. Uses logical CSS so footer/action alignment works in both RTL and LTR. Sealed `IAsyncDisposable` component with code-behind in `.razor.cs` |

### 5. `FormaConfirmDialog` ✅

| | |
|---|---|
| **Old pattern** | `AreYouSure(this, 'message')` — custom confirm that returns bool. Used on delete buttons, destructive actions |
| **Used in** | Every page with delete/remove actions (list pages bulk delete, individual row delete, design change, template swap) |
| **Parameters** | Component is service-driven (no public message/label parameters). Dialog requests are sent via `IConfirmDialogService.ConfirmAsync(string message, string confirmText, string cancelText)` — all arguments validated non-empty |
| **Blazor approach** | Built on `FormaDialog` with explicit confirm/cancel actions supplied by the confirm component (the dialog's default actions disabled for this case). Uses `IConfirmDialogService : INotifyStateChanged` beside its `ConfirmDialogService` in `Forma.Features.Feedback`, registered via DI, returning `Task<bool>`. Service composes `StateNotifier` for async subscriber notification. Sealed `IDisposable` component with code-behind in `.razor.cs` |

### 6. `FormaToast` ✅

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |
| **Parameters** | Service-driven (no public component parameters). Toast requests sent via `IToastService.ShowAsync(string message, ToastType type = Success, TimeSpan? duration = null)` (default 3s). `ToastType` enum: `Success`, `Error`, `Info`, `Warning`. `IToastService` also exposes `IReadOnlyList<ToastMessage> Toasts`, `Task RemoveAsync(string id)`. Arguments validated (message non-empty, duration non-negative, id non-empty). |
| **Blazor approach** | `IToastService : INotifyStateChanged` beside its `ToastService` in `Forma.Features.Feedback`, registered via DI. Service composes `StateNotifier` for async `Func<Task>` subscriber notification. A `FormaToastContainer` (sealed `IDisposable`, code-behind in `.razor.cs`) component at layout level subscribes and renders active toasts. Auto-dismissal via `System.Threading.Timer` per toast. CSS `@keyframes` slide-in animation, type-based color variants. No JS needed |

---

## Tier 2 — Data Display

### 7. `FormaDataGrid<TItem>` ✅

| | |
|---|---|
| **Old pattern** | `asp:GridView` with server-side paging, sortable columns, row click-through, checkbox column for mass select, action icon columns (view/edit/delete), status badges, responsive hide classes |
| **Used in** | pages, products, orders, leads, comments, users, coupons (all 7 main list pages) |
| **Parameters** | `IQueryable<TGridItem> Items` *or* `GridItemsProvider<TGridItem> ItemsProvider` (exactly one), `RenderFragment Columns`, `RenderFragment Status`, `bool EnableCheckedSelection`, `bool EnableRowSelection`, `CheckedSelectionOptions CheckedSelectionOptions` (`Minimum`, `Maximum`, `Caption`), `EventCallback<SelectionRejected> OnSelectionRejected`, `IReadOnlyList<TGridItem> SelectedItems` / `EventCallback<IReadOnlyList<TGridItem>> SelectedItemsChanged`, `EventCallback<TGridItem> OnRowClick`, `Func<TGridItem, string?>? RowClass`, `Func<TGridItem, object> ItemKey`, `string QueryParameterNamePrefix`, `bool EnablePagination` (default `true`), `PaginationOptions PaginationOptions`, `bool Virtualize`, `float ItemSize`, `int OverscanCount`, `int PageSize` (drives an internal `PaginationState`; required only while paginating). Public method: `RefreshDataAsync()`. Public read-only properties: `IsMaximumSelectionReached`, `IsMinimumSelectionSatisfied` |
| **Columns** | A plain `RenderFragment` — callers author columns with QuickGrid's own `PropertyColumn`/`TemplateColumn` directly (no custom column wrapper type); `FormaDataGrid` cascades its item type via `[CascadingTypeParameter]` so those tags infer their generic argument without the caller specifying it |
| **QuickGrid passthrough** | Every *column*-level feature works untouched (`Sortable`, `SortBy`, `Align`, `Format`, `HeaderTemplate`, `ColumnOptions`, `PlaceholderTemplate`, `InitialSortDirection`, `GridSort<T>`). Of the *grid*-level parameters, `Items`, `ItemsProvider`, `ItemKey`, `RowClass`, `OnRowClick`, `QueryParameterNamePrefix`, `Virtualize`, `ItemSize` and `OverscanCount` are forwarded; `Pagination` is owned internally and bound only while `EnablePagination` is on, so an unpaginated grid lets virtualization size its scroll range from the real row count instead of a page. Not forwarded: `Class`/`AdditionalAttributes` on the `<table>` (they land on the wrapping `<div>`), `Theme` (conflicts with the design tokens), and `AnchorMode` |
| **Notes** | The grid owns a footer holding the page controls at the inline start and caller-supplied `Status` content (`role="status"`) at the inline end; the footer is skipped entirely when there is no status and only one page, and the page controls specifically are hidden whenever `EnablePagination` is off or everything fits on one page — even if `Status` still has the footer showing. `QueryParameterNamePrefix` defaults to empty, matching QuickGrid. Two grids on one page that share a column title **must** be given distinct prefixes or they share `?sort=`/`?order=` and sort each other. `EnableCheckedSelection`/`EnableRowSelection` require `Items`: select-all and range selection name the whole set, which an `ItemsProvider` could only produce by fetching every page. `PaginationOptions` defaults to the numbered page links **plus** the page-number box, which is a wider default than a bare `FormaPagination` shows on its own. `EnableRowSelection` adds Explorer/Finder-style mouse (plain click replaces, Ctrl/Cmd toggles, Shift ranges, Alt is a no-op) and keyboard (Arrow/Home/End roving focus, same modifiers) selection independent of the checkbox column; the two flags can be combined or used alone. `CheckedSelectionOptions.Maximum` disables further selection once reached (rows visually muted, `OnSelectionRejected` fires on a blocked attempt) but a full deselect is always allowed; the master control (checkbox or the options' `Caption`) is disabled only when nothing is selected and selecting everything would exceed the maximum. `CheckedSelectionOptions.Minimum` blocks unchecking a row directly below the floor but does not block a plain-click/range-select replacement or the master "deselect all" — those are reset gestures, not incremental removals, so the floor only binds (and only mutes rows) while `EnableCheckedSelection` is on. `OnSelectionRejected` reports which `SelectionBoundary` blocked the attempt and its limit. The options' `Caption` swaps the header checkbox for `SelectAllText`/`DeselectAllText`, either as a static label (only one set) or an interactive toggle button (both set); `CheckedSelectionCaption.Hidden` renders nothing in the header |
| **Sizing** | The rows live in their own scroll region with the footer outside it, and the grid caps itself at `max-block-size: 100%`. That percentage resolves to `none` against an auto-height ancestor, so an ordinary page keeps the plain shrink-to-content layout; inside a height-bounded shell the cap engages, the rows scroll, the column headers stay stuck to the top of the scroll region and the footer stays pinned below it. **The grid takes no height parameter** — a fixed row count or pixel height is a guess about space the component can't see, which wastes room on a large viewport and forces nested scrollbars on a small one. `Virtualize` is meant for that bounded case with `EnablePagination="false"`; in an unbounded page it simply renders everything rather than misbehaving, and `ItemSize` should match the real row height or the scroll range is wrong |
| **Actions & editing** | Row actions are a caller-side pattern over `TemplateColumn` — the grid ships no action-button components, since in Coi those icons navigate to a full editor or raise a confirm dialog rather than editing in place, and a wrapper would be strictly less capable than the `TemplateColumn` it wraps. **Inline editing is built in.** `EnableInlineEditing` + `CloneForEdit` let one row at a time be edited: the grid copies the row, cascades a `RowEditSession` its columns read, validates the copy with a `DataAnnotationsValidator` over its own `EditContext`, and raises `OnRowSave` only once valid — so `CancelEditAsync()` discards and `SaveEditAsync()` commits. Public API: `BeginEdit(item)`, `SaveEditAsync()` (returns whether it was accepted), `CancelEditAsync()`, `IsEditing(item)`, `EditingItem`. Callers drive them from a `TemplateColumn` via `@ref` (null-conditional — the ref is unset on the grid's first render). The edited row drops its selection marker, which takes it out of every mouse and keyboard selection gesture for the duration, so no JS change was needed. Both the editor and the validator sit under a **fixed, keyed** `CascadingValue<EditContext>`: `InputBase` and `DataAnnotationsValidator` both refuse a context swap, so each edit gets its own pair rather than re-parameterising the last |
| **`FormaPropertyColumn<TGridItem, TProp>`** | A `ColumnBase<TGridItem>`-derived column — QuickGrid's column base is public and externally derivable, so this extends the passthrough rather than replacing it, and sits in the same `Columns` fragment as `PropertyColumn`/`TemplateColumn`. Shows `Property`, auto-titles from `[Display]`/`[DisplayName]`/the member name, auto-sorts (a caller-supplied `SortBy` wins), and formats via `Format` against `CurrentCulture`. While its row is edited it swaps to an editor chosen from `TProp` at compile time — `InputText` (string), `InputCheckbox` (bool), `InputDate<T>` (date/time types), `InputNumber<T>` (numeric); anything else, including enums, needs an `EditTemplate`. `Editable="false"` opts a column out, and a property with no setter is never editable. Validation targets the *draft*: the column rewrites `item => item.Member` into `() => draft.Member` for `ValueExpression`, so the `EditContext` identifies the copy's field rather than the untouched row's |
| **Blazor approach** | Extend/wrap `Microsoft.AspNetCore.Components.QuickGrid`, reusing its native `Sortable` columns, `PropertyColumn`/`TemplateColumn`, `RowClass` (per-row CSS) and `OnRowClick` (row click-through) rather than reinventing them. `FormaDataGrid` keeps its own `PaginationState` to drive QuickGrid's slicing but renders `FormaPagination` — not QuickGrid's `Paginator` — for the page controls, translating `PaginationState` to `FormaPagination`'s plain parameters; the grid adapts to the paginator, never the reverse. Add: checkbox column (mass-select, pairs with `FormaSelectionToolbar`) with a tri-state select-all in its header that spans the whole `Items` set rather than the visible page — QuickGrid owns the sort and the page slice, so the rows on screen are not a set the component can name. Action-icon column. Responsive column hiding is optional, not required parity — Coi itself only wraps the table in a scrolling `.table-responsive` container, it never hides columns |

### 8. `FormaSelectionToolbar` ✅

| | |
|---|---|
| **Old pattern** | "Select all" checkbox in header + per-row checkboxes. When items selected, show action bar (delete selected, change status, export). `InitTableEvents()` / `$('.cbx').prop('checked', ...)` |
| **Used in** | pages, products, orders, leads, comments (5 of 7 list pages) |
| **Parameters** | `int Count`, `string? Caption` (the localized selected-item count when omitted), `IReadOnlyList<FormaToolbarItem> Items`, `string? Label` (required once `Items` is non-empty), `EventCallback<string> OnCommand`, `EventCallback OnClear` |
| **Blazor approach** | Pairs with `FormaDataGrid`'s checked selection, taking only the count so it stays non-generic. Renders its actions through `FormaToolbar` with `ShowLabels`, keeping `FormaCloseButton` beside it as the clear control. Collapses through `grid-template-rows: 0fr → 1fr` rather than an absolutely positioned cross-fade, so no wrapper positioning is imposed on the caller. `inert` sits on the controls alone, leaving the caption's live region in the accessibility tree so the first count change is announced |
| **Notes** | Renamed from `FormaContextToolbar` — "context" named the circumstance rather than the subject, and the subject is the selection. `role="toolbar"` now arrives from `FormaToolbar`, which still owes arrow-key roving focus; that gap is `FormaToolbar`'s to close and is tracked there rather than here |

### 38. `FormaField` ✅

| | |
|---|---|
| **Old pattern** | `<label class="lblForm">` over a `.form-control`, floating on focus or value, with `<span class="mand">` for required and the error text below |
| **Used in** | Chrome for every labelled form component — `FormaTextBox`, `FormaTextArea`, `FormaPasswordBox`, `FormaSelect`, `FormaMultiSelect`, `FormaDatePicker`, `FormaColorPicker`, `FormaSlider` and `FormaToggleSwitch` |
| **Parameters** | `string? Label`, `string? TargetId`, `bool Required`, `bool Multiline`, `FieldLabelMode LabelMode`, `RenderFragment ChildContent` (the input element), `RenderFragment? Validation` |
| **Blazor approach** | Renders the wrapper, label and validation slot, and styles the input element through `::deep .forma-field-input` — the one mechanism that reaches markup a *consuming* component rendered, since CSS isolation otherwise scopes each component's sheet to its own output. Non-generic: the consumer passes its own `<ValidationMessage For="...">` into `Validation` rather than the chrome taking a typed expression. `Multiline` anchors the label to the top, because an input element taller than one line has no vertical centre to sit against |
| **Notes** | Exists so the floating label, border, focus ring and validation styling live in one file. Without it each labelled form component reimplements ~110 lines and they drift apart |

### 39. `FormaTextArea` ✅

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="MultiLine">` — the same `.form-control` class as a single-line field, plus `textarea { resize: vertical }` and a label offset (`.row textarea ~ label.lblForm { top: 13px }`) |
| **Used in** | SEO head, meta description, store details — and in Épica, article excerpts and summaries |
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `int Rows` (default 3), `int? MaxLength`, `bool ShowCharacterCount` |
| **Blazor approach** | `InputBase<string?>` rendering a `<textarea>` into `FormaField` with `Multiline`. Owns only the multi-line deltas — `resize: vertical` and a minimum height — so it cannot drift from `FormaTextBox`. No JS: autofill does not apply to a textarea |
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
| **Parameters** | Standalone, no knowledge of QuickGrid or any other component: `int TotalItems` (required), `int PageSize` (required), `int CurrentPage` (required), `EventCallback<int> OnPageChanged`, `PaginationOptions Options`, plus captured pass-through attributes |
| **Options** | How the reader moves — the navigation input in place of the steps, the Go button, a wheel gesture over the pager — travel together in a `PaginationOptions` record rather than one parameter each. A host that composes the pager therefore forwards a single parameter, and a new option later costs the record plus this component and touches no host |
| **Blazor approach** | A self-contained paginator, usable on its own (e.g. the image library) or composed into `FormaDataGrid`. Dependency direction is one-way: larger components adapt to `FormaPagination`, it never adapts to them |

---

## Tier 3 — Form Components

### 12. `FormaInput` — removed, superseded by `FormaTextBox`

| | |
|---|---|
| **Old pattern** | `<asp:TextBox>` + `<label class="lblForm">` positioned over input, animates up on focus/has-value. Validation error shown below. Optional mandatory indicator `<span class="mand">` |
| **Used in** | category popup, user popup, filters, import, contact module, and all form pages |
| **Now** | `FormaTextBox` with a `Label`. It renders the same `<input>` into the same `FormaField` and carries the same autofill detection, over a closed set of text types rather than a free-string one |

### 13. `FormaToggleSwitch` ✅

| | |
|---|---|
| **Old pattern** | jQuery Toggles plugin — styled checkbox as iOS-style toggle with on/off labels, CSS animated slider |
| **Used in** | page editor (SEO, comments, password), product (active, featured), settings (multiple), store-settings (multiple), coupons (active), user (active), order (paid) |
| **Parameters** | `@bind-Value` (bool), `string? Label`, `string? OnLabel`, `string? OffLabel`, `bool Disabled` |
| **Blazor approach** | Pure CSS toggle (hidden checkbox + styled `<label>` with `::before`/`::after` pseudo-elements for the sliding knob). No JS needed |

### 14. `FormaColorPicker` ✅

| | |
|---|---|
| **Old pattern** | `tcp.js` custom color picker — click swatch → popup with gradient/hue selectors, hex input, opacity slider (`no-alpha` class disables) |
| **Used in** | design (bg, primary, secondary, text, sidebar, link colors), settings (popup bg), store-settings (e-commerce accent), slider (bg, arrow, video filter), forms (text, button bg/text), product (badge) |
| **Parameters** | `@bind-Value` (string hex), `string Label` (required, validated), `bool ShowAlpha` |
| **Blazor approach** | `InputBase<string>` over a hex text field, with a native `<input type="color">` laid transparently over the swatch beside it — clicking the swatch opens the platform's own colour dialog with no popup of Forma's own. The hex field carries `dir="ltr"` so the value keeps its reading order in an RTL page, and `ShowAlpha` adds an alpha row and widens the field to `#RRGGBBAA`. No external JS |

### 15. `FormaDatePicker` ✅

| | |
|---|---|
| **Old pattern** | EasePick CDN library with Hebrew locale, year dropdown, reset button, min-date lock |
| **Used in** | coupons (start/end dates) |
| **Parameters** | `@bind-Value` (DateOnly?), `@bind-StartDate` / `@bind-EndDate` (DateOnly?), `string Label` (required, validated), `DateOnly? MinDate`, `DateOnly? MaxDate`, `bool ShowPresets` (default true), `EventCallback OnSelectionComplete`, `bool UseNonGregorianMonths`, `AdditionalAttributes` |
| **Popup placement** | The calendar is a `popover`, so it renders in the **top layer** and no scrolling or clipping ancestor can cut it off — it works inside a grid's scroll region, a dialog body, or any `overflow: auto` box. The top layer keeps no layout tie to the trigger, so the shared `wwwroot/scripts/behaviours/AnchoredPopup.js` behaviour measures and applies the position: it hangs from the trigger's inline-start edge (mirrored in RTL), flips above when there is no room below, clamps into the viewport, and follows scroll and resize. One positioning path for every browser rather than a CSS/JS split. That behaviour is deliberately component-agnostic, and `FormaSelect`, `FormaMultiSelect` and `FormaDropDownMenu` share it. `popover="manual"` keeps the existing backdrop as the dismissal mechanism, which also sidesteps the iOS 17–18.2 bug where a popover ignores a tap outside it |
| **Blazor approach** | A bespoke calendar rather than `InputDate`, because a native `<input type="date">` offers no range selection, no presets, and no non-Gregorian months. A `<button>` trigger opens an anchored popover holding one calendar panel, or two side by side once `StartDateChanged` or `EndDateChanged` is bound, which is what puts the picker in range mode. Months come from `CultureInfo.CurrentCulture`, and `UseNonGregorianMonths` switches to the culture's own calendar — the Hebrew calendar under `he` — by cloning `DateTimeFormatInfo` onto it. `MinDate` greater than `MaxDate` throws from `OnParametersSet` |

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
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `bool ShowToggle = true`, `string AutoComplete = "current-password"`, `PasswordStrengthOptions StrengthOptions` (`ShowMeter`, `Evaluator`, `ShowAdvice`, `Advisor`, `ForbiddenTerms`) |
| **Blazor approach** | Composes `FormaField` for the label, border and validation chrome. `InputBase<string?>` with a dynamic `type` toggled by a button click in C#; the value stays `dir="ltr"` regardless of page direction so a revealed password never reorders, with the gutter, alignment and Edge's own injected `::-ms-reveal` handled per direction in CSS. No JS beyond the shared autofill-detection module |
| **Notes** | The strength meter rates length (discounted for a repeating unit), case mix, digits and symbols into a five-step `PasswordStrength` enum, shown as filled segments plus a translated phrase — never colour alone. `ShowAdvice` lists the specific criteria a value still lacks, sharing its checks with the rater so the two cannot disagree. `ForbiddenTerms` rejects a password that draws on another field's value (email, phone) as a whole or by any alphanumeric fragment of at least four characters, through a `ValidationMessageStore` rather than a private error, so it participates in the form's own validation summary |

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
| **Parameters** | `@bind-Value` (`List<TValue>`), `IReadOnlyList<SelectOption<TValue>> Options` (required), `string Label` (required, validated), `bool Required`, `string? Placeholder`, `bool AllowAdd`, `EventCallback<string> OnAddNew`, `bool Disabled` |
| **Blazor approach** | Interactive component. Clicked → show dropdown with checkboxes. Selected items shown as removable tags above/inside the input area. Search/filter within dropdown. No external JS |

### 20. `FormaSelect` ✅

| | |
|---|---|
| **Old pattern** | JS replaces native `<select>` with styled div: `.custom-select > .cs-val` (display) + `.cs-options > .cs-option` (dropdown). Click to open, click option to select, click outside to close |
| **Used in** | Multiple admin pages for styled dropdowns |
| **Parameters** | `@bind-Value` (any `TValue`, nullable to allow no selection), `IReadOnlyList<SelectOption<TValue>> Options` (required), `string Label` (required, validated), `string? Placeholder`, `bool Required`, `bool Disabled` |
| **Blazor approach** | A `<button>` trigger plus an anchored, keyboard-navigable listbox popup, sharing `AnchoredPopup.js` with `FormaDatePicker` and `FormaMultiSelect` so the list escapes any clipping ancestor. `InputBase<TValue>` directly, matching `InputDate<TValue>`/`InputNumber<TValue>` — the caller supplies a nullable `TValue` (`int?`, `string`, `MyEnum?`) when "nothing selected" needs representing, rather than the component forcing that shape itself |
| **Notes** | Composes `FormaField` in `Pinned` mode, as `FormaMultiSelect` and `FormaDatePicker` do: the trigger always shows content of its own, so the label stays floated rather than sitting over it. Opens focused on the current selection, the way a native `<select>` does. The selected option is marked by a checkmark plus bold weight, not colour alone |

---

## Tier 4 — Interactive Widgets

### 21. `FormaSortableList<TItem>` ✅

| | |
|---|---|
| **Old pattern** | jQuery Dragsort / HTML5 sortable — drag items to reorder. On drop, serialize new order and POST to `sorting.asmx/Sort`. Used for menus, galleries, sliders, categories, product options, delivery methods, form fields, filters |
| **Used in** | menu sort page, gallery module, slider editor, category module, product options, store-settings delivery, contact form builder, filters editor |
| **Parameters** | `@bind-Items` (`IReadOnlyList<TItem>`, required), `RenderFragment<TItem> ItemTemplate` (required), `Func<TItem, object> ItemKey`, `SortableDragFrom DragFrom` (Handle/Item), `bool Disabled`, plus captured pass-through attributes |
| **Blazor approach** | Uses native HTML5 Drag and Drop API (`draggable`, `@ondragstart`, `@ondragover`, `@ondrop`). Track drag source/target index, reorder the list in C#, re-render. Minimal JS interop (only if `preventDefault` is needed on `dragover`) |

### 22. `FormaImagePicker` ✅

| | |
|---|---|
| **Old pattern** | The browse button in `pops/images.aspx`'s upload panel, filtered to image types |
| **Used in** | Anywhere a *new* image comes off the user's device — page editor (cover image, OG image), product editor (images, gallery), design (logo, bg, top image), slider (slide bg, elements) |
| **Parameters** | The same set as `FormaFilePicker` (#34); its `Options.Accept` is pinned |
| **Blazor approach** | A thin skin over `FormaFilePicker` that pins `Accept` to image types and renders a thumbnail preview per accepted file. Carries no dialog, drop, validation or transfer logic of its own |
| **Notes** | Picks from the **device**, not the library. Choosing an image that is already on the server is `FormaExplorer` (#53) — the two are different questions and answering both with one component is what made the old naming wrong |

### 58. `FormaMediaPicker`

| | |
|---|---|
| **Old pattern** | None — Coi's library was images-only, and video arrived as a pasted YouTube URL |
| **Used in** | Surfaces taking either an image or a video off the device — Épica's Videos module, article media, and `FormaComposer` (#36) |
| **Parameters** | The same set as `FormaFilePicker` (#34); its `Options.Accept` is pinned |
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
| **Parameters** | `RenderFragment Trigger` (required — the trigger's content), `RenderFragment ChildContent` (required — menu items), `RenderFragment? HeaderContent`, `ButtonOptions TriggerOptions`. `FormaDropDownMenuItem` takes `string? Href`, `bool Disabled`, `EventCallback OnClick`, `bool KeepsOpen` (activation leaves the menu open, for an item that changes the menu rather than completing it); `FormaDropDownMenuDivider` takes none |
| **Blazor approach** | The component renders its own trigger as a `FormaButton` rather than accepting a whole button from the caller, so it owns `aria-haspopup`, `aria-expanded` and `aria-controls` instead of hoping a supplied fragment carries them; a wrapper span gives the popup something to measure and catches the keydown that a component tag cannot. The panel is a `popover="manual"` in the top layer positioned by the shared `AnchoredPopup.js`, with a backdrop owning dismissal. Items register with the menu through a `CascadingValue`, as `FormaTab` does with `FormaTabs`, so arrow, Home and End navigation works over whatever the caller nested, and Tab moves between the items rather than leaving the menu. A colocated `.razor.js` cancels only the four navigation keys, since Blazor fixes `preventDefault` at render time and a blanket one would also block Enter, Space and Tab |
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
| **Blazor approach** | `FormaDialog` containing `FormaTextArea` + save button. Simple composition |

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
| **Blazor approach** | Interactive card grid. Each card is a filter group with `FormaSortableList` of values inside. Reuses `FormaTextBox` for inputs |

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
| **Blazor approach** | Searchable grid inside `FormaDialog`. Each cell shows font name + sample text styled with that font. Load font CSS on-demand |

### 32. `FormaButton` ✅

| | |
|---|---|
| **Old pattern** | `.btn`, `.btn-primary`, `.btn-sec`, `.btn-thrd`, `.btn-mini`, `.btn-load` (fa-3.6.5.css) — the shared button vocabulary every admin page's action bar, modal footer, and grid row built on top of |
| **Used in** | Single source of truth for buttons across the library — `FormaDataGrid` row/inline-edit actions, and the retrofit target for `FormaDialog`, `FormaConfirmDialog`, `FormaPagination`, and `FormaDatePicker` |
| **Parameters** | `ButtonVariant Variant` (Secondary/Primary/Tertiary/Danger/Ghost, default Secondary), `ButtonSize Size` (Normal/Mini, default Normal), `ButtonType Type` (Button/Submit/Reset, default Button), `bool IconOnly`, `bool Busy`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, `RenderFragment? ChildContent`, plus captured pass-through attributes |
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
| **Parameters** | `EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected` (required), `EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected`, `FilePickerOptions Options` (`Accept`, `AllowMultipleFiles`, `MaxFileSize`, `MaxFiles`), `string? Prompt` |
| **Blazor approach** | `InputFile` behind a styled drop zone, the input nested inside the `<label>` so a click anywhere opens the dialog with no script at all. The DOM exposes file drag-and-drop only through its own `drop` event, so a colocated module assigns the dropped `DataTransfer.files` onto the input and dispatches `change` — a drop and a click therefore converge on one code path and nothing downstream needs to know which happened. Size, count and type are validated in the component, which **reports rejects rather than throwing**, and judges each file on its own: a five-file drop against a cap of two yields two accepted and three rejected rather than losing the batch. `Accept` is re-checked in C# because a drop never consults the dialog's own filter |
| **Notes** | Picks from the **user's device**. It opens an OS dialog or takes a drop and yields `IBrowserFile`s the server has never seen — it is not a browser of stored files, which is what `FormaExplorer` (#53) and `FormaFileManager` (#54) are. Owning no transfer, it deliberately shows no progress: a progress bar measures bytes moving, and moving them is `FormaFileUploader`'s job |

### 35. `FormaFileUploader` ✅

| | |
|---|---|
| **Old pattern** | The handler `pops/images.aspx` posted to — the half that actually wrote the bytes into the media folder and drove the progress bar |
| **Used in** | Behind every surface that accepts a new file: `FormaExplorer` (#53), `FormaFileManager` (#54), `FormaComposer` (#36), and any editor taking a file directly rather than through a library |
| **Parameters** | `IReadOnlyList<IBrowserFile>? Files`, `IFileStore? Store`, `FileUploaderOptions Options`, `EventCallback<IReadOnlyList<ResourceInfo>> OnCompleted`, `EventCallback<UploadFailure> OnFailed`. Public API: `StartAsync()`, `Cancel()`, `IsUploading` |
| **Options** | How the set runs and what it reports travel together in a `FileUploaderOptions` record rather than one parameter each: `AutoStart` (default `true`), `StopOnFailure`, `RollbackOnFailure`, and the measurements `ShowByteCount`, `ShowElapsed`, `ShowTransferRate`, `ShowEstimate`, `ShowTotalMetrics`, `ShowSummary`. `Default` shows the progress bar and state alone; `Detailed` turns every measurement on |
| **Blazor approach** | Takes the files a `FormaFilePicker` produced and moves them one at a time, owning the entire transfer: opening each stream with the file's own size as the ceiling, pumping it, counting bytes through a wrapping stream to drive a per-file progress bar, disposing it, and cancelling the set. **Where the bytes land stays the host's**, supplied as an `IFileStore` — `SaveAsync` takes a file's content and returns the `ResourceInfo` it became, and `DeleteAsync` reports whether it removed one. The store comes from the `Store` parameter or, when that is unset, from the host's own registration, so one registration can serve every instance. Progress arrives on whichever thread the store's read continuation resumed on, so both the list mutation and the render are marshalled back through `InvokeAsync` |
| **Notes** | Returning `ResourceInfo` is what makes the chain close: it is exactly what `FormaExplorer` and `FormaFileManager` list, so an upload feeds the browsing surface with no host-side translation. `IFileStore` and `FileContent` live in `Forma.Framework.Resources` beside `ResourceInfo`, which carries no ASP.NET Core reference, so a host implements a store from a plain class library. A failed file leaves the rest of the set running unless `StopOnFailure` marks the remainder skipped, and `RollbackOnFailure` removes what already landed — a store that reports it did **not** remove a file is itself a failure rather than a silent success. **Settled when built:** it composes beside a picker rather than rendering one, and retrying a single failed file is not in the initial release |

### 36. `FormaComposer`

> **[docs/composer-design.md](docs/composer-design.md) is authoritative for this component.** The rows below are the summary; the design document owns the plugin model, the assembly layout, and the phases.

| | |
|---|---|
| **Old pattern** | **Two Coi surfaces, not one.** The rich text editor was CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px; custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`; `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction, and `allowedContent = true` — no content filtering whatsoever. The larger surface was the **page designer**, `admin/pops/template.aspx` over `Controls/Edit/`: a drag-and-drop page builder with a side palette, a live canvas, per-element delete/style/edit chrome, jquery-sortable reordering, column and element resizing, and a settings editor per element kind in `elm.js`. One entry point, `OpenTemplate(type, title)`, drove seven targets by numeric type — product page templates, the product tile, headings, rich content, popups and the custom dropdown menu |
| **Used in** | page editor, product description, product and category templates, popups, custom menus — and in Épica, the News article body |
| **Toolbar (from Coi's `Website` preset)** | Source · Preview · Templates — Cut/Copy/Paste/PasteText/PasteFromWord · Undo/Redo · SelectAll · RemoveFormat — Smiley · SpecialChar · LineHeight · Maximize — Bold/Italic/Underline/Strike — Numbered & bulleted lists — Justify right/center/left/block · BidiRtl/BidiLtr — Link/Unlink/Anchor — YouTube embed · Image · Table · HorizontalRule — Format · Font · FontSize — TextColor — image library. Subscript/Superscript were explicitly removed; Forma keeps them available but outside the default preset |
| **Designer palette (from `template.aspx`)** | Text, image, Facebook, contact form, table/cells, horizontal rule, button, image slider, add-ons, image gallery, YouTube/video, map, spacer, code/script — plus host-domain token elements swapped in per target: product name, summary, images, price, details, body, options, buy button and quantity; the mini-product tile variants; and page name, summary, image and read-more for a category tile |
| **Parameters** | `@bind-Value` (`ComposerDocument` — content plus the format it is written in), `ComposerOptions Options`, the registered plugin set, and `EventCallback`s for media requests and document change. Presets are separate components rather than a toolbar enum: `FormaComposer` (everything), `FormaStandardRichTextEditor`, `FormaCompactRichTextEditor` |
| **Blazor approach** | **Forma's own editor, and an engine rather than a single component.** Every capability is a plugin, *including each editing surface* — `RichTextEditorPlugin`, `MarkdownEditorPlugin`, `CodeEditorPlugin` and `PageDesignerPlugin` implement the same `IComposerEditorPlugin`, so there is no closed set of modes and a host can add its own. The designer adds a second extension point, `IComposerDesignerPlugin` contributing `ComposerWidget`s, because half Coi's palette was host-domain and Épica's would be too. **A text widget hosts `FormaRichTextEditor`**, which is the recursion Coi had through `CKEDITOR.inline` and the reason the component is called a composer at all. Chrome composes the library: `FormaSelect` for format/font/size, `FormaDropDownMenu` for overflow, `FormaColorPicker` for colour, `FormaExplorer` for stored media, `FormaFilePicker` plus `FormaFileUploader` for uploads, `FormaVideoPlayer` for embeds. Contracts and engine live together in `Forma.Features.Composer`, which the library takes because a composer cannot render without it, and satellites (`.Markdown`, `.TextMate`, `.KaTeX`) carry the external dependencies one package each. Undo/redo, selection, clipboard, conversion, sanitisation and asset loading are engine concerns shared by every editor — so an undo inside a text widget and a widget reorder sit on one stack |
| **Designer document** | A **`ComposerWidgetTree` — JSON, with HTML as output rather than storage.** Each node carries an `InstanceId`, the `WidgetId` of its kind, canvas-edited `Content`, panel-edited `Settings` as an opaque `JsonElement`, and `Children`; columns are nodes rather than an array on the row. Coi saved HTML plus a scoped `<style>` block, which froze rendering decisions at save time — a baked `<img src>` and `style="width:60%"` that a CDN move or theme change could never reach — and never escaped the tree anyway, since recovering a widget needs `data-widget` and `data-settings` attributes read back with an HTML parser. `FormaWidgetRenderer` walks the tree for public pages under static SSR. Document metadata — identity, author, timestamps, publish state, access — stays on the host's row, not least because a client-authored blob cannot be trusted with authorization |
| **Conversion** | Markdown → HTML is always available. **HTML → Markdown is offered only when the document is within the subset Markdown can represent**, and is refused with the blocking features named otherwise — Markdown cannot carry font family, size, colour, highlight, line height, underline, alignment or per-block `dir`, every one of which this component offers. Silently discarding an author's formatting on a switch is worse than declining the switch |
| **Security** | Coi's `allowedContent = true` made the editor a stored-XSS pipe. Sanitisation through `IHtmlSanitizer` is mandatory and runs on load, on paste, on leaving the code editor and on raising content to the host; a host may swap the built-in allowlist for a hardened implementation. `IComposerSyntaxHighlighter` returns **tokens, not markup**, so a highlighter cannot introduce script. `qform`'s `<script src>` injection is replaced by a declarative host-resolved embed |
| **External JS** | This component carries the library's standing carve-out from the JS-free rule, granted by category rather than per library: an editor hosts capabilities — Markdown rendering, maths typesetting, syntax highlighting — that are solved problems nobody should reimplement. Each lives in its own `Forma.AddOns.Composer.{Capability}` satellite behind a plugin contract, ships as static assets under `_content/…` with no bundler, stays in a supporting role, and degrades to absent. Markdig, KaTeX and TextMateSharp all qualify — TextMate because `.tmLanguage.json` needs an Oniguruma-compatible engine that `System.Text.RegularExpressions` is not. The editing surface, selection, undo stack and toolbar stay Forma's, in C# |

### 37. `FormaCloseButton` ✅

| | |
|---|---|
| **Old pattern** | The `×` dismiss control repeated across `admin/pops/*.aspx` modal headers and notification strips |
| **Used in** | Single source of truth for dismiss controls — `FormaDialog`'s header, `FormaToastContainer`'s toasts, `FormaSelectionToolbar`'s clear-selection control |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: close, dismiss, clear selection), `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, plus captured pass-through attributes |
| **Blazor approach** | A thin wrapper over `<FormaButton Variant="Ghost" IconOnly>` supplying the one X glyph, so it owns no colours of its own and inherits the ghost variant's treatment and the icon-only square sizing. `Label` is required rather than defaulted because a wrong accessible name is worse than an obvious compile error, and the three call sites genuinely differ. Ships no `.razor.css` — every visual decision belongs to `FormaButton` |
| **Notes** | Before this existed the three call sites had each reimplemented `.forma-button-ghost` verbatim and drifted: two drew a 14×14 SVG while the toast used the `&times;` entity, and the focus ring was `--focus-color` against `FormaButton`'s `--main-color`. Retinting for a saturated surface (the toast) goes through `--forma-button-ghost-*-color` knobs rather than a fork |

### 40. `FormaEyeButton` ✅

| | |
|---|---|
| **Old pattern** | `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` beside a password field — a clickable span, not a button |
| **Used in** | Single source of truth for reveal controls — `FormaPasswordBox`'s toggle, and any later show/hide or preview affordance |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: show password, hide value, preview), `bool Revealed`, `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick`, plus captured pass-through attributes |
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

### 45. `FormaOverflowMenu` ✅

| | |
|---|---|
| **Glyph** | An **ellipsis** — `⋯` horizontal in a toolbar, `⋮` vertical in a grid row, where a column is narrow and a row is tall |
| **Old pattern** | `Info('divId', '#buttonId', 's')` opening a `dropdown-menu` from a row's options button |
| **Used in** | Row actions once a row carries more than two, which is the threshold `FormaDropDownMenu` already names |
| **Parameters** | `RenderFragment ChildContent` (required — the same `FormaDropDownMenuItem`/`Divider` children), `RenderFragment? HeaderContent`, `MenuOrientation Orientation`, `string Label` (required), `ButtonOptions TriggerOptions` (default `ButtonOptions.GhostIcon`, its `IconOnly` always pinned), `bool Disabled`, plus captured pass-through attributes |
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
| **Parameters** | `IReadOnlyList<IExplorerItem>? Items` or `IExplorerItemsProvider? ItemsProvider` (exactly one), `IReadOnlyList<IExplorerItem>? Folders`, `IExplorerFolderItem? CurrentFolder`, `ExplorerViewMode ViewMode`, `ExplorerSortField SortField`, `ExplorerSortDirection SortDirection`, `IReadOnlyList<IExplorerItem> Selected`, `int CurrentPage`, `int PageSize`, `int? TotalCount`, `long? TotalBytes`, `string? SearchPlaceholder`, `ExplorerOptions Options`, `ExplorerItemOptions ItemOptions`, `ExplorerConstraints Constraints`, `IFileStore? Store`, plus `EventCallback`s for selection, view mode, paging, upload, rename, delete, drag-drop, refresh, rejection and every folder operation. The whole host boundary speaks items: every request and folder change hands back the `IExplorerItem` the host supplied, so a folder with no `ResourceInfo` behind it can be browsed, targeted and reported like any other |
| **Blazor approach** | A manager for media **already on the server**, in the shape of a resource explorer: a resizable folder-tree sidebar, an address bar with back/forward/up and a breadcrumb menu, thumbnail/details/list views, drag-and-drop between folders, and enlargement through `FormaFileViewer` (#56) rather than a viewer of its own. New media arrives through `FormaFilePicker` (#34) plus `FormaFileUploader` (#35) in its toolbar, so it neither opens an OS dialog nor moves bytes itself. It owns no storage — every mutation is reported as intent through a callback. Configuration splits in two: `ExplorerItemOptions` gates every mutating capability per-caller — the item and folder capabilities the explorer permits, intersected with what each item itself supports, plus `AllowUpload` — while `ExplorerOptions` covers how it presents itself — dialog and resizable modes, sidebar and breadcrumb-menu visibility, the optional details columns, whether the collection is paged or scrolls infinitely, and `SelectOnOpen` for a caller that exists only to choose, which starts in selection mode and drops the toggle out of the toolbar rather than making a picker press Select first. Each item also carries its own state: a hidden item leaves the listing and its counts, an invisible one keeps its place undrawn, and a disabled one is drawn but supports no operation. The read, write, and execute capabilities are not supported by the explorer yet and are reserved for future use; open is supported for folders only. The media scrolls inside its own panel so the toolbar, folder tree and pager stay put; the view components read what they draw through a cascaded `IExplorerView` and ask for changes through a cascaded `IExplorerInteraction`, so adding an action or a column is a one-file change and every selection change is reported exactly once |
| **Data** | Two ways in, exactly one of which must be supplied, following `FormaDataGrid`'s `Items`/`ItemsProvider` split. `Items` hands over a collection the explorer holds and filters, sorts and pages itself — right for a few hundred items. `ItemsProvider` answers one `ExplorerItemQuery` at a time and the explorer holds only the window it is showing, so a collection far too large to enumerate stays browsable. **The query carries only what the explorer owns** — the window, its search box and its sort — because the folder is the host's own `CurrentFolder` parameter and the implementation already has it; the same closure supplies any other filter the explorer never needs to learn about. `ExplorerItemPage.TotalCount` is optional, so a store that cannot count cheaply returns null and the explorer drops its pager rather than demanding a number. A store that returns hidden items counts them in that total; the explorer pages and asks for the next window in the store's own count, and reports an appended window's progress against the total less the items it has left out. Typing is debounced (`Options.SearchDebounceDelay`), a superseded query is cancelled rather than merely ignored, and a mutation re-reads the window |
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
| **Notes** | It shares its browsing half with `FormaExplorer` (#53) — breadcrumb, resource grid, selection, pagination and the picker-plus-uploader toolbar. `IExplorerResourcesProvider` is the shape to copy when this is built, and it deliberately stays in the explorer feature rather than in `Forma.Framework.Resources`: a browsing contract was moved down there once and immediately leaked folders into an assembly that models resources alone. **To settle when built:** whether a shared contract is extracted once there are genuinely two consumers, or the two surfaces keep their own |

### 55. `FormaVideoPlayer` ✅

| | |
|---|---|
| **Old pattern** | CKEditor's **YouTube embed** toolbar button (#36) and the raw `<iframe>` markup it pasted into the body |
| **Used in** | The most-demanded component on this list — Épica wants it on four separate surfaces: YouTube-by-URL embeds in articles (6.5), the auto-embedded live broadcast player (9.2), the Videos module's listings and featured video (P10), and the home page's featured video block (11.4). `FormaFileViewer` (#56) delegates its video branch here, and `FormaComposer` inserts it |
| **Parameters** | `string Source` (required), `string Title` (required — the player's accessible name), `bool Autoplay`, `bool DeferLoading` (default `true`), `string? Placeholder`, `IReadOnlyList<VideoCaptionTrack>? Captions`, plus captured pass-through attributes |
| **Blazor approach** | Resolve the address through an ordered chain of `IPlayerProvider`s (`IPlayerProvider`, `PlayerOptions` and `PlayerFrame` live in `Forma.Features.Media`, and so do the built-in providers), so a watch link, a share link and an embed link all work without the caller naming the service. Providers the host registered are tried first, so an application can teach the player an address shape this library never knew — or take over one it would otherwise claim — the same way a host extends `FormaFileViewer`; the eight built-ins follow, with `FilePlayerProvider` last because it accepts any fetchable address. YouTube, Vimeo and Twitch return an embed address the player loads in an `<iframe>`; TikTok, Instagram, Facebook and X publish markup plus a script instead, which a colocated JS module loads once and asks to scan, each service sizing its own player; any other fetchable address falls back to the browser's own `<video>`, with optional `<track>` captions. Size with the `aspect-ratio` property rather than a padding hack. **External JS is confined to the four services whose player *is* a script** — they publish no embed address, and the iframe providers need none |
| **Privacy and weight** | Nothing is fetched from a hosting service until a reader presses play: `DeferLoading` holds both the embed address and the service script behind a play control, which `Autoplay` overrides and a self-hosted file never needs. Embed from `youtube-nocookie.com`, and ask for do-not-track wherever a service offers it — `dnt=1` on Vimeo, `data-dnt="true"` on X. A third-party frame that loads with every article page is both a tracking surface a public news site has to account for and a large fixed cost on a mobile connection. **The still is the host's to supply:** `Placeholder` takes an image the host serves from its own origin, and stands in for a self-hosted file too. Deriving one from the service instead — YouTube publishes a thumbnail per video id — would send the service the very request the play control exists to withhold, so the component never does it |
| **Notes** | `Title` is required because an unnamed `<iframe>` is an unlabelled stop in the tab order. **Autoplay is not what Épica's 9.2 asks for** — "auto-embed live player" means the player appears without an editor placing it, not that it starts playing. Autoplay is forced muted through the JS property, because the `muted` attribute no longer reaches the element once Blazor has created it, and WCAG 1.4.2 requires a control over anything that plays by itself. A service script that never runs — blocked, or simply slow — leaves a link to the video in the player's place |

### 56. `FormaFileViewer` ✅

| | |
|---|---|
| **Old pattern** | The preview pane of `pops/images.aspx` — clicking a library entry to actually look at it |
| **Used in** | The promise #35 already makes and does not own: showing whatever a chosen resource turns out to be. That belongs here, so the picker, the manager (#54) and the explorer's enlarge (#53) show one preview implementation rather than three |
| **Parameters** | `string Source` (required), `IReadOnlyList<string>? Siblings`, `EventCallback<string> OnNavigate`, `bool ShowMetadata`, `bool ZoomOnWheelGesture = true`, `RenderFragment<string>? UnsupportedTemplate`. The caller passes an address, not a resolved resource — a component handed a `ResourceInfo` would need every caller to resolve one first, which is the provider's job. `ResourceInfo` (`Source`, `Name`, `Size`, `CreatedAt`, `ModifiedAt`) and `IResourceProvider` live in `Forma.Framework.Resources`, which carries no ASP.NET Core reference — so a converting package stays a plain class library. Sizes and durations are written for a reader by `ResourceFormatter`, which a view calls when it displays one rather than the model carrying a preformatted string. What a resource **is** is carried by its type rather than by an enum: `FolderResourceInfo` and `FileResourceInfo` split container from content, and `ImageResourceInfo`, `VideoResourceInfo`, `AudioResourceInfo`, `DocumentResourceInfo` and `ArchiveResourceInfo` refine the file branch, each owning the detail it alone has. `IVisualResource` and `ITimedResource` cross the branch for consumers that care only about pixels or running time. Named for resources rather than media because the viewer renders documents too |
| **Blazor approach** | An ordered chain of `IResourceProvider`s resolves the address into a `ResourceInfo`, mirroring `FormaVideoPlayer`'s provider chain: the first to claim it decides what the resource is, and the viewer owns the markup for every kind, so a converting package never ships Razor or CSS. Resolution runs once per address rather than once per render. Providers the host registered are tried first; `BrowserResourceProvider` comes last and claims only what a browser renders unaided, mapping the media type onto the matching resource type, so **an address no provider claims degrades to a type glyph, the name, the size when one is known, and a download link** rather than a broken embed. A `MarkupResourceInfo` is rendered as trusted markup, so a converting provider must sanitise its output. A `DocumentResourceInfo` whose content type is `text/*` is framed with `sandbox=""` — an opaque origin, withholding scripts, forms and navigation — because uploaded HTML is untrusted; anything else goes to `<object>` with a new-tab link as fallback content. With `Siblings` supplied the viewer steps through the set, resolving the horizontal arrow keys through `IsRightToLeft`; the host dialog keeps `Escape` |
| **Converting packages** | `Forma.AddOns.Resources.Markdown` (Markdig) and `Forma.AddOns.Resources.Office` (OfficeIMO) ship separately and return `MarkupResourceInfo`, so neither Markdig nor OfficeIMO lands in the package every consumer downloads. Each reads its bytes through an `IResourceContentReader` the host implements, so a converting package never assumes where a file lives, and sanitises its output with `HtmlSanitizer` before returning it. A host registers them like any other service — `AddScoped<IResourceProvider, …>()` — and without them a `DocumentResourceInfo` still works for whatever the browser handles natively |
| **Zoom and pan** | Settled: the frame is a plain scrollable region (`overflow: auto`) rather than a transform-driven canvas. Touch and pen pan it natively, and `touch-action: pan-x pan-y pinch-zoom` hands pinch-to-zoom to the platform itself — the exact case CSS was expected to handle worst turned out not to need JS at all. The one gap native scrolling doesn't cover is a mouse click-drag, so a small colocated `.razor.js` adds that, gated to `pointerType === 'mouse'` so it never fights the browser's own touch scrolling. Zoom level moves in five fixed steps (100–400%) through on-screen +/− buttons and Ctrl with a wheel rolled over the image, through the shared wheel behaviour; the plain wheel keeps scrolling a zoomed image, as it does anywhere |
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

### 59. `FormaTextBox` ✅

| | |
|---|---|
| **Old pattern** | `<input type="text" class="form-control">` without floating labels or dedicated label wrappers |
| **Used in** | Every text field: labelled form fields, compact inline forms, inline grid filters, dialog fields, toolbars and filter rows |
| **Parameters** | `@bind-Value`, `string? Id`, `string? Placeholder`, `string? Label`, `bool Required`, `bool Disabled`, `bool ReadOnly`, `TextBoxOptions Options` (`Type`, `MinLength`, `MaxLength`, `Pattern`, `InputMode`, `EnterKeyHint`, `AutoCapitalize`, `SpellCheck`, `AutoComplete`; presets `Email`, `Phone`, `Url`, `Name`, `OneTimeCode`), `CssLength? Width` |
| **Blazor approach** | `InputBase<string?>` rendering a plain `<input>`. Giving a `Label` is what decides the shape: without one it is bare, carrying `.forma-text-box`; with one it renders that same input inside `FormaField` wearing `.forma-field-input`, so the label, the box and the validation slot come from the one component `FormaPasswordBox` and `FormaTextArea` also use rather than from a second set of rules imitating it. The only JS is the shared autofill detection, initialised solely when labelled since it exists to keep that floating label floated |
| **Types** | `Type` is a closed set covering what a text box draws itself: plain text, email, telephone and web address. A date, a number or a colour belongs to the component built for it, where the browser supplies the editing surface. Blazor matches parameters case-insensitively, so a raw `type` attribute binds to this parameter rather than reaching the element, and an unsupported type fails loudly instead of being silently dropped |
| **Notes** | Bare, it carries no label, so a caller names it through `Id` and a real `<label for>`, or through `aria-label` where no visible label belongs. `Placeholder` gives way to the floating label once one is set, since the two would otherwise sit on top of each other. `Width` sizes the field wrapper when labelled and the input itself when bare, because the wrapper is what occupies the row. The invalid border does not wait for `modified`, since submitting an untouched required field marks it invalid without ever marking it modified |

### 60. `FormaSearchBox` ✅

| | |
|---|---|
| **Old pattern** | The hand-rolled search box in `FormaExplorerAddressBar` — a magnifying glass glyph, a `type="search"` input, and a clear button, all written directly into that component |
| **Used in** | General search and filter fields anywhere a caller wants a `type="search"` box rather than a plain `FormaTextBox`. Now the box `FormaExplorerAddressBar` composes rather than draws itself |
| **Parameters** | `string Value`, `EventCallback<string> ValueChanged`, `EventCallback<string> OnSearch`, `SearchBoxOptions Options` (debounce delay validated ≥ 0, placeholder, disabled), `string? Id`, `string? Class`, plus captured pass-through attributes |
| **Blazor approach** | A plain `Value`/`ValueChanged` pair rather than `InputBase<T>`, since a search field has no validation state to carry. `Value` updates on every keystroke; `OnSearch` reports the committed term — instantly when `Options.DebounceDelay` is zero, or once typing pauses for that long otherwise. Enter and the built-in clear button both bypass a pending debounce and report immediately. The clear button is `FormaCloseButton`, reused rather than redrawn, and disables alongside the field |
| **Notes** | `dir="auto"` follows what is typed, with the placeholder falling back to the page's own direction so an RTL placeholder does not strand itself in an empty LTR-default field. Retrofitted into `FormaExplorerAddressBar`, which keeps its own wrapper for the expand-on-focus and breadcrumb-collapse behaviour and now leaves the box itself to this component |

### 61. `FormaBreadcrumb` ✅

| | |
|---|---|
| **Old pattern** | The hand-rolled breadcrumb section of `FormaExplorerAddressBar` — root icon, chevron separators, a per-segment dropdown of alternatives, and drop-target handlers, all written directly into that component. `FormaPageTitle`'s own plain `<ol>` of links predates it too |
| **Used in** | Any trail of segments: a page header's site navigation as well as an explorer's folder path. `FormaExplorerAddressBar` and `FormaPageTitle` both compose it now rather than drawing their own |
| **Parameters** | `IReadOnlyList<BreadcrumbItem<TItem>> Items`, `Icon? RootIcon`, `string? AriaLabel`, `EventCallback<TItem?> OnItemClick`, `EventCallback<TItem?> OnItemDrop`. `BreadcrumbItem<TItem>` carries `Label`, `Url?`, `Value?`, `IsCurrent`, and `Children?` for a segment's own dropdown |
| **Blazor approach** | Generic over the value a click or drop reports back, the way `FormaDataGrid<TItem>` and `FormaSortableList<TItem>` already are, with a non-generic marker type so `IStringLocale` resolves independently of `TItem`. A segment with a `Url` renders a real `<a>`; one without renders a `<button>` when `OnItemClick` is wired, or falls back to static text when it is not — so a plain site breadcrumb never gains a false clickable affordance it does nothing with. A chevron beside a segment with `Children` opens a menu of alternatives, closing on a second click, a backdrop click, or Escape |
| **Notes** | `aria-current="page"` is set from the caller-supplied `IsCurrent` rather than inferred from position, since a defensive comparison against the current value is more robust than assuming the last segment is always current. `FormaPageTitle` derives it itself — only the last crumb lacking a `Url` counts as current, not every non-linked one — which also means an intermediate category label no longer looks identical to the current page, since only the true current segment is bold. Chevron separators mirror through `:dir(rtl)`, matching the rest of the library's glyph-mirroring convention |

### 62. `FormaCommandArea` / `FormaCommandBar` ✅

| | |
|---|---|
| **Old pattern** | The header region of `FormaExplorer` — the surface, the rule beneath it and the stacked bars — written into the explorer's own stylesheet |
| **Used in** | The region above any page's content: an explorer header, a list page's action strip, an editor's publish bar. `FormaExplorer` composes it for its own header |
| **Parameters** | `FormaCommandArea`: `RenderFragment? ChildContent`, `string? Class`, plus captured pass-through attributes. `FormaCommandBar`: `RenderFragment? ChildContent`, `RenderFragment? TrailingContent`, `CommandBarOptions Options` (`PageOnWheelGesture`, `ScrollBackLabel`, `ScrollForwardLabel`), `bool Hidden`, `bool Animated`, `string? Class`, plus captured pass-through attributes |
| **Blazor approach** | A pair, split by what each owns. The area is the surface and the stack — a flex column carrying the background, the rule and the padding, holding as many bars as a caller writes into it. Each bar lays its commands on a horizontally scrolling track and, once they outgrow the width, flanks them with an arrow at either end, each disabled at the edge it can no longer travel toward; `TrailingContent` pins content such as an overflow menu past the track, outside the scrolling region. `Hidden`/`Animated` forward straight to a wrapping `FormaVisibility`, so a favorites or plugin bar shown and hidden on demand keeps its scroll position and never has its own track measured mid-collapse. Retune with `--forma-command-area-padding` and `--forma-command-area-background-color` |
| **Why it needs JS** | Whether a bar overflows is a measurement only the browser holds, so a colocated module reports it back rather than C# guessing. A `ResizeObserver` watches the track and each command, and a `MutationObserver` watches the track's child list — **commands can be added or removed without the track's own box changing, which no `ResizeObserver` reports**, so watching size alone leaves the arrows stale the moment a caller's commands change. Reports are coalesced to one a frame and sent only when the answer changes, so scrolling never floods the circuit. Adding the arrows narrows the track, which re-triggers the observer — the loop settles because the second measurement changes nothing |
| **Notes** | **It dictates where things go, never what they are.** No selection state, no bulk actions, no count readout — an explorer's select-mode bar stays the explorer's business, and the area itself owns no strings at all. Scroll offsets are read through `Math.abs` because a right-to-left bar counts `scrollLeft` downwards from zero, and the chevrons are turned per direction since they are not bidi-mirrored. Reached from a host's stylesheet through `::deep`, since the element belongs to this component's scope rather than the caller's |

### 63. `FormaSplitter` ✅

| | |
|---|---|
| **Old pattern** | Two hand-rolled sashes in the explorer — the vertical one resizing the folders panel and the horizontal one resizing the folder list — each with its own pointer plumbing, key handling and clamping |
| **Used in** | Any pair of panes a reader can resize. `FormaExplorer` composes it for its sidebar splitter and `FormaExplorerSidebar` for its tree sash, which is what proved both orientations |
| **Parameters** | `double Value`, `EventCallback<double> ValueChanged`, `double Min`, `double Max`, `double Step` (default `1`), `double PixelsPerUnit` (default `16`), `SplitterOrientation Orientation`, `string Label` (required), `EventCallback<bool> OnResizingChanged`, `string? Class`, plus captured pass-through attributes |
| **Blazor approach** | The divider owns the interaction and the clamping; where the size is stored and how it is applied stays the caller's. `role="separator"` with `aria-orientation`, `aria-valuenow`, `aria-valuemin` and `aria-valuemax`, reachable by tab and driven by the arrow keys, `Home` and `End`. Pointer capture through a colocated module keeps the drag alive once the pointer leaves the divider's narrow hit area, and `OnResizingChanged` lets a host suppress text selection for the length of a drag |
| **Units** | `PixelsPerUnit` converts a pointer's pixel travel into the caller's own unit, defaulting to `16` so a host sizing panes in rem needs no arithmetic of its own; a host working in pixels sets it to `1`. `Step` decides how far one arrow key travels, for a pane whose useful sizes are further apart than a single unit |
| **Notes** | `aria-orientation` describes **the bar, not the axis it moves along** — a `Vertical` splitter is a vertical bar between side-by-side panes, dragged horizontally. Only the horizontal arrows resolve through `IsRightToLeft`, since a right-to-left layout puts the leading pane on the other side; `ArrowUp`, `ArrowDown`, `Home` and `End` are left alone. Focus thickens the grip rather than drawing an outline, which on something this thin renders as stray parallel lines, and the ring returns under forced colours where the fill is dropped |

### 64. `FormaPageHead` ✅

| | |
|---|---|
| **Old pattern** | Each host hand-writing the same `<head>`: charset, viewport, base address, and Forma's stylesheets linked one by one in an order nothing enforced |
| **Used in** | Every host's `App.razor`. The Showcase composes it, and any consuming application does the same |
| **Parameters** | `string Title` (required, validated), `string BaseHref` (default `/`, validated), `bool PreloadAssets` (default `true`), `RenderFragment? ChildContent` |
| **Blazor approach** | Plain markup into the document head, no interactivity. `@Assets` resolves each stylesheet, which fingerprints correctly from inside the library because the running host's asset manifest is what answers, not the assembly the markup was compiled into. `ChildContent` renders after Forma's stylesheets so a host's own land later and win the cascade, and the framework's `ResourcePreloader`, `ImportMap` and `HeadOutlet` close the head so no host repeats them |
| **Ordering** | `HeadOutlet` renders last, so a page's own `PageTitle` and `HeadContent` override the title set above it — a host putting the outlet first would silently break `PageTitle`, which is the ordering this component exists to own. `ImportMap` matters to the library directly, since it is what fingerprints Forma's own colocated JS modules |
| **Notes** | `PreloadAssets` covers the framework's `ResourcePreloader`, which only a WebAssembly render mode benefits from and which resolves an internal service that only the Razor Components hosting stack registers; it is switched off in bUnit, where that stack does not exist, and exercised through the Showcase instead. Not the page's visible title bar — that is `FormaPageTitle`, which took this name's place in `Layout` |

### 65. `FormaToolbar` ✅

| | |
|---|---|
| **Old pattern** | None — a new, generic addition rather than a Coi-derived one, extracted from `FormaComposer`'s own toolbar to give any host a grouped, item-driven command bar without pulling in the composer |
| **Used in** | Anywhere a host needs a bar of commands driven by data rather than hand-written markup. Already the base for `FormaSearchToolbar`, `FormaSelectionToolbar`, and `FormaStatusStrip`; planned as the base for `FormaComposer`'s own toolbar |
| **Parameters** | `string Label` (required, validated), `IReadOnlyList<FormaToolbarItem> Items`, `bool Disabled`, `bool ShowLabels`, `ElementReference? Target`, `EventCallback<string> OnCommand`, `RenderFragment? TrailingContent`, `bool Scrollable`, `CommandBarOptions CommandBarOptions` (forwarded to the track), `bool Hidden`, `bool Animated`, `string? Class` |
| **Item model** | `FormaToolbarItem` is abstract — `Id`, `Title`, `Tooltip?`, `Icon?`, `IsDisabled` — and the subtype decides the rendering. `FormaToolbarGroup` adds `Items`; `FormaToolbarAction` adds `Command`, `ShortcutDisplay?`, `ShortcutAriaKeys?` and is specialized by `FormaToolbarCommand` (fires once) and `FormaToolbarToggle` (adds `IsActive`); `FormaToolbarMenu` adds `Options`; `FormaToolbarReadout` adds `Value` and `IsAnnounced`; `FormaToolbarCustomItem` adds `Component` and `Parameters?`. `FormaToolbarOption`: `Id`, `Title`, `Command`, `IsActive`, `PreviewStyle?`, `Swatch?` |
| **Blazor approach** | Every action renders through `FormaToggleButton`, a ghost `FormaButton` that adds only what a toolbar command needs — a glyph, a label shown or read aloud, and a toggle's pressed state — so hover, disabled, focus and busy come from the one button and the toolbar keeps no button styling of its own; the explorer's view switch and the composer's selection bar compose the same button. `Scrollable` decides the root. When set, the items go on a `FormaCommandBar` rather than reinventing its scrolling track and overflow arrows; when not, the toolbar renders its own `.forma-toolbar` row and the items wrap instead, so a toolbar that cannot overflow carries none of the bar's observers or interop. Either way the root carries `role="toolbar"` and the label. A `switch` over the item type does the dispatch, so each kind renders its own way: a group becomes a `role="group"` cluster named by its `Title` and separated from its neighbours by a rule, a menu becomes a `FormaDropDownMenu` showing the active option's title or a colour swatch, a readout becomes a plain span that gains `role="status"` when `IsAnnounced`, and a custom item becomes a `DynamicComponent`. A toggle's `IsActive` is drawn with an underline as well as colour so it never rests on colour alone. `ShowLabels` puts an icon and its label in the same button and gives it real chrome; an icon-only button stays flat so a dense row of glyphs is not all borders, and only then does it need an `aria-label`. An action's `ShortcutAriaKeys`, such as "Control+Shift+B", is parsed into a `KeyBinding` and attached through the shared `KeyBindings` JS behaviour once `Target` is set — shortcuts stay inert until then, since the toolbar never listens document-wide. A matched shortcut runs through the same `HandleCommandAsync` a click does, and is refused when the toolbar's own `Disabled` or the specific action's `IsDisabled` says so |
| **Notes** | Making the group an item is what lets one `Items` list mix bare items and clusters, and lets a caller with nothing to group skip the ceremony — a bare item renders in a `display: contents` wrapper so it joins the row without inheriting a separator. Groups do not nest, and the guard says so rather than rendering something surprising. Deliberately holds no plugin registry, no multi-row layout engine, and no undo/redo special-case — those stay `FormaComposer`'s own concern. Titles are caller-supplied and rendered as given, so the host localizes them and the component owns no strings at all. A tooltip that would only repeat a visible label is dropped, so hovering a named button does not echo it back. Each item and option is keyed by its `Id` and each group by its own, so a host adding or removing one never tears down a neighbour's open menu — keying a group by its first item's `Id` instead would, since that key moves the moment that item leaves. `IsDisabled` lets one item stand down without disabling the whole toolbar, for a command that needs a selection or a state the others don't. The collapse sits once, on whichever element is actually the root: forwarded to `FormaCommandBar` when scrolling, and owned through the toolbar's own `FormaVisibility` when not — without that second path `Hidden` would silently do nothing on a toolbar that has no bar to forward to. `Hidden` also detaches the `KeyBindings` shortcut listener even while `Target` stays set, since the toolbar going invisible must stop its shortcuts firing regardless of what element still holds focus |
| **Outstanding** | Sets `role="toolbar"` without the arrow-key roving focus an ARIA toolbar owes; the roles its children carry are correct, but keyboard navigation between them is still plain tab order |

### 66. `FormaStatusStrip` ✅

| | |
|---|---|
| **Old pattern** | None — extracted from `FormaComposerStatusStrip`'s shape into a generic component, so any host can show a quiet strip of readouts and commands without pulling in the composer. `FormaComposerStatusStrip` still exists and is not yet migrated onto it |
| **Used in** | Planned to replace `FormaComposerStatusStrip` |
| **Parameters** | `string? Label` (required once `Items` is non-empty), `IReadOnlyList<FormaToolbarItem> Items`, `bool Disabled`, `StatusStripEmptyMode EmptyMode` (`Hidden`/`Shown`, default `Hidden`), `bool Hidden`, `bool Animated`, `EventCallback<string> OnCommand` |
| **Blazor approach** | A thin wrapper around `FormaToolbar`: a `FormaToolbarReadout` for a value, a `FormaToolbarCommand` for something the strip lets a host trigger. `FormaToolbar` already gives a readout `role="status"`/`aria-live="polite"` when its `IsAnnounced` is set, so the strip needs no announcer of its own the way `FormaComposerStatusStrip` built by hand. `EmptyMode` decides whether an empty `Items` renders nothing at all (`Hidden`, the default) or keeps the strip's chrome in the layout as an empty bar (`Shown`); either way, no nested `FormaToolbar` is rendered while there is nothing for it to show, so `Label` stays optional in that case |
| **Notes** | `Disabled` forwards straight to the inner `FormaToolbar`, unlike `FormaSearchToolbar` and `FormaSelectionToolbar`, which have no such state to disable. `Hidden` is independent of `EmptyMode` — it collapses the strip even while it holds items, keeping it (and its inner `FormaToolbar`) mounted rather than empty; `EmptyMode` only governs what happens when there is genuinely nothing to show. The strip's own `FormaVisibility` already collapses everything inside it, so `Hidden` and `Animated` are deliberately *not* passed down to that toolbar as well — doing so nests a second collapse and runs two transitions over the same content |

### 67. `FormaVisibility` ✅

| | |
|---|---|
| **Old pattern** | None — extracted after `FormaCommandBar` and `FormaStatusStrip` each grew their own `Hidden`/`Animated` and hand-rolled an identical `grid-template-rows: 0fr → 1fr` collapse, down to the same three CSS rules copy-pasted under a different class prefix each time |
| **Used in** | `FormaCommandBar` and `FormaStatusStrip` wrap their own chrome in it. `FormaToolbar` reaches it only indirectly, forwarding `Hidden`/`Animated` to the `FormaCommandBar` it is built on rather than adding a second wrapper of its own |
| **Parameters** | `RenderFragment ChildContent` (required), `bool Hidden`, `bool Animated` |
| **Blazor approach** | Collapses `ChildContent`'s block size to zero through `grid-template-rows: 0fr` → `1fr` plus `opacity`, rather than removing it, and marks it `inert` while `Hidden` — so a caller's JS-attached state (a scroll-boundary watcher, an attached keyboard shortcut) survives being hidden and shown again instead of tearing down and reattaching. `Animated` alone adds the `transition`; without it the same mechanism just snaps instantly, needing no separate code path. An `overflow: hidden` inner wrapper is what makes the collapsing row actually clip content of unknown height |
| **Notes** | Chosen over `[hidden]` specifically because the native attribute forces `display: none` in the UA stylesheet, which cannot be transitioned to or from. Exists so this mechanism lives in one file the way `FormaField` centralises label chrome — without it, every collapsible bar reimplements the same four rules under its own class prefix and they drift apart. Has no Showcase page of its own; it is exercised through the pages of the components that compose it |

---

### 68. `FormaShareMenu` ✅

| | |
|---|---|
| **Old pattern** | None — loose Download/Print/Copy buttons scattered per surface, and no sharing at all |
| **Used in** | Anywhere a subject leaves the application: an article shared to a network, a file downloaded, a document printed. Planned over the composer's Preview and Source outputs and `FormaExplorer`'s download actions |
| **Parameters** | `ShareItem? Item` (its `ShareItemKind` — link, file, image or contact — name, meta, icon, the address it is shared as, and the `ResourceInfo` it downloads as), `IReadOnlyList<ShareAction> Actions` (consumer actions: id, caller-localized title, icon, command, primary or not), `EventCallback<string> OnCommand`, `RenderFragment? Trigger` (default is the share glyph and label), `ButtonOptions TriggerOptions`, pass-through attributes for the trigger |
| **Blazor approach** | Composes `FormaDropDownMenu` wholesale — trigger, anchored popup, backdrop, keyboard and focus all come from it — and adds only what a share surface means: an item header; a networks row riding a `FormaCommandBar` track so it scrolls with arrows rather than wrapping or widening the menu, carrying the link networks, the native Share hand-off and every registered destination; the built-in commands — Download, Email, Copy Link, Print — standing in the open beneath it; and consumer commands alone behind More. Every entry on the networks row is an `IShareTarget` resolved from DI — nothing is hardcoded in the component. The baseline networks (Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, Pinterest) ship as navigation destinations in `Forma.AddOns.Share.Targets`, registered by `AddFormaShareNetworks()`, which a host calls beside `AddFormaUI()` since the component library references only the share contracts; a destination answering an address renders as a link opening the network's composer in a new tab, and one without posts through its API when chosen. Kinds still decide what applies: an image adds Pinterest, pointed at its media; a file or contact gets no network row at all, a file keeping Download and Copy Link and a contact keeping email and its card; a `ResourceInfo` yields Download as a plain download link, saved from its source under its name, the same way `FormaFileViewer` offers a file — in-memory content is deliberately not modeled, since a consumer holding content adds its own action for it. Where the browser exposes a native share surface, a Share entry hands the subject to the platform's own sheet — the OS sheet answers "where does this go", this menu answers "what can the app do", so the two never compete. Consumer actions are data, matching the toolbar's model, so a composer plugin can contribute without markup; primary ones join the row and the rest fall behind More, whose section collapses through `FormaVisibility` |
| **Notes** | Ships no strings a caller must localize: baseline labels are the component's own resources, network names are proper nouns, and action titles arrive caller-localized like `FormaToolbar` titles. The primary row's items are still `FormaDropDownMenuItem`s, restyled into columns, so they register with the menu's arrow-key navigation like every other row. Networks needing per-user configuration to share at all (Mastodon's instance) are deliberately absent from the baseline, and so are apps with no public web share endpoint (Instagram, TikTok whose Share Kit needs a client key, Discord, Messenger whose dialog needs an app id) — those are reachable only through the native Share hand-off, where the platform lists them itself. LinkedIn's share-offsite endpoint works, but its mark is no longer in Simple Icons, so it waits on lawful artwork. A host sends items elsewhere — a cloud drive, a chat channel, an internal vault — by registering `IShareTarget` implementations, each declaring what it accepts and joining the row for those items alone. The share contracts live in `Forma.Framework.Share` and the destinations in `Forma.AddOns.Share.Targets`, which ships API destinations out of the box, each a plain HTTP integration switched on by a registrar: Discord (`AddFormaDiscordSharing`, a channel webhook, no OAuth), Dropbox (`AddFormaDropboxSharing`, the save_url API), Google Drive and OneDrive (`AddFormaGoogleDriveSharing` / `AddFormaOneDriveSharing`, uploads under a host-supplied access token) and TikTok (`AddFormaTikTokSharing`, publish-by-pull). Skype ships nothing because Microsoft retired it — its share endpoint now redirects to Teams |

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
| **Phase 1** | `FormaDialog` ✅, `FormaConfirmDialog` ✅, `FormaToast` ✅ | Infrastructure — everything depends on dialogs and notifications |
| **Phase 2** | `FormaPageTitle` ✅, `FormaSearchToolbar` ✅, `FormaTabs` ✅ | Layout — the shell of a page |
| **Phase 3** | `FormaInput` ✅, `FormaToggleSwitch` ✅, `FormaPasswordBox` ✅, `FormaCurrencyInput`, `FormaSlider` ✅, `FormaDatePicker` ✅, `FormaSelect` ✅ | Form inputs — needed before building any edit forms |
| **Phase 4** | `FormaDataGrid<TItem>` ✅, `FormaSelectionToolbar` ✅, `FormaStatusBadge` ✅, `FormaCapacityIndicator`, `FormaPagination` ✅ | Data display — needed for list pages |
| **Phase 5** | `FormaSortableList<TItem>` ✅, `FormaMultiSelect`  ✅, `FormaCategorySelector`, `FormaDropDownMenu` ✅, `FormaColorPicker` ✅ | Interactive widgets — needed for editor pages |
| **Phase 6** | `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built |
| **Phase 7** | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors — built alongside their specific pages |
| **Phase 8** | `FormaButton` ✅ | Shared button primitive — built after the fact and retrofitted onto `FormaDataGrid`, `FormaDialog`, `FormaConfirmDialog`, `FormaPagination`, `FormaDatePicker` as the single source of truth for buttons |
| **Phase 9** | `FormaCheckBox` ✅ | Shared checkbox primitive — built after the fact and retrofitted onto `FormaDataGrid` and `FormaMultiSelect` as the single source of truth for checkboxes |
| **Phase 10** | `FormaCloseButton` ✅ | Shared dismiss primitive — built after the fact and retrofitted onto `FormaDialog`, `FormaToastContainer` and `FormaSelectionToolbar` as the single source of truth for the `×` control |
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
