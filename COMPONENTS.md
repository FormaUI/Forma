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
| **Blazor approach** | Built on `FormaModal` with explicit confirm/cancel actions supplied by the confirm component (modal default actions disabled for this case). Uses `IConfirmDialogService : INotifyStateChanged` (in `Forma.Blazor.Framework`) / `ConfirmDialogService` (in `Forma.Blazor.Core`), registered via DI, returning `Task<bool>`. Service composes `StateNotifier` for async subscriber notification. Sealed `IDisposable` component with code-behind in `.razor.cs` |

### 6. `FormaToast` ✅

| | |
|---|---|
| **Old pattern** | `ShowInfo('message')` → Toastr-style slide-in notification + `BtnDone(btn)` save-confirmation animation (button text changes to ✓ briefly) |
| **Used in** | All admin pages — after save, delete, update, import, export |
| **Parameters** | Service-driven (no public component parameters). Toast requests sent via `IToastService.ShowAsync(string message, ToastType type = Success, TimeSpan? duration = null)` (default 3s). `ToastType` enum: `Success`, `Error`, `Info`, `Warning`. `IToastService` also exposes `IReadOnlyList<ToastMessage> Toasts`, `Task RemoveAsync(string id)`. Arguments validated (message non-empty, duration non-negative, id non-empty). |
| **Blazor approach** | `IToastService : INotifyStateChanged` (in `Forma.Blazor.Framework`) / `ToastService` (in `Forma.Blazor.Core`), registered via DI. Service composes `StateNotifier` for async `Func<Task>` subscriber notification. A `FormaToastContainer` (sealed `IDisposable`, code-behind in `.razor.cs`) component at layout level subscribes and renders active toasts. Auto-dismissal via `System.Threading.Timer` per toast. CSS `@keyframes` slide-in animation, type-based color variants. No JS needed |

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
| **Used in** | Chrome for every labelled form component — `FormaInput`, `FormaTextArea`, `FormaPasswordInput`, `FormaSelect`, `FormaMultiSelectDropDown`, `FormaDatePicker`, `FormaColorPicker`, `FormaRangeSlider` and `FormaToggleSwitch` |
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
| **Popup placement** | The calendar is a `popover`, so it renders in the **top layer** and no scrolling or clipping ancestor can cut it off — it works inside a grid's scroll region, a modal body, or any `overflow: auto` box. The top layer keeps no layout tie to the trigger, so the shared `wwwroot/scripts/AnchoredPopup.js` module measures and applies the position: it hangs from the trigger's inline-start edge (mirrored in RTL), flips above when there is no room below, clamps into the viewport, and follows scroll and resize. One positioning path for every browser rather than a CSS/JS split. That module is deliberately component-agnostic — `FormaSelect`, `FormaMultiSelectDropDown` and `FormaDropDownMenu` are the same trigger-plus-anchored-panel shape and should import it rather than re-solve the clipping. `popover="manual"` keeps the existing backdrop as the dismissal mechanism, which also sidesteps the iOS 17–18.2 bug where a popover ignores a tap outside it |
| **Blazor approach** | Use Blazor `InputDate` which renders native `<input type="date">`. Browser provides locale-aware calendar. For reset, add a clear button. For min-date, use `min` HTML attribute |

### 16. `FormaRangeSlider` ✅

| | |
|---|---|
| **Old pattern** | `<input type="range">` + hidden field + live value display label |
| **Used in** | forms (border radius), slider (duration), design/edit (font size, site width) |
| **Parameters** | `@bind-Value` (any `INumber<TValue>`), `string Label`, `TValue Min`, `TValue Max`, `TValue Step = TValue.One`, `string? Unit` ("px", "%", "s"), `bool Disabled` |
| **Blazor approach** | `InputBase<TValue>` rendering a native `<input type="range">`, generic over any `INumber<T>` so an `int` and a `double` slider share one component. The current value is shown beside the label rather than in a second field. Tints the native track and thumb with `accent-color` rather than hand-painting a filled track, so the browser's own RTL mirroring stays correct with no direction-aware calculation of its own to keep in step with it |
| **Notes** | The displayed value is `aria-hidden`; a screen reader instead hears `aria-valuetext`, set only when `Unit` is given — without one the native `valuenow` already says the number, and setting it anyway would repeat the same words the input already announces on its own. The value's colour deliberately is not the muted caption token: at its font size that measures under 4.5:1, so it stays at full text contrast and is set apart by size alone, the same trade the strength meter in `FormaPasswordInput` makes |

### 17. `FormaPasswordInput` ✅

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

### 19. `FormaMultiSelectDropDown` ✅

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
| **Blazor approach** | A `<button>` trigger plus an anchored, keyboard-navigable listbox popup, sharing `AnchoredPopup.js` with `FormaDatePicker` and `FormaMultiSelectDropDown` so the list escapes any clipping ancestor. `InputBase<TValue>` directly, matching `InputDate<TValue>`/`InputNumber<TValue>` — the caller supplies a nullable `TValue` (`int?`, `string`, `MyEnum?`) when "nothing selected" needs representing, rather than the component forcing that shape itself |
| **Notes** | Composes `FormaField` in `Pinned` mode, as `FormaMultiSelectDropDown` and `FormaDatePicker` do: the trigger always shows content of its own, so the label stays floated rather than sitting over it. Opens focused on the current selection, the way a native `<select>` does. The selected option is marked by a checkmark plus bold weight, not colour alone |

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
| **Parameters** | `IReadOnlyList<ResourceInfo> Entries`, `string? Accept`, `int MaxSelection = 0` (0 = unlimited), `EventCallback<IReadOnlyList<ResourceInfo>> OnSelected`, folder navigation callbacks |
| **Blazor approach** | `FormaModal` (size Full) hosting a folder breadcrumb, a selectable entry grid and `FormaPagination`, with `FormaUploader` in its toolbar. Type-agnostic — it previews a selected entry through `FormaFileViewer` (#56), so images, video and documents all work through one component. The host supplies the entries and performs the storage operations; the picker never touches a filesystem |

### 36. `FormaRichTextEditor`

| | |
|---|---|
| **Old pattern** | CKEditor 4 (`Controls/CKeditor2/config.js`) in three presets — `Website`, `Basic`, `Mini` — with a fourth, collapsed arrangement under 600px. Custom plugins `imagelibrary` (opens `pops/images.aspx`), `youtube`, `smiley`, `lineheight`, `qform`. `enterMode = ENTER_BR`, `format_tags = p;h1;h2;h3;h4`, RTL contents direction |
| **Used in** | page editor, product description — and in Épica, the News article body |
| **Toolbar (from Coi's `Website` preset)** | Source · Preview · Templates — Cut/Copy/Paste/PasteText/PasteFromWord · Undo/Redo · SelectAll · RemoveFormat — Smiley · SpecialChar · LineHeight · Maximize — Bold/Italic/Underline/Strike — Numbered & bulleted lists — Justify right/center/left/block · BidiRtl/BidiLtr — Link/Unlink/Anchor — YouTube embed · Image · Table · HorizontalRule — Format · Font · FontSize — TextColor — image library. Subscript/Superscript were explicitly removed |
| **Parameters** | `@bind-Value`, `EditorMode Mode` (Html/Markdown), `EditorToolbar Toolbar` (Full/Basic/Mini, mirroring Coi's presets), `bool AllowSourceView`, `EventCallback OnMediaRequested` |
| **Blazor approach** | **Forma's own editor, not a wrapper around someone else's.** The editing surface is `contenteditable` driven from C#, and the chrome is the library's existing components: the format/font/size pickers are `FormaSelect`, the toolbar overflow is `FormaDropDownMenu`, colour is `FormaColorPicker`, image insertion delegates to `FormaImagePicker` (never a second file browser), and direct uploads go through `FormaUploader`. Toolbar presets mirror Coi's three, including the sub-600px collapse. **Markdown is a first-class mode**, not an export: `Mode` switches which document the editor edits, and `AllowSourceView` toggles between the rendered surface and raw HTML/Markdown. Bidi controls and RTL contents direction are required, not optional |
| **External JS** | One narrow dependency, and only for **rendering** Markdown to HTML — a parser is a solved problem not worth reimplementing. It never owns the editing surface, the selection, the undo stack or the toolbar; those are Forma's. Coi's feature set is the floor for what the editor must do, not a spec for how to do it |

### 37. `FormaCloseButton` ✅

| | |
|---|---|
| **Old pattern** | The `×` dismiss control repeated across `admin/pops/*.aspx` modal headers and notification strips |
| **Used in** | Single source of truth for dismiss controls — `FormaModal`'s header, `FormaToastContainer`'s toasts, `FormaContextToolbar`'s clear-selection control |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: close, dismiss, clear selection), `EventCallback<MouseEventArgs> OnClick`, `bool Disabled` |
| **Blazor approach** | A thin wrapper over `<FormaButton Variant="Ghost" IconOnly>` supplying the one X glyph, so it owns no colours of its own and inherits the ghost variant's treatment and the icon-only square sizing. `Label` is required rather than defaulted because a wrong accessible name is worse than an obvious compile error, and the three call sites genuinely differ. Ships no `.razor.css` — every visual decision belongs to `FormaButton` |
| **Notes** | Before this existed the three call sites had each reimplemented `.forma-btn-ghost` verbatim and drifted: two drew a 14×14 SVG while the toast used the `&times;` entity, and the focus ring was `--focus-color` against `FormaButton`'s `--main-color`. Retinting for a saturated surface (the toast) goes through `--forma-btn-ghost-*-color` knobs rather than a fork |

### 40. `FormaEyeButton` ✅

| | |
|---|---|
| **Old pattern** | `<span onclick="TogglePass(this)" class="fas fa-eye-slash">` beside a password field — a clickable span, not a button |
| **Used in** | Single source of truth for reveal controls — `FormaPasswordInput`'s toggle, and any later show/hide or preview affordance |
| **Parameters** | `string Label` (required — the accessible name, which differs per use: show password, hide value, preview), `bool Revealed`, `ButtonSize Size`, `bool Disabled`, `EventCallback<MouseEventArgs> OnClick` |
| **Blazor approach** | A thin wrapper over `<FormaButton Variant="Ghost" IconOnly>` supplying one eye glyph whose strike-through path is the only thing that changes, so the two states cannot drift apart. Ships no `.razor.css` — every visual decision belongs to `FormaButton`. `Revealed` and `Label` are both caller-owned: the component never assumes it is guarding a password |
| **Notes** | `FormaPasswordInput` had reimplemented the ghost variant by hand — its own muted colour, hover pair, focus ring and coarse-pointer square — which is the same drift that produced `FormaCloseButton`. Because the button element now belongs to `FormaButton`, CSS isolation puts it out of reach of the field's own stylesheet, so the field positions a wrapper span instead of the control |

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
| **Used in** | `FormaToolbar` actions, `FormaMultiSelectDropDown`'s add-new affordance, and any editor that appends a row |
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
| **Used in** | Layouts with no header row to click — `FormaFilePicker`'s tile grid, and any card list |
| **Parameters** | `RenderFragment ChildContent` (the sort options as menu items), `string Label` (required) |
| **Blazor approach** | A preset over `FormaDropDownMenu` like `FormaOverflowMenu`, because sorting offers a choice rather than performing one action — which is why it is a `Menu` and not a `Button`, and why it sits with the menus rather than in `Actions/`. A grid that already has sortable column headers should keep using them — two controls over one sort state is a synchronisation problem with no upside |

### 50. `FormaViewButton`

| | |
|---|---|
| **Glyph** | **Layout lines** — stacked rows for a list, a square of cells for a tile grid |
| **Old pattern** | None — Coi's image library had one fixed thumbnail grid |
| **Used in** | `FormaFilePicker`, switching between a dense list and thumbnails |
| **Parameters** | `ViewMode Mode`, `EventCallback<ViewMode> ModeChanged`, `string Label` (required), `ButtonSize Size`, `bool Disabled` |
| **Blazor approach** | Stateful like `FormaEyeButton`, drawing the mode it will switch **to** and naming that mode in the label, since an icon-only toggle that draws its current state cannot say which it is. **To settle when built:** whether a two-option segmented control showing both modes at once beats a toggle, which is guesswork until the picker exists |

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

### 53. `FormaGallery`

| | |
|---|---|
| **Old pattern** | The gallery module's sortable thumbnail list, each row carrying an image and its caption |
| **Used in** | Any set of images edited as a unit — article image sets, race stage photos, sponsor logos, and Épica's Photo Galleries module (P15, whose CMS task is bulk upload plus captioning) |
| **Parameters** | `@bind-Items` (`List<GalleryItem>`), `EventCallback<GalleryItem> OnCaptionChanged`, `bool AllowCaptions`, `int MaxItems`, `EventCallback OnAddRequested` |
| **Blazor approach** | A composition, not a new widget: `FormaSortableList` supplies the drag-and-keyboard reordering, `FormaImagePicker` or `FormaUploader` supplies "add", and this adds the one thing neither has — a caption field per tile and a remove control, edited in place rather than through a dialog per image |
| **Notes** | This is the **editing** surface only — it implements no viewer of its own. Click-to-enlarge opens `FormaFileViewer` (#56) inside a `FormaModal`, so the gallery, the picker and the manager all enlarge an image through the same component. Worth checking against `FormaSortableList` + `FormaImagePicker` before building: the pair already covers ordering and adding, so this earns its place on captioning and bulk add, and on the four-plus surfaces that repeat the pattern — not on the composition alone |

### 54. `FormaFileManager`

| | |
|---|---|
| **Old pattern** | `pops/images.aspx` again — the same page as `FormaFilePicker` (#35), but its *management* half: create, rename and delete folders, move files between them, delete in bulk, watch the capacity indicator |
| **Used in** | The media library as a page in its own right, rather than a dialog opened to answer a question |
| **Parameters** | `IReadOnlyList<ResourceInfo> Entries`, folder navigation callbacks, `EventCallback<ResourceInfo> OnRename`, `EventCallback<IReadOnlyList<ResourceInfo>> OnDelete`, `EventCallback<FolderMove> OnMove`, `EventCallback OnFolderCreate` |
| **Blazor approach** | The picker and the manager are the same browsing surface with different endings: one **returns a selection and closes**, the other **mutates and stays**. So the shared half — folder breadcrumb, entry grid, selection, `FormaPagination` and the `FormaUploader` in its toolbar — is extracted once and composed by both, rather than each growing its own copy. The manager adds the destructive actions and owns none of the storage, reporting intent through callbacks exactly as the uploader does |
| **Notes** | Building this without extracting the shared surface first is how the two drift, which is the same failure that produced `FormaCloseButton`, `FormaEyeButton` and `FormaButton` after the fact rather than before. **To settle when built:** whether #35's "Blazor approach" is rewritten to compose that extracted surface, or the manager is defined as the base and the picker becomes a modal skin over it |

### 55. `FormaVideoPlayer` ✅

| | |
|---|---|
| **Old pattern** | CKEditor's **YouTube embed** toolbar button (#36) and the raw `<iframe>` markup it pasted into the body |
| **Used in** | The most-demanded component on this list — Épica wants it on four separate surfaces: YouTube-by-URL embeds in articles (6.5), the auto-embedded live broadcast player (9.2), the Videos module's listings and featured video (P10), and the home page's featured video block (11.4). `FormaFileViewer` (#56) delegates its video branch here, and `FormaRichTextEditor` inserts it |
| **Parameters** | `string Source` (required), `string Title` (required — the player's accessible name), `bool Autoplay`, `bool DeferLoading` (default `true`), `string? Placeholder`, `IReadOnlyList<VideoCaptionTrack>? Captions`, plus captured pass-through attributes |
| **Blazor approach** | Resolve the address through an ordered chain of `IPlayerProvider`s (`IPlayerProvider`, `PlayerOptions` and `PlayerFrame` live in `Forma.Blazor.Framework`), so a watch link, a share link and an embed link all work without the caller naming the service. Providers the host registered are tried first, so an application can teach the player an address shape this library never knew — or take over one it would otherwise claim — the same way a host extends `FormaFileViewer`; the eight built-ins follow, with `FilePlayerProvider` last because it accepts any fetchable address. YouTube, Vimeo and Twitch return an embed address the player loads in an `<iframe>`; TikTok, Instagram, Facebook and X publish markup plus a script instead, which a colocated JS module loads once and asks to scan, each service sizing its own player; any other fetchable address falls back to the browser's own `<video>`, with optional `<track>` captions. Size with the `aspect-ratio` property rather than a padding hack. **External JS is confined to the four services whose player *is* a script** — they publish no embed address, and the iframe providers need none |
| **Privacy and weight** | Nothing is fetched from a hosting service until a reader presses play: `DeferLoading` holds both the embed address and the service script behind a play control, which `Autoplay` overrides and a self-hosted file never needs. Embed from `youtube-nocookie.com`, and ask for do-not-track wherever a service offers it — `dnt=1` on Vimeo, `data-dnt="true"` on X. A third-party frame that loads with every article page is both a tracking surface a public news site has to account for and a large fixed cost on a mobile connection. **The still is the host's to supply:** `Placeholder` takes an image the host serves from its own origin, and stands in for a self-hosted file too. Deriving one from the service instead — YouTube publishes a thumbnail per video id — would send the service the very request the play control exists to withhold, so the component never does it |
| **Notes** | `Title` is required because an unnamed `<iframe>` is an unlabelled stop in the tab order. **Autoplay is not what Épica's 9.2 asks for** — "auto-embed live player" means the player appears without an editor placing it, not that it starts playing. Autoplay is forced muted through the JS property, because the `muted` attribute no longer reaches the element once Blazor has created it, and WCAG 1.4.2 requires a control over anything that plays by itself. A service script that never runs — blocked, or simply slow — leaves a link to the video in the player's place |

### 56. `FormaFileViewer`

| | |
|---|---|
| **Old pattern** | The preview pane of `pops/images.aspx` — clicking a library entry to actually look at it |
| **Used in** | The promise #35 already makes and does not own: showing whatever a chosen entry turns out to be. That belongs here, so the picker, the manager (#54) and the gallery's enlarge (#53) show one preview implementation rather than three |
| **Parameters** | `string Source` (required), `IReadOnlyList<string>? Siblings`, `EventCallback<string> OnNavigate`, `bool ShowMetadata`, `RenderFragment<string>? UnsupportedTemplate`. The caller passes an address, not a resolved resource — a component handed a `ResourceInfo` would need every caller to resolve one first, which is the provider's job. `ResourceInfo` (`Source`, derived `Name`, `Kind`, `Size`, `virtual DisplaySize`, `Extension`), its `EmbeddedResourceInfo` and `MarkupResourceInfo` subtypes, `ResourceKind` and `IResourceProvider` live in `Forma.Blazor.Framework`, which carries no ASP.NET Core reference — so a converting package stays a plain class library. `FileResourceInfo` wraps and implements `IBrowserFile`, so it lives in `Forma.Blazor` where that reference already exists. Named for resources rather than media because the viewer renders documents too |
| **Blazor approach** | An ordered chain of `IResourceProvider`s resolves the address into a `ResourceInfo`, mirroring `FormaVideoPlayer`'s provider chain: the first to claim it decides what the resource is, and the viewer owns the markup for every kind, so a converting package never ships Razor or CSS. Resolution runs once per address rather than once per render. Providers the host registered are tried first; `BrowserResourceProvider` comes last and claims only what a browser renders unaided, so **an address no provider claims degrades to a type glyph, the name, the size when one is known, and a download link** rather than a broken embed. A `MarkupResourceInfo` is rendered as trusted markup, so a converting provider must sanitise its output. An `EmbeddedResourceInfo` whose content type is `text/*` is framed with `sandbox=""` — an opaque origin, withholding scripts, forms and navigation — because uploaded HTML is untrusted; anything else goes to `<object>` with a new-tab link as fallback content. With `Siblings` supplied the viewer steps through the set, resolving the horizontal arrow keys through `IsRightToLeft`; the host modal keeps `Escape` |
| **Converting packages** | `Forma.Blazor.Files.Markdown` (Markdig) and `Forma.Blazor.Files.Office` (OfficeIMO) ship separately and return `MarkupResourceInfo`, so neither Markdig nor OfficeIMO lands in the package every consumer downloads. A host registers them like any other service — `AddScoped<IResourceProvider, …>()` — and without them `ResourceKind.Document` still works for whatever the browser handles natively |
| **Zoom and pan** | Settled: the frame is a plain scrollable region (`overflow: auto`) rather than a transform-driven canvas. Touch and pen pan it natively, and `touch-action: pan-x pan-y pinch-zoom` hands pinch-to-zoom to the platform itself — the exact case CSS was expected to handle worst turned out not to need JS at all. The one gap native scrolling doesn't cover is a mouse click-drag, so a small colocated `.razor.js` adds that, gated to `pointerType === 'mouse'` so it never fights the browser's own touch scrolling. Zoom level moves in five fixed steps (100–400%) through on-screen +/− buttons, with no wheel or double-click zoom, since the scroll wheel already has a native job on this element and hijacking it would fight the reader's own scrolling |
| **Notes** | **Viewing only — crop, rotate and resize are editing** and belong to `FormaImageEditor` (#57), even though Coi's `jcrop` widget sat on this same preview. A viewer that quietly mutates the file is a different component with a different contract, and folding it in here is how this one would stop being safe to open on anything |

### 57. `FormaImageEditor`

| | |
|---|---|
| **Old pattern** | Two surfaces, not one. `jcrop` in `slider.aspx` did a **constrained crop-to-fit**: aspect ratio and minimum size both pinned to the destination slot, the image scaled into a fixed 590×350 box, an initial selection set for the editor, and `tellSelect()` posting `x/y/w/h` plus the target dimensions to `crop.asmx`. Separately, `pops/pixie.aspx` hosted the commercial **Pixie** editor in an `<iframe>`, launched per-entry from the image library |
| **Used in** | Making an arbitrary upload fit a fixed slot — article covers and OG images, video thumbnails, sponsor logos, gallery tiles. Launched from `FormaFileViewer` (#56), the manager (#54) and the picker (#35), the same way Coi launched it from its library |
| **Parameters** | `string Source` (required), `double? AspectRatio`, `IReadOnlyList<AspectRatioPreset>? Presets`, `int? MinWidth`, `int? MinHeight`, `int? OutputWidth`, `int? OutputHeight`, `EventCallback<ImageEdit> OnApply`, `EventCallback OnCancel` |
| **Scope** | **In:** crop with free or locked ratio and named presets, rotate in 90° steps, flip horizontal and vertical, resize to target dimensions, and zoom for precision. **Out:** filters, text, stickers, drawing, shapes, gradients and layers — that is Pixie's tier, it is a canvas compositing app rather than a control, and reaching it by pulling in a library would spend the one external-JS exception this repo has deliberately not spent twice |
| **Blazor approach** | Follow Coi's actual division of labour, which already matches this library's boundary: **the component describes the edit, the host performs it.** `OnApply` emits an `ImageEdit` — crop rectangle, rotation, flips, target size — and the host renders it with ImageSharp or SkiaSharp, exactly as `FormaUploader` owns no storage and `FormaFilePicker` never touches a filesystem. A colocated `.razor.js` drives the handles and the live preview through Pointer Events and Canvas 2D; that is hand-written interop for behaviour only script can provide, not an external library |
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
| **CKEditor** | Now in scope as `FormaRichTextEditor` (#36) rather than excluded — Forma builds its own editor over `contenteditable` with its own controls, taking Coi's feature set as the floor. The only external JS is a Markdown renderer |

---

## Suggested Build Order

Components are ordered by dependency and reuse frequency:

| Phase | Components | Rationale |
|---|---|---|
| **Phase 1** | `FormaModal` ✅, `FormaConfirmDialog` ✅, `FormaToast` ✅ | Infrastructure — everything depends on modals and notifications |
| **Phase 2** | `FormaPageHeader` ✅, `FormaToolbar` ✅, `FormaTabs` ✅ | Layout — the shell of a page |
| **Phase 3** | `FormaInput` ✅, `FormaToggleSwitch` ✅, `FormaPasswordInput` ✅, `FormaCurrencyInput`, `FormaRangeSlider` ✅, `FormaDatePicker` ✅, `FormaSelect` ✅ | Form inputs — needed before building any edit forms |
| **Phase 4** | `FormaDataGrid<TItem>` ✅, `FormaContextToolbar` ✅, `FormaStatusBadge` ✅, `FormaCapacityIndicator`, `FormaPagination` ✅ | Data display — needed for list pages |
| **Phase 5** | `FormaSortableList<TItem>` ✅, `FormaMultiSelectDropDown`  ✅, `FormaCategorySelector`, `FormaDropDownMenu` ✅, `FormaColorPicker` ✅ | Interactive widgets — needed for editor pages |
| **Phase 6** | `FormaNotesEditor`, `FormaExportButton`, `FormaDesignPicker`, `FormaFontPicker` | Feature-specific components — built when their feature pages are built |
| **Phase 7** | `FormaPermissionMatrix`, `FormaProductOptionsEditor`, `FormaFilterEditor` | Complex editors — built alongside their specific pages |
| **Phase 8** | `FormaButton` ✅ | Shared button primitive — built after the fact and retrofitted onto `FormaDataGrid`, `FormaModal`, `FormaConfirmDialog`, `FormaPagination`, `FormaDatePicker` as the single source of truth for buttons |
| **Phase 9** | `FormaCheckbox` ✅ | Shared checkbox primitive — built after the fact and retrofitted onto `FormaDataGrid` and `FormaMultiSelectDropDown` as the single source of truth for checkboxes |
| **Phase 10** | `FormaCloseButton` ✅ | Shared dismiss primitive — built after the fact and retrofitted onto `FormaModal`, `FormaToastContainer` and `FormaContextToolbar` as the single source of truth for the `×` control |
| **Phase 11** | `FormaField` ✅, `FormaTextArea` ✅ | Shared field chrome — extracted from `FormaInput` so the floating label, focus ring and validation styling live once, then `FormaTextArea` built on it. `FormaPasswordInput` ✅ composes it; `FormaCurrencyInput` will too rather than restating it |
| **Phase 12** | `FormaEyeButton` ✅ | Shared reveal primitive — built after the fact and retrofitted onto `FormaPasswordInput`, which had hand-rolled the ghost icon button it now uses |
| **Phase 13** | `FormaDeleteButton`, `FormaUpdateButton`, `FormaEditButton`, `FormaAddButton`, `FormaDuplicateButton`, `FormaRefreshButton`, `FormaShareButton`, `FormaPublishButton` | The rest of the icon-button vocabulary — the same thin wrapper over `FormaButton`, one glyph each, so a row's actions stop being ad-hoc markup per page |
| **Phase 14** | `FormaOverflowMenu`, `FormaContextMenu` | Menu presets over `FormaDropDownMenu` — the ellipsis trigger first, then the same panel opened by right-click, once there is a menu worth opening that way |
| **Phase 15** | `FormaVideoPlayer` ✅, `FormaFileViewer` | Media rendering — the player first because it stands alone and four Épica surfaces want it, then the viewer that delegates its video branch to it. Both come before the picker chain, which previews through the viewer |
| **Phase 16** | `FormaUploader`, `FormaFilePicker`, `FormaImagePicker` | The picker chain, in that order: the uploader, then the picker that hosts it and previews through `FormaFileViewer`, then the image skin over the picker |
| **Phase 17** | `FormaSortMenu`, `FormaViewButton` | The two controls that exist for the picker's tile grid — a layout with no column headers to sort by and two layouts to switch between. Neither has a surface to serve until Phase 16 exists |
| **Phase 18** | `FormaGallery`, `FormaFileManager` | Media surfaces — both compose the uploader, picker and viewer rather than re-solving them. The manager's shared browsing half is extracted from `FormaFilePicker` in the same step, not copied |
| **Phase 19** | `FormaRichTextEditor` | The largest editor, and last of the composing components — its chrome is `FormaSelect`, `FormaDropDownMenu` and `FormaColorPicker`, it delegates image insertion to `FormaImagePicker`, uploads through `FormaUploader`, and inserts `FormaVideoPlayer` |
| **Phase 20** | `FormaImageEditor` | The heaviest media component and the last — it needs the surfaces above to launch it, and its pointer handles, coordinate mapping and keyboard path are worth building once the rest of the media flow is settled |

---

## File Structure

```
Forma.Blazor/Components/
├── FormaIcon.cs                          # readonly record struct with FontAwesome()/From() factories
├── FormaField.razor / .razor.cs / .razor.css
├── FieldLabelMode.cs                     # enum: Floating, Pinned, Above, Inline
├── Layout/
│   ├── BreadcrumbItem.cs                # record with Label + Url?, constructor-validated
│   ├── FormaPageHeader.razor / .razor.cs
│   ├── FormaToolbar.razor / .razor.cs
│   ├── FormaContextToolbar.razor / .razor.cs
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
│   └── FormaPagination.razor
├── Indicators/
│   ├── FormaStatusBadge.razor
│   └── FormaCapacityIndicator.razor
├── Actions/
│   ├── ButtonSize.cs
│   ├── ButtonType.cs
│   ├── ButtonVariant.cs
│   ├── FormaButtonOptions.cs
│   ├── FormaButton.razor
│   ├── FormaCloseButton.razor
│   ├── FormaEyeButton.razor
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
├── Input/
│   ├── SelectOption.cs
│   ├── PasswordStrength.cs
│   ├── FormaCheckbox.razor
│   ├── FormaInput.razor
│   ├── FormaTextArea.razor
│   ├── FormaToggleSwitch.razor
│   ├── FormaPasswordInput.razor
│   ├── FormaCurrencyInput.razor
│   ├── FormaRangeSlider.razor
│   ├── FormaDatePicker.razor
│   ├── FormaSelect.razor
│   ├── FormaColorPicker.razor
│   └── FormaMultiSelectDropDown.razor
├── Interactive/
│   ├── FormaSortableList.razor
│   ├── FormaCategorySelector.razor
│   ├── FormaDropDownMenu.razor
│   ├── FormaDropDownMenuItem.razor
│   ├── FormaDropDownMenuDivider.razor
│   ├── FormaOverflowMenu.razor
│   ├── FormaContextMenu.razor
│   └── FormaSortMenu.razor
├── Files/
│   ├── FormaUploader.razor
│   ├── FormaFilePicker.razor
│   ├── FormaFileManager.razor
│   └── FormaFileViewer.razor          # dispatches to Media/ for the type it recognises
├── Media/
│   ├── FormaImagePicker.razor
│   ├── FormaGallery.razor
│   ├── FormaImageEditor.razor / .razor.js
│   ├── FormaVideoPlayer.razor
│   ├── FormaDesignPicker.razor
│   └── FormaFontPicker.razor
├── Editors/
│   ├── FormaRichTextEditor.razor
│   ├── FormaNotesEditor.razor
│   ├── FormaPermissionMatrix.razor
│   ├── FormaProductOptionsEditor.razor
│   └── FormaFilterEditor.razor
└── Metadata/
    └── FormaPageLocalization.razor / .razor.cs    # canonical + hreflang links for the current page
```

```
Forma.Blazor.Framework/
├── Css/
│   └── CssColor.cs
└── Components/
    ├── INotifyStateChanged.cs
    ├── Feedback/
    │   ├── IToastService.cs
    │   ├── IConfirmDialogService.cs
    │   ├── ToastMessage.cs
    │   ├── ToastType.cs
    │   └── ConfirmDialogRequest.cs
    └── Indicators/
        ├── IStatusBadgeCatalog.cs
        └── StatusBadgeStyle.cs

Forma.Blazor.Core/
└── Components/
    ├── StateNotifier.cs
    ├── Feedback/
    │   ├── ToastService.cs
    │   └── ConfirmDialogService.cs
    └── Indicators/
        └── StatusBadgeCatalog.cs
```
