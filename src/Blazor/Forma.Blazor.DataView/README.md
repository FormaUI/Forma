# Forma.Blazor.DataView

`FormaDataView`, the collection component of the **Forma** component library. Its engine manages a
set of items and the state around them, the source, sort, page, selection, grouping, the address and
the empty and loading states, and interchangeable views present them: `FormaDataGrid` as rows with
aligned columns, `FormaDataList` as a list, `FormaDataCards` as cards and `FormaDataTree` as a tree,
or a view of the host's own. One data view can hold several views, with a switch choosing between
them.

**.NET 11 · Blazor components.**

## Install and register

```pwsh
dotnet add package formaui-net.Blazor.DataView --prerelease
```

The package depends on `formaui-net.Blazor`, which restores beside it. Its components are in the
`Forma.Blazor.Components.DataView` namespace.

```csharp
using Forma.Blazor;
using Forma.Blazor.Components.DataView;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddFormaServerIntegration();
builder.Services.AddFormaDataViewServices();
```

`AddFormaDataViewServices()` registers the `IDataViewQueryExecutor` a data view counts and reads a queryable
through, one that enumerates it in memory. A host whose items come from a database registers its own
first, as shown under "Reading from a database".

## A grid

```razor
@using Forma.Blazor.Components.DataView
@inject IOrderService OrderService

<FormaDataView Items="_orders" PageSize="20" AddressOptions='@(new DataViewAddressOptions("orders"))'>
    <FormaDataGrid>
        <Columns>
            <FormaPropertyColumn Property="@(order => order.Number)" />
            <FormaPropertyColumn Property="@(order => order.Customer)" />
            <FormaPropertyColumn Property="@(order => order.PlacedOn)" Title="Placed" Format="d" />
            <FormaPropertyColumn Property="@(order => order.Total)" Format="C" Align="ColumnAlign.End" />
            <FormaTemplateColumn Title="Status">@context.Status</FormaTemplateColumn>
        </Columns>
    </FormaDataGrid>
</FormaDataView>

@code {
    private IQueryable<Order> _orders = Enumerable.Empty<Order>().AsQueryable();

    protected override async Task OnInitializedAsync()
        => _orders = (await OrderService.GetRecentOrdersAsync()).AsQueryable();
}
```

`Items` is an `IQueryable<TItem>`, which the engine sorts, counts and pages. A new queryable is a
new set, which the data view reads again, so a page holds its queryable in a field. `PageSize` sets the
page, `EnablePagination="false"` shows every item in one scroll region, and `AppendMode` appends each
page to the items from a Load More action after them, `Manual`, and as the reader reaches their end,
`Automatic`. `AddressOptions` writes
the sort, the page and the active view to the address under its prefix, so a link reproduces them
and two data views on one page keep theirs apart.

A `FormaPropertyColumn` shows a member, titled by the member's name unless `Title` is given, and
sorts by it when its header is pressed, which `Sortable="false"` turns off. A `FormaTemplateColumn`
renders its content for each item, given as `context`. `Width` sets a column's width, `Frozen` keeps
it in place while the others scroll sideways, and `Priority` decides which columns give way when the
width runs out.

## Several views

The views are declared inside the data view, and `FormaDataViewSwitch` and `FormaDataSortMenu`
choose the view and the sort, inside the data view or, given its reference, beside it:

```razor
@if (_products is not null)
{
    <FormaDataViewSwitch DataView="_products" />
    <FormaDataSortMenu TItem="Product" DataView="_products" Sorts="_sorts" />
}

<FormaDataView @ref="_products" TItem="Product" Items="_catalog" EnablePagination="false"
               ItemName="@(product => product.Name)" AddressOptions='@(new DataViewAddressOptions("products"))'>
    <FormaDataGrid>
        <Columns>
            <FormaPropertyColumn Property="@(product => product.Name)" />
            <FormaPropertyColumn Property="@(product => product.Price)" Format="C0" />
        </Columns>
    </FormaDataGrid>
    <FormaDataList>
        <ItemTemplate Context="product">@product.Category</ItemTemplate>
    </FormaDataList>
    <FormaDataCards>
        <ThumbnailTemplate Context="product"><img src="@product.ImageUrl" alt="" /></ThumbnailTemplate>
        <ItemTemplate Context="product">@product.Price.ToString("C0")</ItemTemplate>
    </FormaDataCards>
</FormaDataView>

@code {
    private FormaDataView<Product>? _products;
    private IQueryable<Product> _catalog = Enumerable.Empty<Product>().AsQueryable();

    private readonly IReadOnlyList<DataViewSortOption<Product>> _sorts =
    [
        new("name", "Name", DataViewSort<Product>.ByAscending(product => product.Name)),
        new("price", "Price", DataViewSort<Product>.ByAscending(product => product.Price)) { InitialDirection = DataViewSortDirection.Descending },
        new("category", "Category", DataViewSort<Product>.ByAscending(product => product.Category).ThenAscending(product => product.Name)),
    ];
}
```

`DefaultSort` names the sort the data view opens in, one of the sort options, before any view has
drawn, so cards open in the order a grid would; a sort the address names wins. `RequiresSort` keeps a
sort applied, a header turning its direction rather than clearing it.

`View` and `ViewChanged` name the active view by its key: `grid`, `list`, `cards` or `tree`. A tree
reads the hierarchy from `ParentKey`, and `CurrentItem` names the node whose children the other
views show. A provider is asked with the node in the request's `Parent`, and its `Kind` says
whether the request reads the content the other views show, `Content`, or the children a tree
shows, `Children`, so a provider answers each its own way, even at the roots, as a file browser
lists every file beside a tree of folders.

`Layout="DataViewLayout.Host"` hands the arrangement to the host: it places the views, a sidebar tree
among them, inside the data view's content wherever it wants them, and `FormaDataViewFooter` where the
paginator and the status belong, the data view drawing no footer of its own. The host's arrangement
fills the data view's region, so each of its places scrolls within itself:

```razor
<FormaDataView TItem="Entry" Items="_entries" Layout="DataViewLayout.Host" ...>
    <div class="arrangement">
        <aside class="sidebar"><FormaDataTree TItem="Entry" SelectsCurrent="true" /></aside>
        <FormaSplitter @bind-Value="_sidebarWidth" Min="10" Max="24" Label="Resize Folders" />
        <div class="content">
            <FormaDataGrid TItem="Entry">...</FormaDataGrid>
            <FormaDataViewFooter />
        </div>
    </div>
</FormaDataView>
```

A sidebar's `RootsItem` is an item of the host's, under a key of its own, that stands for the roots,
drawn first among the top-level nodes: it never opens, it shows as current while the roots are, and
choosing it makes them current again, as a file browser's All Files row does.

`OnCurrentItemChanging` is raised before a move into a node changes anything, with the node and the
one before it, and its handler calls `Refuse()` to keep the reader where they are, as a host that
checks a folder may be opened does.

`InitialOpenKeys` names the nodes that start open, by the keys `ItemKey` reads, so a tree reopens
the branches its reader left open; each opens once, as the level holding it loads, and only those
branches are asked for. A node that becomes current opens the ancestors that reach it, so a host
opening on a node deep in the hierarchy names those ancestors too:

```razor
<FormaDataView TItem="Entry" ItemsProvider="ProvideAsync" ItemKey="@(entry => entry.Id)"
               HasChildren="@(entry => entry.IsFolder)" InitialOpenKeys="_openFolderIds"
               @bind-CurrentItem="_folder">
    <FormaDataTree TItem="Entry" SelectsCurrent="true" />
    <FormaDataGrid TItem="Entry">...</FormaDataGrid>
</FormaDataView>
```

## Items from a provider

`ItemsProvider` stands in for `Items` where the store answers one slice at a time. It receives a
`DataViewRequest` of `StartIndex`, `Count`, the sort and the grouping, and returns a `DataViewPage`
of the slice and the total count, null where the store cannot count cheaply. `ApplySorting` orders
a queryable the provider holds as the request asks, here an Entity Framework Core set:

```razor
<FormaDataView TItem="Order" ItemsProvider="ProvideOrdersAsync" PageSize="50">
    ...
</FormaDataView>
```

```csharp
private async ValueTask<DataViewPage<Order>> ProvideOrdersAsync(DataViewRequest<Order> request)
{
    var sorted = request.ApplySorting(_database.Orders);
    var slice = sorted.Skip(request.StartIndex);

    return new DataViewPage<Order>(
        await (request.Count is { } count ? slice.Take(count) : slice).ToArrayAsync(request.CancellationToken),
        await _database.Orders.CountAsync(request.CancellationToken));
}
```

## Grouping and virtualization

`GroupBy` folds the items under headings, each of which the reader can collapse:

```razor
<FormaDataView Items="_orders" PageSize="20" GroupBy="@(DataViewGrouping<Order>.By(order => order.Customer))">
    <FormaDataGrid>
        <Columns>
            <FormaPropertyColumn Property="@(order => order.Number)" />
            <FormaPropertyColumn Property="@(order => order.Total)" Format="C" Align="ColumnAlign.End" />
        </Columns>
    </FormaDataGrid>
</FormaDataView>
```

`Virtualize` renders only the items near the viewport while `EnablePagination` is off, in every
view, with `ItemHeight` as the height of one row. A provider behind a grouped, virtualized data view
answers a request whose `IncludesGroupSizes` is set with `GroupSizes`, which
`request.CountGroups(source)` computes.

## Selection

`EnableCheckedSelection` gives every item a checkbox with a master control over them, and
`EnableItemSelection` selects by pointer and keyboard on the items themselves. Both report through
`SelectedItems` and `SelectedItemsChanged`, bound as `@bind-SelectedItems`. `ItemKey` identifies an
item across reloads. `CheckedSelectionOptions` sets a `Minimum` and a `Maximum`, reported through
`OnSelectionRejected` when a change would cross one, a `Maximum` of one making each choice replace the
last, and its `SelectAllMode` sets what the master
control reaches: `Direct` selects the whole set at one press, `FromMenu` the items on screen with a
menu offering the page and the set, `FromPager` the items on screen with the footer offering the set,
and `None` leaves the master control out.

`EnableItemFocus` gives the items the focus where no selection is on: the collection is one tab stop,
the arrows move the focus from item to item without selecting or opening anything, and Enter and a
press activate the item, raising `OnItemClick`. A selection mode turns checked selection on over it,
its `ChecksOnPress` having a press check the item's box while Enter still activates it, and the
switch keeps the focus on the item it stood on:

```razor
<FormaDataView Items="_files" EnableItemFocus="true" EnableCheckedSelection="_isSelecting"
               CheckedSelectionOptions="@(new CheckedSelectionOptions { ChecksOnPress = true })"
               @bind-SelectedItems="_chosen" OnItemClick="Open">
    <FormaDataCards Dense="true" />
</FormaDataView>
```

Four rules, each a function of the item, say what an item may do. `IsItemDisabled` mutes an item and
takes it out of every selection and activation, `CanSelectItem` keeps an item from being selected
while it still activates, `CanActivateItem` keeps it from activating or becoming the current node,
and `CanFocusItem` draws it without a marker, so the keys pass over it:

```razor
<FormaDataView Items="_entries" EnableItemSelection="true" ItemKey="@(entry => entry.Id)"
               IsItemDisabled="@(entry => entry.IsLocked)"
               CanSelectItem="@(entry => !entry.IsFolder)">
    <FormaDataList />
</FormaDataView>
```

`TabOrder="DataViewTabOrder.ActiveItem"` takes every item's own controls out of the Tab sequence but the
active item's, the one holding the focus or the collection's tab stop, so Tab enters a collection of
hundreds of items once and leaves it after one item's controls. A control inside an item that carries
`data-item-actions` opens the item's actions, which Shift+F10 and the Menu key on the item press, the
focus coming back to the item as they close:

```razor
<FormaDataView Items="_files" EnableItemSelection="true" TabOrder="DataViewTabOrder.ActiveItem">
    <FormaDataCards>
        <ActionsTemplate Context="file">
            <FormaButton OnClick="() => Open(file)">Open</FormaButton>
            <FormaOverflowMenu Label="@($"Actions for {file.Name}")" data-item-actions>
                <FormaDropDownMenuItem OnClick="() => Rename(file)">Rename</FormaDropDownMenuItem>
            </FormaOverflowMenu>
        </ActionsTemplate>
    </FormaDataCards>
</FormaDataView>
```

A grid whose rows hold editors, links or controls its callers reach directly keeps the default,
`EveryItem`, and a row being edited keeps its editors in the Tab sequence either way.

`ItemAttributes` gives the element a view draws for an item attributes and handlers of the host's, a
drag source's among them, under the view's own, which win:

```razor
<FormaDataView Items="_entries" ItemKey="@(entry => entry.Id)"
               ItemAttributes="@(entry => new Dictionary<string, object>
               {
                   ["draggable"] = "true",
                   ["ondragstart"] = EventCallback.Factory.Create<DragEventArgs>(this, () => _dragged = entry),
               })">
    <FormaDataCards />
</FormaDataView>
```

## Inline editing

With `EnableInlineEditing` and a `CloneForEdit` that copies an item, the grid edits one row at a
time. `BeginEdit(item)` opens a row on its copy, each `FormaPropertyColumn` showing an editor for its
member's type; `SaveEditAsync()` validates the copy against its data annotations and raises
`OnRowSave` with it, and `CancelEditAsync()` discards it:

```razor
<FormaDataView Items="_employees" EnablePagination="false">
    <FormaDataGrid @ref="_grid" EnableInlineEditing="true" CloneForEdit="@(employee => employee with { })" OnRowSave="SaveAsync">
        <Columns>
            <FormaPropertyColumn Property="@(employee => employee.Name)" />
            <FormaPropertyColumn Property="@(employee => employee.StartDate)" Format="d" />
            <FormaTemplateColumn Title="Actions">
                @if (_grid?.IsEditing(context) == true)
                {
                    <FormaButton OnClick="() => _grid!.SaveEditAsync()">Save</FormaButton>
                    <FormaButton OnClick="() => _grid!.CancelEditAsync()">Cancel</FormaButton>
                }
                else
                {
                    <FormaButton OnClick="() => _grid!.BeginEdit(context)">Edit</FormaButton>
                }
            </FormaTemplateColumn>
        </Columns>
    </FormaDataGrid>
</FormaDataView>

@code {
    private FormaDataGrid<Employee>? _grid;
}
```

## Reading from a database

A queryable backed by a database is counted and read through the registered `IDataViewQueryExecutor`
when it `Supports` the query, so the work runs in the database asynchronously. With Entity Framework
Core:

```csharp
using Forma.Blazor.Components.DataView;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

public sealed class EntityFrameworkQueryExecutor : IDataViewQueryExecutor
{
    public bool Supports<TItem>(IQueryable<TItem> query)
        => query.Provider is IAsyncQueryProvider;

    public async ValueTask<int> CountAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken)
        => await query.CountAsync(cancellationToken);

    public async ValueTask<TItem[]> ToArrayAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken)
        => await query.ToArrayAsync(cancellationToken);
}
```

```csharp
builder.Services.AddSingleton<IDataViewQueryExecutor, EntityFrameworkQueryExecutor>();
builder.Services.AddFormaDataViewServices();
```

The data view's own strings are localized through the library's locale packs. The
[design document](https://github.com/FormaUI/Forma/blob/main/docs/data-view-design.md) describes the
engine, its views and the rules a view keeps.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
