# Forma.Blazor.Explorer

`FormaExplorer`, a file manager over what a site already holds, for the **Forma** component
library: a folder tree, an address bar with history and breadcrumbs, thumbnail, details and list
views, search, sorting and grouping, selection, drag and drop between folders, upload, and a
preview of any item. It stores nothing itself: the host supplies the items and folders, every
change the reader asks for comes back to the host as a request to carry out, and an upload goes
into the host's `IFileStore`. A host that already renders `Forma.Blazor` adds this package and
nothing else changes: the same `AddFormaUI()` registers what it needs, the same namespaces reach
it, and the same locale file layout localizes it.

**.NET 11 · Interactive Server components.**

## Install

```pwsh
dotnet add package 2Site.Forma.Blazor.Pro.Explorer
```

The package depends on `2Site.Forma.Blazor`, the product, and on
`2Site.Forma.Framework.Catalog`, which install beside it. The component lives in
`Forma.Blazor.Components.Explorer`. The items it lists, the provider that answers it, the requests it
raises and the ceilings it keeps are the items package's, under `Forma.Framework.Catalog`,
so a host writes them once for every surface that browses them; the explorer's own vocabulary — its
options and what its parts read — ships inside the package under `Forma.Framework.Explorer`.

## Register

The component has no registrar of its own. `AddFormaUI()` registers everything it needs, and the
component's assembly is listed beside the product's when components are mapped:

```csharp
using Forma.Blazor;
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.Services.AddFormaUI();
builder.Services.AddScoped<IFileStore, UploadFolderStore>();

var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(Forma.Blazor._Imports).Assembly,
        typeof(Forma.Blazor.Components.Explorer._Imports).Assembly);
```

The `IFileStore` is where uploads go. The explorer takes it from its `Store` parameter, or from the
host's registration when the parameter is left empty; a host with no store turns `AllowUpload` off
in its item options. The preview is `FormaFileViewer`, so any `IResourceProvider` the host
registered for it applies here too. The store, the providers, and the resource records the
explorer's items carry are documented in
[`Forma.Framework.Resources`](../../Forma.Framework/Forma.Framework.Resources/README.md).

## What a host supplies

### Items and folders

The explorer lists `ICatalogItem`s. A folder is an `ICatalogFolderItem`, placed in the tree by
the key of its parent, so a folder needs no resource behind it to be a folder.

| Member | What it is |
| --- | --- |
| `Key` | The stable identifier, unique among the explorer's items and unchanged across refreshing, sorting and paging. |
| `Name` | The displayed name. |
| `Capabilities` | The operations the item itself supports, as `CatalogItemCapabilities` flags. |
| `Visibility` | `Visible`, `Hidden` (leaves the listing and every count), or `Invisible` (keeps its place, undrawn). |
| `IsDisabled` | Whether the item is drawn muted, offers no operation, and cannot be selected. |
| `CreatedAt`, `ModifiedAt` | When the item was created and last changed, or null when unknown. |
| `ThumbnailSource` | The address of the item's thumbnail, or null when it has none. |
| `Resource` | The `ResourceInfo` behind the item, or null; an image resource is drawn as its picture and any other as the glyph of its kind, and the preview, the kind and size columns and the byte ceilings read it. |
| `ParentKey` | On a folder, the key of the folder it sits inside, or null at the top level. |
| `Count` | On a folder, how many items it holds, or null when unknown. |

`CatalogItem` and `CatalogFolderItem` are ready implementations with init-only members, and
`CatalogItem.For(resource)`, `ForAll(resources)` and `ForFolder(folder)` wrap a `ResourceInfo`,
a list of them, or a `FolderResourceInfo`, deriving the capabilities each kind supports.

`Folders` takes the folder items; the sidebar tree, the breadcrumbs and the drop targets are built
from them. `CurrentFolder` is the host's: the tree, the breadcrumbs, back, forward and up all
raise `OnFolderChanged` with the folder the reader chose, null for the root, and the host sets
`CurrentFolder` to it. The explorer never filters by folder, so the host hands over the items of
the folder it is showing.

### Where the items come from

Exactly one of two parameters supplies the items:

| Parameter | What it takes | Right for |
| --- | --- | --- |
| `Items` | The whole collection, which the explorer searches, sorts and pages itself. | A few hundred items. |
| `ItemsProvider` | An `ICatalogProvider` answering one window at a time; the explorer holds only what it shows. | A library too large to enumerate. |

A provider's `GetItemsAsync(CatalogQuery, CancellationToken)` receives what the explorer
owns — `Skip`, `Take`, `Search`, `SortBy` and `Direction` — and returns an `CatalogPage` of
the items and a `TotalCount`. The folder is not in the query, because the host already has it. A
store that cannot count cheaply returns a null total, which leaves the explorer without a page
count; infinite scrolling keeps asking for as long as windows come back full.

With a provider, `TotalCount` and `TotalBytes` tell the explorer how much the whole library holds,
so the ceilings below are measured against the library rather than the window; with `Items`, the
supplied items are counted and measured.

### What the explorer reports

Every callback hands back the items the host supplied. The explorer changes nothing itself: the
host carries a request out and supplies the items again, or, behind a provider, the explorer
re-reads its window on its own once a rename, deletion, drop or upload is reported.

| Callback | Carries | Raised when |
| --- | --- | --- |
| `OnFolderChanged` | `ICatalogFolderItem?` | The reader navigates; null is the root. |
| `OnRename` | `CatalogRenameRequest` (`Item`, `NewName`) | A rename is confirmed. |
| `OnDelete` | `CatalogDeleteRequest` (`Items`) | A deletion is confirmed, of one item or the selection. |
| `OnDrop` | `CatalogDropRequest` (`Items`, `TargetFolder`) | Items land on a folder; dragging a selected item moves the whole selection, and a null target is the root. |
| `OnFolderCreate` | `CatalogFolderCreateRequest` (`ParentFolder`, `Name`) | A new folder is confirmed. |
| `OnFolderRename` | `CatalogFolderRenameRequest` (`Folder`, `NewName`) | A folder rename is confirmed. |
| `OnFolderDelete` | `CatalogFolderDeleteRequest` (`Folder`) | A folder deletion is confirmed, once per folder. |
| `OnFolderDrop` | `CatalogFolderDropRequest` (`SourceFolder`, `TargetFolder`) | A folder lands on another, or on the root. |
| `OnUploaded` | `IReadOnlyList<ResourceInfo>` | The store has taken the uploaded files. |
| `OnRejected` | `CatalogRejection` (`Reason`, `Allowed`, `Message`) | A ceiling refused an upload, a folder or a drop. |
| `OnRefresh` | — | The refresh action is pressed. |
| `SelectedChanged` | `IReadOnlyList<ICatalogItem>` | The selection changes; pairs with `Selected` as `@bind-Selected`. |
| `OnPageChanged` | `int` | The page changes; pairs with `CurrentPage`. |
| `ViewModeChanged`, `GroupFieldChanged`, `DetailsColumnsChanged`, `DetailsHeaderVisibleChanged` | The new value | The reader changes the presentation, for a host that remembers it. |

## What the explorer offers

### Views, sorting and grouping

`ViewMode` opens the explorer in `Thumbnails`, `Details` or `List`, and the toolbar switches
between them. `SortField` orders by `Name`, `Size`, `Kind`, `DateAdded` or `DateModified` in the
`SortDirection` given; the sort menu and the details view's column headers change both.
`GroupField` gathers the items under collapsible headings by the same fields, or `None`. The
details view opens with the optional columns named in `Options.DetailsColumns` — `Kind`, `Size`,
`Modified`, `Added` — and its header shown by `Options.ShowDetailsHeader`; the columns menu changes
both afterwards. Every configured column stays, and the pane scrolls sideways when they do not fit.

### Search and paging

The search box filters a held collection as the reader types, and asks a provider once typing
pauses for `Options.SearchDebounceDelay`; `SearchPlaceholder` replaces the box's own text. The
collection is cut into pages of `PageSize`, reached through a pager, or appended as the reader
reaches the end when `Options.InfiniteScroll` is `Manual`, through a button, or `Automatic`.

### Selection

The toolbar's Select toggle enters selection mode, where each item shows a checkbox and a selection
bar carries the count, delete and clear. `Options.MaxSelection` caps the selection, zero for no
cap, and `1` makes each choice replace the last. Choosing a folder clears selected items and
choosing an item clears selected folders, so a selection holds one kind at a time. A caller that
exists only to choose sets `Options.SelectOnOpen`, which opens in selection mode with no way out.

### Operations

`ItemOptions` says what this explorer permits: `AllowedCapabilities` on items,
`AllowedFolderCapabilities` on folders, and `AllowUpload`. What an item offers is what it
supports and the explorer permits, and nothing while it is disabled. `ExplorerItemOptions.Default`
permits everything; `ReadOnly` leaves browsing and selection and removes upload, rename, delete
and every folder action.

| Capability | What it allows |
| --- | --- |
| `Open` | A folder can be entered. |
| `View` | An item with a resource opens in the preview. |
| `Drag`, `Drop` | The item can be picked up; a folder can receive what is dropped. |
| `Move` | The item can be dropped into another folder. |
| `Rename`, `Delete` | The item's action bar and the selection bar offer the operation. |
| `Create` | A folder can hold a new folder. |

`Read`, `Write` and `Execute` are reserved and offer nothing.

### Upload and preview

With `AllowUpload` on, the toolbar opens a panel where the reader picks images, video and audio,
and `FormaFileUploader` moves them into the store; the panel closes and `OnUploaded` reports the
resources once the store has them. Activating an item with a resource opens it in `FormaFileViewer`
inside a dialog, stepping through the other items in view.

### Constraints

`Constraints` names the ceilings the explorer refuses to cross, each unlimited at zero. A refusal
is announced under the header and reported through `OnRejected`, whose `Reason` is one of
`CatalogRejectionReason`.

| Ceiling | What it limits |
| --- | --- |
| `AcceptedResourceKinds` | Which files may be added, as `globs:*.jpg;*.pdf` or `contentType:image;video`; anything when null. |
| `MaxFolderDepth` | How deep folders nest, a top-level folder counting as one. |
| `MaxFolderRootCount` | How many folders sit at the top level. |
| `MaxSubfolderCount` | How many folders sit directly inside another. |
| `MaxFolderItemCount` | How many items one folder holds. |
| `MaxItemCount` | How many items the explorer holds in all. |
| `MaxTotalBytes` | How many bytes the explorer holds in all. |

### Presentation

`Options` is an `ExplorerOptions`, `ExplorerOptions.Default` unless given.

| Option | What it does |
| --- | --- |
| `ShowSidebar` | Shows the folder tree, when `Folders` has any; off, the breadcrumbs are the way between folders. |
| `ShowFolderMenu` | Gives each breadcrumb a menu of that folder's children. |
| `Resizable` | Lets the reader resize the explorer. |
| `AsDialog` | Presents the explorer as dialog content, without a frame of its own. |
| `SelectOnOpen`, `MaxSelection` | The selection settings above. |
| `InfiniteScroll`, `SearchDebounceDelay` | The paging and search settings above. |
| `DetailsColumns`, `ShowDetailsHeader` | The details view's opening columns and header. |

## Example

A page over a catalog the host owns, handing the explorer the items of the folder it is showing
and carrying out what the reader asks:

```razor
@page "/media"
@rendermode InteractiveServer
@using Forma.Blazor.Components.Explorer
@using Forma.Framework.Catalog
@using Forma.Framework.Catalog
@using Forma.Framework.Catalog.Requests
@using Forma.Framework.Resources

<FormaExplorer Items="_items"
               Folders="_folders"
               CurrentFolder="_currentFolder"
               OnFolderChanged="HandleFolderChangedAsync"
               OnRename="HandleRenameAsync"
               OnDelete="HandleDeleteAsync"
               OnDrop="HandleDropAsync"
               OnUploaded="_ => LoadAsync()"
               @bind-Selected="_selected" />

@code {
    [Inject] private IMediaCatalog Catalog { get; set; } = default!;

    private IReadOnlyList<ICatalogItem> _items = [];
    private IReadOnlyList<ICatalogItem> _folders = [];
    private IReadOnlyList<ICatalogItem> _selected = [];
    private ICatalogFolderItem? _currentFolder;

    protected override Task OnInitializedAsync()
        => LoadAsync();

    private async Task LoadAsync()
    {
        _folders = CatalogItem.ForAll(await Catalog.GetFoldersAsync());
        _items = CatalogItem.ForAll(await Catalog.GetFilesAsync(_currentFolder?.Key));
    }

    private Task HandleFolderChangedAsync(ICatalogFolderItem? folder)
    {
        _currentFolder = folder;

        return LoadAsync();
    }

    private async Task HandleRenameAsync(CatalogRenameRequest request)
    {
        await Catalog.RenameAsync(request.Item.Key, request.NewName);
        await LoadAsync();
    }

    private async Task HandleDeleteAsync(CatalogDeleteRequest request)
    {
        await Catalog.DeleteAsync(request.Items.Select(item => item.Key));
        await LoadAsync();
    }

    private async Task HandleDropAsync(CatalogDropRequest request)
    {
        await Catalog.MoveAsync(request.Items.Select(item => item.Key), request.TargetFolder?.Key);
        await LoadAsync();
    }
}
```

The same page over a library too large to hold, answering one window at a time from the folder
the page knows:

```csharp
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog;

public sealed class CatalogItemsProvider(IMediaCatalog catalog, Func<string?> currentFolder) : ICatalogProvider
{
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        var page = await catalog.SearchAsync(
            currentFolder(), query.Search, query.SortBy, query.Direction, query.Skip, query.Take, cancellationToken);

        return new CatalogPage(CatalogItem.ForAll(page.Files), page.Total);
    }
}
```

```razor
@using Forma.Framework.Explorer
@using Forma.Framework.Catalog.Queries

<FormaExplorer ItemsProvider="_provider"
               Folders="_folders"
               CurrentFolder="_currentFolder"
               TotalCount="_libraryCount"
               PageSize="48"
               Options="@(new ExplorerOptions { InfiniteScroll = ExplorerInfiniteScrollMode.Automatic })"
               OnFolderChanged="folder => _currentFolder = folder" />

@code {
    private ICatalogProvider _provider = default!;
    private int _libraryCount;

    protected override async Task OnInitializedAsync()
    {
        _provider = new CatalogItemsProvider(Catalog, () => _currentFolder?.Key);
        _folders = CatalogItem.ForAll(await Catalog.GetFoldersAsync());
        _libraryCount = await Catalog.CountAsync();
    }
}
```

## License

`FormaExplorer` is part of Forma Pro, which is licensed under the Forma Pro Community License
rather than the MIT license the rest of Forma carries. Individuals, and organizations within the
license's eligibility, use it at no charge; an organization beyond that takes a commercial license.
See [`LICENSE.md`](../LICENSE.md) and [`COMMERCIAL.md`](../COMMERCIAL.md).
