# Admin Reusable Components Inventory

> Identified from the old Coi admin codebase. Each component maps an old Web Forms + jQuery
> pattern to a Blazor component using **only** built-in Blazor components, native HTML elements,
> and browser APIs — no external JS libraries.
>
> Target location: `Forma.Blazor/Components/Admin/`
>
> **Naming convention:** All components use the `Forma` prefix for their tag names (e.g., `<FormaModal>`,
> `<FormaTabs>`) to avoid clashes with third-party libraries. CSS classes use the `forma-` prefix.
> C# types (enums, services) rely on namespaces for disambiguation and don't carry the prefix.

---

## Tier 1 — Core Layout & Infrastructure

These components form the shell of every admin page. Build first.

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
| **Parameters** | `bool Opened`, `EventCallback OnClose`, `string? Title`, `ModalSize Size` (Small/Medium/Large/ExtraLarge/Full, default Medium), `RenderFragment? Body`, `RenderFragment? Footer`, `bool CloseOnBackdropClick = true`, `bool ShowConfirmAction = true`, `bool ShowCancelAction = true`, `string? ConfirmText`, `string? CancelText`, `EventCallback OnConfirm`, `EventCallback OnCancel`, `bool CloseOnConfirm = true`, `bool CloseOnCancel = true` |
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

### 7. `FormaDataGrid<TItem>`

| | |
|---|---|
| **Old pattern** | `asp:GridView` with server-side paging, sortable columns, row click-through, checkbox column for mass select, action icon columns (view/edit/delete), status badges, responsive hide classes |
| **Used in** | pages, products, orders, leads, comments, users, coupons (all 7 main list pages) |
| **Parameters** | `IQueryable<TItem> Items`, `RenderFragment Columns`, `bool ShowCheckboxes`, `EventCallback<TItem> OnRowClick`, `PaginationState Pagination` |
| **Columns** | `FormaDataGridColumn<TItem>` — `Title`, `Property`, `Sortable`, `CssClass`, `RenderFragment<TItem> ChildContent` (for custom cell rendering) |
| **Blazor approach** | Extend/wrap `Microsoft.AspNetCore.Components.QuickGrid`. QuickGrid provides sorting, pagination, column templates natively. Add: checkbox column, action column, CSS class system, mobile responsive column hiding |

### 8. `FormaMassActions`

| | |
|---|---|
| **Old pattern** | "Select all" checkbox in header + per-row checkboxes. When items selected, show action bar (delete selected, change status, export). `InitTableEvents()` / `$('.cbx').prop('checked', ...)` |
| **Used in** | pages, products, orders, leads, comments (5 of 7 list pages) |
| **Parameters** | `IReadOnlyList<TItem> SelectedItems`, `RenderFragment ChildContent` (action buttons), `EventCallback OnSelectionChanged` |
| **Blazor approach** | Works with `FormaDataGrid` — the grid tracks selected items in a `HashSet<TItem>`. `FormaMassActions` bar conditionally renders when selection count > 0. Uses CSS transition to slide in/out |

### 9. `FormaStatusBadge`

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

### 11. `FormaPagination`

| | |
|---|---|
| **Old pattern** | Server-rendered page links: `<a class="page">1</a> <a class="page">2</a>...`, custom paging in code-behind with `PageSize`, `PageIndex`, total count |
| **Used in** | All list pages, image library |
| **Parameters** | Works with `PaginationState` from QuickGrid, or standalone: `int TotalItems`, `int PageSize`, `int CurrentPage`, `EventCallback<int> OnPageChanged` |
| **Blazor approach** | QuickGrid has built-in `Paginator`. For standalone use (e.g., image library), create a simple paginator component |

---

## Tier 3 — Form Components

### 12. `FormaFloatingLabelField` ✅

| | |
|---|---|
| **Old pattern** | `<asp:TextBox>` + `<label class="lblForm">` positioned over input, animates up on focus/has-value. Validation error shown below. Optional mandatory indicator `<span class="mand">` |
| **Used in** | category popup, user popup, filters, import, contact module, and all form pages |
| **Parameters** | `@bind-Value`, `string Label`, `bool Required`, `string? Placeholder`, `string Type = "text"` |
| **Blazor approach** | Wraps `InputText` (or `InputNumber`/`InputDate` via generic `TValue`). Label animation via CSS `:focus-within` + `:placeholder-shown` pseudo-classes. Integrates with `EditForm` validation via `ValidationMessage` |

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

### 19. `FormaMultiSelectDropdown`

| | |
|---|---|
| **Old pattern** | `.multi-input` with `.tags` + `.multi-dropdown` list. `$('#el').multi()` plugin. Tags display selected items, click X to remove. Dropdown with checkboxes. Optional "add new" button |
| **Used in** | page editor (categories), product editor (categories), coupons (categories) |
| **Parameters** | `@bind-Value` (List<int>), `IEnumerable<SelectOption> Options`, `string Placeholder`, `bool AllowAdd = false`, `EventCallback<string> OnAddNew` |
| **Blazor approach** | Interactive component. Clicked → show dropdown with checkboxes. Selected items shown as removable tags above/inside the input area. Search/filter within dropdown. No external JS |

### 20. `FormaCustomSelect`

| | |
|---|---|
| **Old pattern** | JS replaces native `<select>` with styled div: `.custom-select > .cs-val` (display) + `.cs-options > .cs-option` (dropdown). Click to open, click option to select, click outside to close |
| **Used in** | Multiple admin pages for styled dropdowns |
| **Parameters** | `@bind-Value`, `IEnumerable<SelectOption> Options`, `string? Placeholder` |
| **Blazor approach** | `InputSelect` with CSS styling. If the native select styling is insufficient, build a custom dropdown component using a `<button>` trigger + absolutely positioned `<div>` list. `@onfocusout` to close |

---

## Tier 4 — Interactive Widgets

### 21. `FormaSortableList<TItem>`

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
| **Blazor approach** | Opens `FormaModal` (size: Full) containing the image library UI. Folder navigation, upload via `InputFile` + `IBrowserFile`, thumbnail grid. The library itself is a standalone page/component. Returns paths to caller via `EventCallback` |

### 23. `FormaCategorySelector`

| | |
|---|---|
| **Old pattern** | `lists-src.js` — two-list transfer UI: "available categories" on left, "selected categories" on right. Click item → animated transfer between lists. Hidden input tracks selected IDs |
| **Used in** | page editor (category.ascx module), product editor |
| **Parameters** | `@bind-SelectedIds` (List<int>), `IEnumerable<Category> AvailableCategories` |
| **Blazor approach** | Two `<ul>` lists side by side. Click item → move between lists (C# list manipulation + re-render). CSS animation via `transition` on list items. No external JS |

### 24. `FormaDropdownMenu`

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
| **Blazor approach** | Interactive card grid. Each card is a filter group with `FormaSortableList` of values inside. Reuses `FormaFloatingLabelField` for inputs |

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
| **CKEditor** | A separate decision — either a lightweight built-in Markdown/HTML editor or integrate a JS-free alternative. Out of scope for shared admin components |

---

## Suggested Build Order

Components are ordered by dependency and reuse frequency:

| Phase | Components | Rationale |
|---|---|---|
| **Phase 1** | `FormaModal` ✅, `FormaConfirmDialog` ✅, `FormaToast` ✅ | Infrastructure — everything depends on modals and notifications |
| **Phase 2** | `FormaPageHeader` ✅, `FormaToolbar` ✅, `FormaTabs` ✅ | Layout — the shell of every admin page |
| **Phase 3** | `FormaFloatingLabelField` ✅, `FormaToggleSwitch` ✅, `FormaPasswordInput`, `FormaCurrencyInput`, `FormaRangeSlider`, `FormaDatePicker`, `FormaCustomSelect` | Form inputs — needed before building any edit forms |
| **Phase 4** | `FormaDataGrid<TItem>`, `FormaMassActions`, `FormaStatusBadge`, `FormaCapacityIndicator`, `FormaPagination` | Data display — needed for list pages |
| **Phase 5** | `FormaSortableList<TItem>`, `FormaMultiSelectDropdown`, `FormaCategorySelector`, `FormaDropdownMenu`, `FormaColorPicker` | Interactive widgets — needed for editor pages |
| **Phase 6** | `FormaImagePicker`, `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built |
| **Phase 7** | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors — built alongside their specific pages |

---

## File Structure

```
Forma.Blazor/Components/Admin/
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
├── DataDisplay/
│   ├── FormaDataGrid.razor
│   ├── FormaDataGridColumn.razor
│   ├── FormaMassActions.razor
│   ├── FormaStatusBadge.razor
│   ├── FormaCapacityIndicator.razor
│   └── FormaPagination.razor
├── FormInputs/
│   ├── FormaFloatingLabelField.razor
│   ├── FormaToggleSwitch.razor
│   ├── FormaPasswordInput.razor
│   ├── FormaCurrencyInput.razor
│   ├── FormaRangeSlider.razor
│   ├── FormaDatePicker.razor
│   ├── FormaCustomSelect.razor
│   ├── FormaColorPicker.razor
│   └── FormaMultiSelectDropdown.razor
├── Interactive/
│   ├── FormaSortableList.razor
│   ├── FormaCategorySelector.razor
│   ├── FormaDropdownMenu.razor
│   └── FormaImagePicker.razor
├── Editors/
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
