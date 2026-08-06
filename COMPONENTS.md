# Component Inventory

> Identified from the old Coi admin codebase, which is where the component set was derived
> from — the library itself is general-purpose. Each component maps an old Web Forms + jQuery
> pattern to a Blazor component using **only** built-in Blazor components, native HTML elements,
> and browser APIs — no external JS libraries.
>
> Target location: `Forma.Blazor/Components/`
>
> **Naming convention:** All components use the `Forma` prefix for their tag names (e.g., `<FormaModal>`,
> `<FormaTabs>`) to avoid clashes with third-party libraries. CSS classes use the `forma-` prefix.
> C# types (enums, services) rely on namespaces for disambiguation and don't carry the prefix.

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
| **Parameters** | `RenderFragment ChildContent` containing `FormaTab` children. Each `FormaTab` has `string Title` (required, validated), `FormaIcon? Icon` (typed icon via `FormaIcon` record struct), `RenderFragment ChildContent` |
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
| **Blazor approach** | Built on `FormaModal` with explicit confirm/cancel actions supplied by the confirm component (modal default actions disabled for this case). Uses `IConfirmDialogService : INotifyStateChanged` (in `Forma.Blazor.Abstractions`) / `ConfirmDialogService` (in `Forma.Blazor.Core`), registered via DI, returning `Task<bool>`. Service composes `StateNotifier` for async subscriber notification. Sealed `IDisposable` component with code-behind in `.razor.cs` |

### 6. `FormaToast` ✅

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |
| **Parameters** | Service-driven (no public component parameters). Toast requests sent via `IToastService.ShowAsync(string message, ToastType type = Success, TimeSpan? duration = null)` (default 3s). `ToastType` enum: `Success`, `Error`, `Info`, `Warning`. `IToastService` also exposes `IReadOnlyList<ToastMessage> Toasts`, `Task RemoveAsync(string id)`. Arguments validated (message non-empty, duration non-negative, id non-empty). |
| **Blazor approach** | `IToastService : INotifyStateChanged` (in `Forma.Blazor.Abstractions`) / `ToastService` (in `Forma.Blazor.Core`), registered via DI. Service composes `StateNotifier` for async `Func<Task>` subscriber notification. A `FormaToastContainer` (sealed `IDisposable`, code-behind in `.razor.cs`) component at layout level subscribes and renders active toasts. Auto-dismissal via `System.Threading.Timer` per toast. CSS `@keyframes` slide-in animation, type-based color variants. No JS needed |

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
| **Blazor approach** | Wraps `InputText` (or `InputNumber`/`InputDate` via generic `TValue`). Label animation via CSS `:focus-within` + `:placeholder-shown` pseudo-classes. Integrates with `EditForm` validation via `ValidationMessage`. Multiline is a planned mode of this component (`Rows` → renders `<textarea>`) rather than a separate `FormaTextArea` |

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
| **Popup placement** | The calendar is a `popover`, so it renders in the **top layer** and no scrolling or clipping ancestor can cut it off — it works inside a grid's scroll region, a modal body, or any `overflow: auto` box. The top layer keeps no layout tie to the trigger, so the shared `wwwroot/scripts/AnchoredPopup.js` module measures and applies the position: it hangs from the trigger's inline-start edge (mirrored in RTL), flips above when there is no room below, clamps into the viewport, and follows scroll and resize. One positioning path for every browser rather than a CSS/JS split. That module is deliberately component-agnostic — `FormaSelect`, `FormaMultiSelectDropDown` and `FormaDropDownMenu` are the same trigger-plus-anchored-panel shape and should import it rather than re-solve the clipping. `popover="manual"` keeps the existing backdrop as the dismissal mechanism, which also sidesteps the iOS 17–18.2 bug where a popover ignores a tap outside it |
| **Blazor approach** | Use Blazor `InputDate` which renders native `<input type="date">`. Browser provides locale-aware calendar. For reset, add a clear button. For min-date, use `min` HTML attribute |

### 16. `FormaRangeSlider`

| | |
|---|---|
| **Old pattern** | `<input type="range">` + hidden field + live value display label |
| **Used in** | forms (border radius), slider (duration), design/edit (font size, site width) |
| **Parameters** | `@bind-Value` (int/double), `int Min`, `int Max`, `int Step = 1`, `string? Label`, `string? Unit` ("px", "%", "s") |
| **Blazor approach** | Wraps `InputNumber` rendered as `type="range"`. Display current value next to the slider. Pure HTML + Blazor binding |

### 17. `FormaPasswordInput`

| | |
|---|---|
| **Old pattern** | `<asp:TextBox TextMode="Password">` + `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` toggles between password/text |
| **Used in** | user editor, settings (API keys), admin login |
| **Parameters** | `@bind-Value`, `string Label`, `bool ShowToggle = true` |
| **Blazor approach** | `InputText` with dynamic `type` attribute (password/text) toggled by button click in C#. No JS needed |

### 18. `FormaCurrencyInput`

| | |
|---|---|
| **Old pattern** | `.form-addon` wrapping input with currency symbol overlay |
| **Used in** | product (price, sale price, cost), store-settings (min order, shipping), coupons (discount amount) |
| **Parameters** | `@bind-Value` (decimal), `string Symbol = "₪"`, `string? Label` |
| **Blazor approach** | `InputNumber` inside a flex container with the currency symbol as an adjacent `<span>`. CSS for visual grouping |

### 19. `FormaMultiSelectDropDown` ✅

| | |
|---|---|
| **Old pattern** | `.multi-input` with `.tags` + `.multi-dropdown` list. `$('#el').multi()` plugin. Tags display selected items, click X to remove. Dropdown with checkboxes. Optional "add new" button |
| **Used in** | page editor (categories), product editor (categories), coupons (categories) |
| **Parameters** | `@bind-Value` (List<int>), `IEnumerable<SelectOption> Options`, `string Placeholder`, `bool AllowAdd = false`, `EventCallback<string> OnAddNew` |
| **Blazor approach** | Interactive component. Clicked → show dropdown with checkboxes. Selected items shown as removable tags above/inside the input area. Search/filter within dropdown. No external JS |

### 20. `FormaSelect`

| | |
|---|---|
| **Old pattern** | JS replaces native `<select>` with styled div: `.custom-select > .cs-val` (display) + `.cs-options > .cs-option` (dropdown). Click to open, click option to select, click outside to close |
| **Used in** | Multiple admin pages for styled dropdowns |
| **Parameters** | `@bind-Value`, `IEnumerable<SelectOption> Options`, `string? Placeholder` |
| **Blazor approach** | A `<button>` trigger plus an anchored option list, sharing `AnchoredPopup.js` with `FormaDatePicker` and `FormaMultiSelectDropDown` so the list escapes any clipping ancestor. Native `InputSelect` is the fallback where a plain select is acceptable, but its list is OS-rendered and cannot be themed to match the rest of the form vocabulary |

---

## Tier 4 — Interactive Widgets

### 21. `FormaSortableList<TItem>` ✅

| | |
|---|---|
| **Old pattern** | jQuery Dragsort / HTML5 sortable — drag items to reorder. On drop, serialize new order and POST to `sorting.asmx/Sort`. Used for menus, galleries, sliders, categories, product options, delivery methods, form fields, filters |
| **Used in** | menu sort page, gallery module, slider editor, category module, product options, store-settings delivery, contact form builder, filters editor |
| **Parameters** | `@bind-Items` (List<TItem>), `RenderFragment<TItem> ItemTemplate`, `string? DragHandleClass`, `EventCallback<List<TItem>> OnReorder` |
| **Blazor approach** | Uses native HTML5 Drag and Drop API (`draggable`, `@ondragstart`, `@ondragover`, `@ondrop`). Track drag source/target index, reorder the list in C#, re-render. Minimal JS interop (only if `preventDefault` is needed on `dragover`) |

### 22. `FormaImagePicker`

| | |
|---|---|
| **Old pattern** | `OpenImgLibrary()` → opens `pops/images.aspx` in `ModalFrame`. Full image library: folder tree, grid of thumbnails, multi-select, upload, move, delete, edit, capacity indicator. Returns selected image URL(s) to parent |
| **Used in** | page editor (cover image, OG image), product editor (images, gallery), design (logo, bg, top image), slider (slide bg, elements) |
| **Parameters** | `int MaxSelection = 0` (0 = unlimited), `EventCallback<List<string>> OnImagesSelected` |
| **Blazor approach** | A thin image-specific skin over `FormaFilePicker` (#35): pins the accept filter to images, renders thumbnail previews, and projects the picker's selection back as image paths. Carries no library, modal, upload or paging logic of its own |

### 23. `FormaCategorySelector`

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |
| **Parameters** | `@bind-SelectedIds` (List<int>), `IEnumerable<Category> AvailableCategories` |
| **Blazor approach** | Two `<ul>` lists side by side. Click item → move between lists (C# list manipulation + re-render). CSS animation via `transition` on list items. No external JS |

### 24. `FormaDropDownMenu`

| | |
|---|---|
| **Old pattern** | `Info('divId', '#buttonId', 's')` → shows a `dropdown-menu dropdown-menu-arrow` positioned below a button. Click outside to close |
| **Used in** | order detail (options: print, notes, invoice), and various action menus |
| **Parameters** | `RenderFragment Trigger` (the button), `RenderFragment ChildContent` (menu items) |
| **Blazor approach** | Button + absolutely positioned `<div>`. Toggle visibility on click. Close on `@onfocusout` or click-outside via minimal JS interop. Native `<details>`/`<summary>` is another option |

---

## Tier 5 — Specialized / Page-Specific Components

These are more complex, possibly page-specific, but still benefit from being components.

### 25. `FormaNotesEditor`

| | |
|---|---|
| **Old pattern** | Modal with `<textarea>` for admin notes on orders/leads, save button, timestamp display |
| **Used in** | order detail, lead detail |
| **Parameters** | `string? Notes`, `EventCallback<string> OnSave` |
| **Blazor approach** | `FormaModal` containing `InputTextArea` + save button. Simple composition |

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
| **Old pattern** | Click export → server generates Excel file → client polls with `DownloadTimer` until file ready → `window.location` to download URL |
| **Used in** | orders, leads |
| **Parameters** | `string ExportUrl`, `string Label = "ייצוא"` |
| **Blazor approach** | Button triggers server-side export. Use `NavigationManager` or JS interop `window.location.href` to initiate download. Consider `StreamRendering` for progress indication |

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

### 33. `FormaCheckbox` ✅

| | |
|---|---|
| **Old pattern** | Plain `<input type="checkbox">` styled by `fa-3.6.5.css` — no shared component, each admin page restated its own markup |
| **Used in** | Single source of truth for checkboxes across the library — the retrofit target for `FormaDataGrid`'s header/row selection checkboxes and `FormaMultiSelectDropDown`'s option checkboxes |
| **Parameters** | `bool Checked`, `EventCallback<bool> CheckedChanged`, `bool Indeterminate`, `bool Disabled`, `bool PreventDefault`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | Plain `<input type="checkbox">`, not `InputBase<bool>` — callers that need form integration wrap it themselves. `Indeterminate` is IDL-only with no content attribute, so it is applied through a small JS interop module, and only when a checkbox actually becomes indeterminate. `PreventDefault` suppresses the native toggle so a caller that computes its own next state — `FormaDataGrid`'s tri-state select-all, which can reject a click outright — owns the checked state entirely; `OnClick` stays a plain notification that changes no behaviour on its own. Sealed component with code-behind in `.razor.cs` |

### 34. `FormaUploader`

| | |
|---|---|
| **Old pattern** | The upload panel inside `pops/images.aspx` — a file input plus a progress bar posting to a handler that writes into the media folder |
| **Used in** | the media library, and any editor that takes a file directly rather than through the library |
| **Parameters** | `EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected`, `string? Accept`, `bool Multiple`, `long MaxFileSize`, `int MaxFiles` |
| **Blazor approach** | `InputFile` behind a styled drop zone. The DOM only exposes drag-and-drop of files through its own `drop` event, so a small module supplies the dragover/drop plumbing; per-file progress comes from reading `IBrowserFile.OpenReadStream` in chunks. Size, count and type are validated in the component, which reports rejects rather than throwing. Owns no storage — it hands `IBrowserFile`s to the host |

### 35. `FormaFilePicker`

| | |
|---|---|
| **Old pattern** | `OpenImgLibrary()` → `pops/images.aspx` in `ModalFrame`: folder tree, grid of thumbnails, multi-select, upload, move, delete, capacity indicator |
| **Used in** | the base for every "choose something from the media library" flow |
| **Parameters** | `IReadOnlyList<MediaEntry> Entries`, `string? Accept`, `int MaxSelection = 0` (0 = unlimited), `EventCallback<IReadOnlyList<MediaEntry>> OnSelected`, folder navigation callbacks |
| **Blazor approach** | `FormaModal` (size Full) hosting a folder breadcrumb, a selectable entry grid and `FormaPagination`, with `FormaUploader` in its toolbar. Type-agnostic — it renders whatever preview a `MediaEntry` declares, so images, video and documents all work through one component. The host supplies the entries and performs the storage operations; the picker never touches a filesystem |

### 36. `FormaEditor`

| | |
|---|---|
| **Old pattern** | CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px. Custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`. `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction |
| **Used in** | page editor, product description — and in Épica, the News article body |
| **Toolbar (from Coi's `Website` preset)** | Source · Preview · Templates — Cut/Copy/Paste/PasteText/PasteFromWord · Undo/Redo · SelectAll · RemoveFormat — Smiley · SpecialChar · LineHeight · Maximize — Bold/Italic/Underline/Strike — Numbered & bulleted lists — Justify right/center/left/block · BidiRtl/BidiLtr — Link/Unlink/Anchor — YouTube embed · Image · Table · HorizontalRule — Format · Font · FontSize — TextColor — image library. Subscript/Superscript were explicitly removed |
| **Parameters** | `@bind-Value`, `EditorMode Mode` (Html/Markdown), `EditorToolbar Toolbar` (Full/Basic/Mini, mirroring Coi's presets), `bool AllowSourceView`, `EventCallback OnMediaRequested` |
| **Blazor approach** | The one component where an **external JS editor engine is the right call** — the JS rules exist to keep components small and dependency-free, and a rich text engine is precisely the case they yield to. Everything around the engine stays Forma's: the format/font/size pickers are `FormaSelect`, the toolbar overflow is `FormaDropDownMenu`, colour is `FormaColorPicker`, image insertion delegates to `FormaImagePicker` (never a second file browser), and direct uploads go through `FormaUploader`. Toolbar presets mirror Coi's three, including the sub-600px collapse. **Markdown is a first-class mode**, not an export: `Mode` switches the engine's document model and `AllowSourceView` toggles between the rendered surface and raw HTML/Markdown. Bidi controls and RTL contents direction are required, not optional |

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
| **CKEditor** | Now in scope as `FormaEditor` (#36) rather than excluded — it wraps an external editor engine and surrounds it with Forma's own controls |

---

## Suggested Build Order

Components are ordered by dependency and reuse frequency:

| Phase | Components | Rationale |
|---|---|---|
| **Phase 1** | `FormaModal` ✅, `FormaConfirmDialog` ✅, `FormaToast` ✅ | Infrastructure — everything depends on modals and notifications |
| **Phase 2** | `FormaPageHeader` ✅, `FormaToolbar` ✅, `FormaTabs` ✅ | Layout — the shell of a page |
| **Phase 3** | `FormaInput` ✅, `FormaToggleSwitch` ✅, `FormaPasswordInput`, `FormaCurrencyInput`, `FormaRangeSlider`, `FormaDatePicker`, `FormaSelect` | Form inputs — needed before building any edit forms |
| **Phase 4** | `FormaDataGrid<TItem>` ✅, `FormaContextToolbar` ✅, `FormaStatusBadge` ✅, `FormaCapacityIndicator`, `FormaPagination` ✅ | Data display — needed for list pages |
| **Phase 5** | `FormaSortableList<TItem>` ✅, `FormaMultiSelectDropDown` ✅, `FormaCategorySelector`, `FormaDropDownMenu`, `FormaColorPicker` | Interactive widgets — needed for editor pages |
| **Phase 6** | `FormaUploader`, `FormaFilePicker`, `FormaImagePicker`, `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built. The media three build in that order: uploader, then the picker that hosts it, then the image skin over the picker |
| **Phase 7** | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor`, `FormaEditor` | Complex editors — built alongside their specific pages |
| **Phase 8** | `FormaButton` ✅ | Shared button primitive — built after the fact and retrofitted onto `FormaDataGrid`, `FormaModal`, `FormaConfirmDialog`, `FormaPagination`, `FormaDatePicker` as the single source of truth for buttons |
| **Phase 9** | `FormaCheckbox` ✅ | Shared checkbox primitive — built after the fact and retrofitted onto `FormaDataGrid` and `FormaMultiSelectDropDown` as the single source of truth for checkboxes |

---

## File Structure

```
Forma.Blazor/Components/
├── FormaIcon.cs                          # readonly record struct with FontAwesome()/From() factories
├── Layout/
│   ├── BreadcrumbItem.cs                # record with Label + Url?, constructor-validated
│   ├── FormaPageHeader.razor / .razor.cs
│   ├── FormaToolbar.razor / .razor.cs
│   ├── FormaTabs.razor / .razor.cs
│   └── FormaTab.razor / .razor.cs
├── Feedback/
│   ├── FormaModal.razor / .razor.cs / .razor.js
│   ├── FormaModal.razor.css
│   ├── ModalSize.cs
│   ├── FormaConfirmDialog.razor / .razor.cs
│   ├── FormaConfirmDialog.razor.css
│   ├── FormaToastContainer.razor / .razor.cs
│   └── FormaToastContainer.razor.css
├── Data/
│   ├── FormaDataGrid.razor
│   ├── FormaContextToolbar.razor
│   ├── FormaStatusBadge.razor
│   ├── FormaCapacityIndicator.razor
│   └── FormaPagination.razor
├── Input/
│   ├── FormaButton.razor
│   ├── FormaCheckbox.razor
│   ├── FormaInput.razor
│   ├── FormaToggleSwitch.razor
│   ├── FormaPasswordInput.razor
│   ├── FormaCurrencyInput.razor
│   ├── FormaRangeSlider.razor
│   ├── FormaDatePicker.razor
│   ├── FormaSelect.razor
│   ├── FormaColorPicker.razor
│   ├── FormaUploader.razor
│   └── FormaMultiSelectDropDown.razor
├── Interactive/
│   ├── FormaSortableList.razor
│   ├── FormaCategorySelector.razor
│   ├── FormaDropDownMenu.razor
│   ├── FormaFilePicker.razor
│   └── FormaImagePicker.razor
├── Editors/
│   ├── FormaEditor.razor
│   ├── FormaNotesEditor.razor
│   ├── FormaPermissionMatrix.razor
│   ├── FormaProductOptionsEditor.razor
│   └── FormaFilterEditor.razor
└── Specialized/
    ├── FormaExportButton.razor
    ├── FormaDesignPicker.razor
    └── FormaFontPicker.razor
```

```
Forma.Blazor.Abstractions/
├── Foundation/
│   └── INotifyStateChanged.cs
└── Components/
    └── Feedback/
        ├── IToastService.cs
        ├── IConfirmDialogService.cs
        ├── ToastMessage.cs
        ├── ToastType.cs
        └── ConfirmDialogRequest.cs

Forma.Blazor.Core/
├── Foundation/
│   └── StateNotifier.cs
└── Components/
    └── Feedback/
        ├── ToastService.cs
        └── ConfirmDialogService.cs
```
