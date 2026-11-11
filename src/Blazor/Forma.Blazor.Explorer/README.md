# Forma.Blazor.Explorer

`FormaExplorer`, a file manager for the **Forma** component library over items and folders the host
already holds: a folder tree, an address bar with history and breadcrumbs, thumbnail, details and
list views, search, sorting and grouping, selection, drag and drop between folders, upload, and a
preview of any item. The host supplies the items and carries out each change the reader asks for,
and uploads go into the host's `IFileStore`.

**.NET 11 · Blazor components.**

## Install

```pwsh
dotnet add package formaui-net.Blazor.Explorer --prerelease
dotnet add package formaui-net.Framework.Localization.Json --prerelease
```

The package depends on `formaui-net.Blazor` and `formaui-net.Framework.Catalog`, which restore
beside it. The component is in the `Forma.Blazor.Components.Explorer` namespace; the items, provider
and requests it works with are
[`Forma.Framework.Catalog`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Catalog/README.md)'s,
and its options are
[`Forma.Framework.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Explorer/README.md)'s,
which comes with this package.

## Register

`AddFormaServerIntegration()` registers what the component needs. Register an `IFileStore` for uploads, and list the
component's assembly beside the library's when mapping components:

```csharp
using Forma.Blazor;
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaServerIntegration();
builder.Services.AddScoped<IFileStore, MediaFileStore>();

var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(Forma.Blazor._Imports).Assembly,
        typeof(Forma.Blazor.Components.Explorer._Imports).Assembly);
```

The `Store` parameter takes a store for one explorer in place of the registered one. An explorer
without a store sets `AllowUpload` to false in its `ItemOptions`. The preview is `FormaFileViewer`,
so any `IResourceProvider` the host registers applies to it too; both contracts are described in
[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Resources/README.md).

## Use

A page hands the explorer the items of the folder it is showing, the folders for the tree, and a
handler for each change it carries out:

```razor
@page "/media"
@rendermode InteractiveServer
@using Forma.Blazor.Components.Explorer
@using Forma.Framework.Catalog
@using Forma.Framework.Catalog.Requests
@inject IMediaLibrary Library

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
    private IReadOnlyList<ICatalogItem> _items = [];
    private IReadOnlyList<ICatalogItem> _folders = [];
    private IReadOnlyList<ICatalogItem> _selected = [];
    private ICatalogFolderItem? _currentFolder;

    protected override Task OnInitializedAsync()
        => LoadAsync();

    private async Task LoadAsync()
    {
        _folders = CatalogItem.ForAll(await Library.GetFoldersAsync());
        _items = CatalogItem.ForAll(await Library.GetFilesAsync(_currentFolder?.Key));
    }

    private Task HandleFolderChangedAsync(ICatalogFolderItem? folder)
    {
        _currentFolder = folder;

        return LoadAsync();
    }

    private async Task HandleRenameAsync(CatalogRenameRequest request)
    {
        await Library.RenameAsync(request.Item.Key, request.NewName);
        await LoadAsync();
    }

    private async Task HandleDeleteAsync(CatalogDeleteRequest request)
    {
        await Library.DeleteAsync(request.Items.Select(item => item.Key));
        await LoadAsync();
    }

    private async Task HandleDropAsync(CatalogDropRequest request)
    {
        await Library.MoveAsync(request.Items.Select(item => item.Key), request.TargetFolder?.Key);
        await LoadAsync();
    }
}
```

`CatalogItem.ForAll` wraps the host's `ResourceInfo` records as items, a `FolderResourceInfo`
becoming a folder placed in the tree by its `Parent`. A host's own entities can implement
`ICatalogItem` and `ICatalogFolderItem` instead.

## Items or a provider

Exactly one of two parameters supplies the items:

| Parameter | What it takes | Suits |
| --- | --- | --- |
| `Items` | The whole collection, which the explorer searches, sorts and pages itself. | A few hundred items. |
| `ItemsProvider` | An `ICatalogProvider` answering one slice at a time. | A library too large to hold. |

With a provider, `TotalCount` and `TotalBytes` give the size of the whole library, which the
constraints are measured against, and the explorer reads its items again after a rename, deletion,
drop or upload. The explorer asks the provider for files alone, through the query's `Kinds`, and
lists the `Folders` the host gives before them. `_provider` below is an `ICatalogProvider` such as the one the catalog's README
writes:

```razor
@using Forma.Framework.Explorer
@using Forma.Framework.Explorer.Content.Queries

<FormaExplorer ItemsProvider="_provider"
               Folders="_folders"
               CurrentFolder="_currentFolder"
               TotalCount="_libraryCount"
               PageSize="48"
               Options="@(ExplorerOptions.Default with { InfiniteScroll = ExplorerInfiniteScrollMode.Automatic })"
               OnFolderChanged="folder => _currentFolder = folder" />
```

## Callbacks

Every callback carries the items the host supplied. The host carries the request out and supplies
the items again. A deletion, a drop and a rename take files and folders alike, a folder told apart
by `ICatalogFolderItem`, and a host that could not carry one out calls its `Fail()`, which keeps the
selection as it was.

| Callback | Carries | Raised when |
| --- | --- | --- |
| `OnFolderChanged` | `ICatalogFolderItem?` | The reader navigates; null is the root. The host sets `CurrentFolder` to it. |
| `OnFileActivated` | `ICatalogItem` | A file is activated, by a press or Enter, in place of opening its preview. |
| `OnRename` | `CatalogRenameRequest` | A rename of a file or a folder is confirmed. |
| `OnDelete` | `CatalogDeleteRequest` | A deletion of one item or the selection, files and folders together, is confirmed. |
| `OnDrop` | `CatalogDropRequest` | Items, files and folders together, land on a folder, or on the root. |
| `OnFolderCreate` | `CatalogFolderCreateRequest` | A new folder is confirmed. |
| `OnUploaded` | `IReadOnlyList<ResourceInfo>` | The store has taken the uploaded files. |
| `OnRejected` | `CatalogRejection` | A constraint refused an upload, a folder or a drop, or an item may not be deleted or moved. |
| `OnRefresh` | — | The refresh action is pressed. |
| `SelectedChanged` | `IReadOnlyList<ICatalogItem>` | The selection changes; `@bind-Selected` pairs it with `Selected`. |
| `OnPageChanged` | `int` | The page changes; pairs with `CurrentPage`. |
| `ViewModeChanged`, `GroupFieldChanged`, `DetailsColumnsChanged`, `DetailsHeaderVisibleChanged` | The new value | The reader changes the presentation. |

## Configuring

`ViewMode` opens the explorer in `Thumbnails`, `Details` or `List`. `SortField` and `SortDirection`
set the order, by `Name`, `Size`, `Kind`, `DateAdded` or `DateModified`, and `GroupField` gathers
the items under collapsible headings by the same fields. `PageSize` sets the page, and
`SearchPlaceholder` the search box's text.

`Options` is an `ExplorerOptions`:

| Option | What it does |
| --- | --- |
| `ShowSidebar` | Shows the folder tree. |
| `ShowFolderMenu` | Gives each breadcrumb a menu of that folder's children. |
| `Resizable` | Lets the reader resize the explorer. |
| `AsDialog` | Presents the explorer as dialog content. |
| `SelectOnOpen` | Opens in selection mode and stays in it, for an explorer used to choose items. |
| `MaxSelection` | Caps the selection; zero is unlimited, and one makes each choice replace the last. |
| `InfiniteScroll` | `Off` pages; `Manual` appends the next page from a button, and `Automatic` as the reader reaches the end. |
| `DetailsColumns`, `ShowDetailsHeader` | The details view's opening columns, of `Kind`, `Size`, `Modified` and `Added`, and its header. |
| `SearchDebounceDelay` | How long typing pauses before a provider is asked. |

`ItemOptions` is an `ExplorerItemOptions` of the operations permitted: `AllowedCapabilities` on
items, `AllowedFolderCapabilities` on folders, and `AllowUpload`. An item offers an operation when
it supports it and the options permit it. `ExplorerItemOptions.Default` permits every operation,
and `ExplorerItemOptions.ReadOnly` permits browsing and selection.

`Constraints` is a `CatalogConstraints` of the kinds the explorer lists and accepts, the kinds an
upload may add where they differ, and the ceilings it keeps, each unlimited at zero; a refusal is
announced in the explorer and raised through `OnRejected`. A folder can be chosen only where the
accepted kinds name it, `contentType:image;folder`, and is entered otherwise, so an explorer that
chooses images lets the reader into every folder to reach them:

```razor
<FormaExplorer Items="_items"
               Constraints="@(new CatalogConstraints { AcceptedResourceKinds = "contentType:image;video", MaxFolderDepth = 3 })"
               OnRejected="rejection => _message = rejection.Message" />
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
