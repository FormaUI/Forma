# Forma Data Grid Design

## Scope

`FormaDataGrid<TItem>`: how it builds on QuickGrid, what it forwards and what it owns, its
sizing, its two kinds of selection and their bounds, inline editing, and
`FormaPropertyColumn<TGridItem, TProp>`.

## Decision summary

- **QuickGrid underneath, never reinvented.** The grid extends
  `Microsoft.AspNetCore.Components.QuickGrid`, reusing its sortable columns,
  `PropertyColumn` and `TemplateColumn`, `RowClass` and `OnRowClick`. It adds what an
  admin list needs beside them: a checkbox column with a tri-state select-all, row
  selection, inline editing, and a footer.
- **Columns are QuickGrid's own.** `Columns` is a plain `RenderFragment`; callers author
  `PropertyColumn`, `TemplateColumn` and `FormaPropertyColumn` directly, and the grid
  cascades its item type through `[CascadingTypeParameter]` so those tags infer their
  generic argument.
- **The paginator is the library's.** The grid keeps its own `PaginationState` to drive
  QuickGrid's slicing and renders `FormaPagination` for the page controls, translating the
  state into the paginator's plain parameters. The grid adapts to the paginator, never the
  reverse. `PaginationOptions` defaults to the paginator's own default.
- **No height parameter.** A fixed row count or pixel height is a guess about space the
  component cannot see. The rows live in their own scroll region with the footer outside
  it, and the grid caps itself at `max-block-size: 100%`: against an auto-height ancestor
  the cap resolves to none and the grid shrinks to its content; inside a height-bounded
  shell the rows scroll, the column headers stick to the top of the region and the footer
  stays pinned below it. A paginated grid without `Virtualize` holds its scroll region at
  the measured height of a full page, so a short last page does not shrink it.
  `Virtualize` is for that bounded case with pagination off; in an
  unbounded page it renders everything, and `ItemSize` must match the real row height.
- **Row actions are the caller's.** The grid ships no action-button components; an action
  column is a `TemplateColumn` over the icon buttons, since a wrapper would be strictly
  less capable than the template it wraps.
- **No responsive hiding.** A table keeps every column and scrolls sideways when they do
  not fit.

## The passthrough

Every column-level feature works untouched: `Sortable`, `SortBy`, `Align`, `Format`,
`HeaderTemplate`, `ColumnOptions`, `PlaceholderTemplate`, `InitialSortDirection`,
`GridSort<T>`. Of the grid-level parameters, `Items`, `ItemsProvider`, `ItemKey`,
`OnRowClick`, `QueryParameterNamePrefix`, `Virtualize`, `ItemSize` and `OverscanCount` are
forwarded, and `RowClass` with the grid's own selected and disabled classes layered onto
it. `Pagination` is owned by the grid and bound only while
`EnablePagination` is on, so an unpaginated grid lets virtualization size its scroll range
from the real row count. Not forwarded: `Class` and `AdditionalAttributes`, which land on
the wrapping element as on every component; and `Theme`, which conflicts with the design
tokens.

`QueryParameterNamePrefix` defaults to empty, as QuickGrid's does. Two grids on one page
that share a column title need distinct prefixes, or they share `?sort=` and `?order=` and
sort each other.

## The footer

The footer holds the page controls at the inline start and the caller's `Status` content,
a `role="status"` region, at the inline end. It is skipped entirely when there is no status
and only one page; the page controls alone are hidden whenever pagination is off or
everything fits on one page, even while `Status` keeps the footer showing.

## Selection

Two kinds, each behind its own flag, combined or used alone; both require `Items`, because
select-all and range selection name the whole set, which an `ItemsProvider` could only
produce by fetching every page.

- **Checked selection**, `EnableCheckedSelection`: a checkbox column whose header carries
  a tri-state select-all over the whole `Items` set rather than the visible page, since
  QuickGrid owns the sort and the page slice and the rows on screen are not a set the
  component can name. It pairs with `FormaSelectionToolbar`.
- **Row selection**, `EnableRowSelection`: Explorer-style mouse and keyboard selection
  independent of the checkbox column. A plain click replaces, Ctrl or Cmd toggles, Shift
  ranges, Alt is a no-op; Arrow, Home and End rove focus, Shift extending the range and
  Ctrl or Cmd moving focus alone.

`CheckedSelectionOptions` bounds the checked set. `Maximum` disables further selection
once reached, muting the remaining rows and raising `OnSelectionRejected` on a blocked
attempt, while a full deselect is always allowed; the master control is disabled only when
nothing is selected and selecting everything would exceed the maximum. `Minimum` blocks
unchecking a row, or toggling it off with Ctrl or Cmd, below the floor, but not a
plain-click or range replacement or the master deselect-all, which are reset gestures
rather than incremental removals; the floor mutes rows only while checked selection is
on. `OnSelectionRejected`
reports which `SelectionBoundary` blocked the attempt and its limit. The options' `Caption`
swaps the header checkbox for `SelectAllText` and `DeselectAllText`, as a static label when
one is set or an interactive toggle when both are; `CheckedSelectionCaption.Hidden` renders
nothing in the header.

## Inline editing

`EnableInlineEditing` with `CloneForEdit` lets one row at a time be edited. The grid copies
the row, cascades a `RowEditSession` its columns read, validates the copy with a
`DataAnnotationsValidator` over an `EditContext` of its own, and raises `OnRowSave` only
once the copy is valid: `CancelEditAsync()` discards and raises `OnRowCancel` with the
untouched row, `SaveEditAsync()` commits and reports whether it was accepted.
`BeginEdit(item)`, `IsEditing(item)` and `EditingItem` complete the surface, driven from a
`TemplateColumn` through `@ref`, which is unset on the grid's first render. The item is a
reference type, since the editors write through to the draft. The edited row drops its
selection marker, taking it out of every mouse and keyboard selection gesture for the
duration.

The editor and the validator sit under a fixed, keyed `CascadingValue<EditContext>`:
`InputBase` and `DataAnnotationsValidator` both refuse a context swap, so each edit gets
its own pair rather than re-parameterizing the last.

## `FormaPropertyColumn<TGridItem, TProp>`

A column derived from QuickGrid's public `ColumnBase<TGridItem>`, so it extends the
passthrough rather than replacing it and sits in the same `Columns` fragment as
`PropertyColumn` and `TemplateColumn`. It shows `Property`, titles itself from `[Display]`,
`[DisplayName]` or the member name, sorts unless a caller-supplied `SortBy` wins, and
formats through `Format` against the current culture. While its row is edited it swaps to
an editor chosen from `TProp`: `InputText` for a string, `InputCheckbox`
for a Boolean, `InputDate<T>` for a date or time, `InputNumber<T>` for a number; anything
else, an enum included, needs an `EditTemplate`. `Editable="false"` opts a column out, and
a property with no setter is never editable; either still reads the draft while the row is
open. Validation targets the draft: the column
rewrites `item => item.Member` into `() => draft.Member` for `ValueExpression`, so the
`EditContext` identifies the copy's field rather than the untouched row's.
