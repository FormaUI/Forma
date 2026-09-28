# Forma.Blazor.DataView

`FormaDataView`, the browsing engine of the **Forma** component library, and the views that
present what it browses: `FormaDataGrid`, rows with aligned columns and headers, sorted by a header
and edited a row at a time. The engine takes a set of items or a provider answering one window at
a time, sorts, counts and pages them, keeps a keyed selection within the bounds a page sets, writes
its sort and page to the address so a link reproduces what a reader saw, and draws the empty, busy
and loading states and the footer; a view draws the items. Built on `Forma.Blazor` and speaking its
namespaces, so a host that already renders the library adds this package and the same `AddFormaUI()`
registers what it needs.

**.NET 11 · Interactive Server components.**

## Install and register

```pwsh
dotnet add package FormaUI.NET-Blazor.DataView
```

The package depends on `Forma.Blazor`, the library it is built on, which restores beside it.

```csharp
using Forma.Blazor;
using Forma.Blazor.Components.DataView;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFormaUI();
builder.Services.AddFormaDataView();
```

`AddFormaDataView()` registers the query executor a data view counts and reads a queryable through,
one that enumerates the queryable where it stands. A host whose items come from a store registers
its own `IDataViewQueryExecutor` first, so a query backed by a database runs there rather than on
the renderer; a host that registers nothing gets the in-memory one all the same.

## A grid over a set

```razor
@using Forma.Blazor.Components.DataView

<FormaDataView Items="_orders" PageSize="20" AddressOptions='@(new DataViewAddressOptions("orders"))'>
    <FormaDataGrid>
        <Columns>
            <FormaPropertyColumn Property="@(order => order.Number)" Sortable="true" />
            <FormaPropertyColumn Property="@(order => order.Customer)" Sortable="true" />
            <FormaPropertyColumn Property="@(order => order.Total)" Format="C" Align="ColumnAlign.End" />
            <FormaTemplateColumn Title="@L["Status"]">
                <FormaStatusBadge Text="@context.Status" Status="@context.StatusKey" />
            </FormaTemplateColumn>
        </Columns>
    </FormaDataGrid>
</FormaDataView>
```

`Items` is any `IQueryable<TItem>`; the engine sorts, counts and takes the page in the store the
queryable stands on. `ItemsProvider` stands in for it where the store answers one window at a time,
taking a `DataViewRequest` and returning a `DataViewPage`. A prefix in `AddressOptions` keeps the
sort and the page in the address, apart from any other data view on the page. `GroupBy` folds the
items under headings, a `DataViewGrouping` naming the key and the name a heading shows; a heading
folds its group away and opens it again, and a folded group stays folded until the grouping changes.

A checkbox column comes with `EnableCheckedSelection`, and selection through the rows themselves,
by pointer and keyboard, with `EnableItemSelection`; both report through `SelectedItems` and
`SelectedItemsChanged`, bounded by `CheckedSelectionOptions`. Inline editing is the grid's:
`EnableInlineEditing` with `CloneForEdit`, driven through `BeginEdit`, `SaveEditAsync` and
`CancelEditAsync` on the grid.

Every view, the engine's selection and its states are localized through the locale packs, and the
[design document](../../docs/data-view-design.md) states the engine, its views and the rules a
change to them keeps.
