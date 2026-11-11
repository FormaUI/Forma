# Forma.Framework.Catalog

The model of a browsable collection of items and folders: the item and folder contracts, the
provider that answers one slice of them at a time, the requests a surface hands back when the reader
renames, deletes or moves something, and the ceilings a collection keeps. A host writes its items and
its provider against these contracts once, and every surface that browses a catalog lists them.

## Install

```pwsh
dotnet add package formaui-net.Framework.Catalog --prerelease
```

The package builds on `Forma.Framework.Resources` alone, so items and providers can live in a plain
class library. Its types are used directly, without registration.

## Items

`ICatalogItem` is one item, and `ICatalogFolderItem` is a folder, adding `ParentKey` and `Count`.

| Member | What it is |
| --- | --- |
| `Key` | The stable identifier, unique among the items and the same each time the item is rebuilt. |
| `Name` | The displayed name. |
| `Capabilities` | The operations the item supports, as `CatalogItemCapabilities` flags: `Open`, `View`, `Drag`, `Drop`, `Move`, `Rename`, `Delete`, `Create` and more. |
| `Visibility` | `Visible`, `Hidden` (left out of the listing and its counts), or `Invisible` (keeps its place, undrawn). |
| `IsDisabled` | Whether the item is listed with its operations unavailable. |
| `CreatedAt`, `ModifiedAt` | When the item was created and last changed. |
| `ThumbnailSource` | The address of the item's thumbnail. |
| `Resource` | The `ResourceInfo` the item wraps, which a thumbnail, a preview and the kind and size read. |
| `ParentKey` | On a folder, the key of the folder it sits inside; null is the top level. |
| `Count` | On a folder, how many items it holds. |

`GetKind` reads what an item is as a `CatalogItemKind`, `Folder`, `Image`, `Video`, `Audio`, `Archive`,
`Document`, `File` or `Unknown`, the same in every culture.

`CatalogItem` and `CatalogFolderItem` implement both with init-only members. `CatalogItem.For`,
`ForAll` and `ForFolder` build them from resources, with the capabilities each kind supports:

```csharp
using Forma.Framework.Catalog;

IReadOnlyList<ICatalogItem> folders = CatalogItem.ForAll(await store.GetFoldersAsync());
IReadOnlyList<ICatalogItem> files = CatalogItem.ForAll(await store.GetFilesAsync(folderKey));
```

A host's own entity can implement `ICatalogItem` itself, deciding what each entry supports:

```csharp
using Forma.Framework.Catalog;
using Forma.Framework.Resources;

public sealed class PhotoItem(Photo photo) : ICatalogItem
{
    public string Key => photo.Id;

    public string Name => photo.FileName;

    public CatalogItemCapabilities Capabilities => photo.IsLocked
        ? CatalogItemCapabilities.View
        : CatalogItemCapabilities.View | CatalogItemCapabilities.Drag | CatalogItemCapabilities.Move
            | CatalogItemCapabilities.Rename | CatalogItemCapabilities.Delete;

    public CatalogItemVisibility Visibility => CatalogItemVisibility.Visible;

    public bool IsDisabled => photo.IsProcessing;

    public DateTimeOffset? CreatedAt => photo.UploadedAt;

    public DateTimeOffset? ModifiedAt => photo.EditedAt;

    public string? ThumbnailSource => photo.ThumbnailUrl;

    public ResourceInfo? Resource { get; } = new ImageResourceInfo(photo.Url)
    {
        ContentType = photo.ContentType,
        Size = photo.Bytes,
        Width = photo.Width,
        Height = photo.Height,
    };
}
```

## Providers

A collection too large to hold in memory is answered one slice at a time by an `ICatalogProvider`.
`GetItemsAsync` receives a `CatalogQuery` of `FolderKey`, `Skip`, `Take`, `Search`, `SortBy` and
`Direction`, and returns a `CatalogPage` of the slice's `Items` and a `TotalCount`. `FolderKey` names
the folder the slice was asked for when it was asked, null for every folder's items, so a provider
answers from the query alone. Items the sort ties stand in the ascending ordinal order of their keys,
in either direction, so neighboring slices neither repeat nor skip one.

`Skip` and `Take` count the store's positions, and `TotalCount` how many positions the query
matched, so a slice holding a hidden item, or one the surface's filter rejects, still moves the next
slice on by every position it read. A page whose items stand for more positions than it returns,
because the provider left some out itself, says how many it read in `ReadCount`. The query's
`Filter` names the kinds the surface lists, for a store that applies it before paging; one that does,
and leaves hidden items out too, gives the count of what the whole query lists in `ListedCount`,
which is the only count a surface shows as how many items there are. A null `TotalCount` suits a
store that cannot count cheaply: `HasMore` then says whether another slice follows, and without it a
slice that read fewer positions than `Take` ends the collection.

A provider that lists folders lists them as `ICatalogFolderItem`s whose `ParentKey` places them, the
folder key of every item in them; a flat catalog lists none. The query's `Kinds` names the kinds of
item it asks for, null asking for every kind and an empty set for none, and `AsksFor` answers for one.
A provider applies them before it pages, so the slice, `ReadCount`, `TotalCount`, `ListedCount` and
`HasMore` all describe the items of those kinds alone: a surface asks a provider for its folders
alone, or for its files alone, and pages through either. A caller can leave an item of another kind
out of an answer, but its counts and positions stay wrong for a provider that ignored the kinds.

```csharp
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;

public sealed class PhotoItemsProvider(IPhotoStore store) : ICatalogProvider
{
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        var photos = await store.ListAsync(
            query.FolderKey,
            query.AsksFor(CatalogItemKind.Folder),
            query.AsksFor(CatalogItemKind.Image),
            query.Search, query.SortBy, query.Direction, query.Skip, query.Take, cancellationToken);

        return new CatalogPage([.. photos.Select(photo => new PhotoItem(photo))], TotalCount: null);
    }
}
```

`ReadAllAsync` reads every item a query asks for, a slice of `Take` positions at a time from `Skip`
on, moving past a slice that holds no item by the positions it read and stopping where a slice says
none follow, which suits a surface that needs a whole list, such as every folder, at the cost of
reading all of it:

```csharp
var everyFolder = new CatalogQuery(null, 0, 200, null, CatalogSortField.Name, CatalogSortDirection.Ascending)
{
    Kinds = new HashSet<CatalogItemKind> { CatalogItemKind.Folder },
};

var folders = await provider.ReadAllAsync(everyFolder, cancellationToken);
```

A query's `Grouping` gathers the items into groups that order them before the sort does, so a group
whose items span two slices is one group in both. It names the field, `CatalogGroupField`, and the
groups in the order they stand, each a key the same in every culture and on every day and a rule a
store can apply without the reader's culture or clock: `CatalogNameGroup` for the names beginning
with a range of characters, `CatalogSizeGroup` and `CatalogDateGroup` for a range of sizes or of
instants, `CatalogKindGroup` for one kind, `CatalogMissingGroup` for the items holding no size or no
date, and `CatalogRemainderGroup` for whatever the others leave. An item falls in the first group that
admits it, and items stand in their groups' order, then the sort's, then their keys'. A query with
`IncludesGroupSizes` asks the page for how many items each group holds across the whole query, in
`GroupSizes`; `CatalogGrouping.CountGroups` counts them for a provider that holds its items.

`CatalogResourceFilter.Parse` reads an accepted-kinds pattern, `globs:*.jpg;*.pdf` or
`contentType:image;video`, and its `Accepts` answers for one resource. A glob names files; a content
type of `folder` names folders, and `AcceptsFolders` says whether a filter takes them.

## Requests and constraints

Each change the reader asks a surface for comes back to the host as a request in
`Forma.Framework.Catalog.Requests`, carrying the host's own items for the host to carry out. A rename,
a deletion and a drop take files and folders alike, and a host that could not carry one out calls its
`Fail()`, which the surface reads once the host has answered:

| Request | Carries |
| --- | --- |
| `CatalogRenameRequest` | `Item`, `NewName` |
| `CatalogDeleteRequest` | `Items` |
| `CatalogDropRequest` | `Items`, `TargetFolder` (null is the top level) |
| `CatalogFolderCreateRequest` | `ParentFolder`, `Name` |

`CatalogConstraints` holds the kinds a collection lists and accepts, `AcceptedResourceKinds`, the
kinds an upload may add where they differ, `AcceptedUploadKinds`, and the ceilings it keeps, each
unlimited at zero: `MaxFolderDepth`, `MaxFolderRootCount`, `MaxSubfolderCount`,
`MaxFolderItemCount`, `MaxItemCount` and `MaxTotalBytes`. A surface reports a refusal as a
`CatalogRejection` of its `Reason`, the `Allowed` ceiling and a localized `Message`.

```csharp
var constraints = new CatalogConstraints
{
    AcceptedResourceKinds = "contentType:image;video",
    MaxFolderDepth = 3,
    MaxTotalBytes = 500L * 1024 * 1024,
};
```

## Building a surface

A component that browses a catalog keeps its state in the types this package provides:

| Type | What it is |
| --- | --- |
| `CatalogCollectionView` | A held collection filtered by kind, searched by name, sorted and cut to the visible page. |
| `CatalogListProvider` | An `ICatalogProvider` answering from a list it holds, shaped as a `CatalogCollectionView` shapes it, so a held collection and a store answer alike. |
| `CatalogProviderItems` | The items fetched from a provider, one slice replacing the last or slices appended, with the store positions read, whether more follow and the fetch in flight. |
| `CatalogFolderTree` | The folder items indexed by key and by parent. |
| `CatalogHistory` | The folders visited, with a cursor moving back and forward. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
