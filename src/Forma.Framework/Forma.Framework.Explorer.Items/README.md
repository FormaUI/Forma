# Forma.Framework.Explorer.Items

The items a browsing surface lists: what an item and a folder are, the provider that answers a
surface one window of them at a time, the requests a surface hands back to its host, the ceilings
a host's collection keeps, and the bookkeeping every such surface repeats — the view over a held
collection, the window over a provider, the folder tree, the history and the selection. A host
writes its own items and its own provider on these contracts once, and every surface that
browses them reads the same ones.

`FormaExplorer` is the surface that speaks it today. A surface of its own — a gallery, a file
manager, a picker — builds on this package and on nothing of the explorer's, so two surfaces
share the model without either depending on the other.

## Install

```pwsh
dotnet add package 2Site.Forma.Framework.Explorer.Items
```

A host of `2Site.Forma.Blazor.Pro.Explorer` already has it, restored beside the component. Install
it directly in the project that implements items or a provider, which needs no component behind
it. The package references nothing from ASP.NET Core, so an item or a provider is written in a
plain class library, and it registers nothing.

## Namespaces

| Namespace | What it holds |
| --- | --- |
| `Forma.Framework.Explorer.Items` | The item model: `IExplorerItem` and `IExplorerFolderItem`, the ready `ExplorerItem` and `ExplorerFolderItem`, the capabilities and visibility of an item, and `ExplorerConstraints`, the ceilings a collection keeps. |
| `.Queries` | `IExplorerItemsProvider` and the query and page it exchanges, the sort field and direction, the resource filter, the view over a held collection and the window fetched from a provider. |
| `.Requests` | The records every change a reader asks for comes back to the host as, and the rejection a ceiling reports. |
| `.Navigation` | The folder tree indexed by key and by parent, and the history of folders visited. |
| `.Selection` | The selected items, one kind at a time, and what a surface's parts read of them. |

## What a caller implements

### Items

`IExplorerItem` is one item a surface lists. `IExplorerFolderItem` adds `ParentKey` and `Count`,
and narrows `Resource` to a `FolderResourceInfo`; a folder is a folder by implementing it, and
needs no resource behind it. The rules an implementation keeps:

- `Key` is unique among the items and unchanged when the item is reconstructed while refreshing,
  filtering, sorting or paging; the selection and the folder tree identify an item by it.
- `Capabilities` says what the item supports. What a surface permits is its own configuration,
  and an operation is offered only where both agree.
- `Resource` is what a thumbnail, a preview, the kind and size and the byte ceilings read, so an
  item with a resource is shown as the kind its `ResourceInfo` says it is.

`ExplorerItem` and `ExplorerFolderItem` implement both over init-only members, and
`ExplorerItem.For`, `ForAll` and `ForFolder` build them from resources with the capabilities each
kind supports.

### The provider

`IExplorerItemsProvider.GetItemsAsync(ExplorerItemQuery, CancellationToken)` answers one window.
`ExplorerItemQuery` carries the state a surface owns — `Skip`, `Take`, `Search`, `SortBy` and
`Direction` — and `ExplorerItemPage` carries the `Items` of the window and a `TotalCount`.
Whatever the query does not carry belongs to the implementation:

- The folder being browsed is the host's; the query never names it.
- The kinds accepted are applied by the provider itself. `ExplorerResourceFilter.Parse(pattern)`
  reads an accepted-kinds pattern, `globs:*.jpg;*.pdf` or `contentType:image;video`, and
  `Accepts(resource)` answers for one resource.
- `TotalCount` counts every item that matches, hidden ones included, or is null when the store
  cannot say without counting; a window shorter than `Take` ends the collection.
- `ExplorerItemPage.Empty` is a window of nothing with a total of zero.

## What a caller constructs

`ExplorerConstraints` is an init-only record of the ceilings a host's collection keeps — the
resource kinds accepted, the most top-level folders, subfolders, nesting depth, items per folder,
items and bytes — with `ExplorerConstraints.None` keeping none. Each ceiling pairs with an
`ExplorerRejectionReason`, and a refusal comes back as an `ExplorerRejection` of the `Reason`, the
`Allowed` ceiling where the reason is a count, and a `Message` written in the reader's language.

## What a surface reports

Every change a reader asks for is a record in `Forma.Framework.Explorer.Items.Requests` carrying
the host's own items: `ExplorerRenameRequest`, `ExplorerDeleteRequest`, `ExplorerDropRequest`,
`ExplorerFolderCreateRequest`, `ExplorerFolderRenameRequest`, `ExplorerFolderDeleteRequest` and
`ExplorerFolderDropRequest`. Which callback of a surface raises which is tabled in that surface's
README.

## What a surface keeps

| Type | What it is |
| --- | --- |
| `ExplorerCollectionView` | The view over a held collection: filtered by kind, searched by name, sorted, and cut to the visible page. |
| `ExplorerProviderWindow` | The window fetched from a provider one page at a time, with the fetch itself in flight; a superseded fetch is abandoned rather than raced. |
| `ExplorerFolderTree` | The folder items indexed by key and by parent. |
| `ExplorerHistory` | The trail of folders visited, with a cursor moving back and forward along it. |
| `ExplorerSelection` | The selected items, one kind at a time, capped by a limit; `IExplorerSelectionView` is what a surface's parts read of it. |

## Example

A host's own catalog entry as an item, with what it supports decided by the entry:

```csharp
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

public sealed class PhotoItem(Photo photo) : IExplorerItem
{
    public string Key => photo.Id;

    public string Name => photo.FileName;

    public ExplorerItemCapabilities Capabilities => photo.IsLocked
        ? ExplorerItemCapabilities.View
        : ExplorerItemCapabilities.View
            | ExplorerItemCapabilities.Drag
            | ExplorerItemCapabilities.Move
            | ExplorerItemCapabilities.Rename
            | ExplorerItemCapabilities.Delete;

    public ExplorerItemVisibility Visibility => ExplorerItemVisibility.Visible;

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

A provider answering windows of those items from a catalog that lists without counting, so the
total is null and a surface keeps asking while windows come back full:

```csharp
using Forma.Framework.Explorer.Items.Queries;

public sealed class PhotoItemsProvider(IPhotoCatalog catalog, Func<string?> currentFolderKey) : IExplorerItemsProvider
{
    public async ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken)
    {
        var photos = await catalog.ListAsync(
            currentFolderKey(), query.Search, query.SortBy, query.Direction, query.Skip, query.Take, cancellationToken);

        return new ExplorerItemPage([.. photos.Select(photo => new PhotoItem(photo))], TotalCount: null);
    }
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
