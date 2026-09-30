# Forma.Blazor.DataView

`FormaDataView` is the collection component of the **Forma** component library. Its engine manages
items and shared state, while interchangeable built-in or custom views determine how those items
are presented and interacted with. It can present a simple list or support richer arrangements
such as grids, cards and trees.

The engine accepts a set of items or a provider that supplies one slice at a time, and supports
sorting, counting, paging, keyed selection and synchronizing sort and page state with the address.
It also renders the empty, busy and loading states and the footer. Each view supplies its own
presentation and interactions: `FormaDataGrid`, for example, displays rows with aligned columns
and headers, supports sorting by a header and edits one row at a time.

Built on `Forma.Blazor` and using its namespaces, the package integrates with the library through
the same `AddFormaUI()` registration.

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
queryable stands on. `ItemsProvider` stands in for it where the store answers one slice at a time,
taking a `DataViewRequest` and returning a `DataViewPage`. A prefix in `AddressOptions` keeps the
sort and the page in the address, apart from any other data view on the page. `GroupBy` folds the
items under headings, a `DataViewGrouping` naming the key and the name a heading shows; a heading
folds its group away and opens it again, and a folded group stays folded until the grouping changes.

`Virtualize` draws only the rows near the viewport, in every view, grouped or not. A grouped data
view that virtualizes asks for the size of each group with the set's count: a provider answers a
request whose `IncludesGroupSizes` is set with `GroupSizes`, which `CountGroups` computes from a
queryable the provider holds.

A checkbox column comes with `EnableCheckedSelection`, and selection through the rows themselves,
by pointer and keyboard, with `EnableItemSelection`; both report through `SelectedItems` and
`SelectedItemsChanged`, bounded by `CheckedSelectionOptions`. Inline editing is the grid's:
`EnableInlineEditing` with `CloneForEdit`, driven through `BeginEdit`, `SaveEditAsync` and
`CancelEditAsync` on the grid.

Every view, the engine's selection and its states are localized through the locale packs, and the
[design document](https://github.com/FormaUI/Forma/blob/main/docs/data-view-design.md) states the engine, its views and the rules a
change to them keeps.
