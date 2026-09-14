# Forma.Framework.Explorer

The vocabulary `FormaExplorer` speaks: the items and folders it lists, the provider that answers
it one window of them at a time, the options and constraints that shape one explorer, the
requests it hands back to its host, and the state the parts drawing it read. A host writes its
own items and its own provider on these contracts.

The assembly ships inside `2Site.Forma.Blazor.Pro.Explorer`; nothing is installed or registered
for it. It references nothing from ASP.NET Core, so an item or a provider is written in a plain
class library. How the component itself is consumed — its parameters, its callbacks and the
registrations it reads — is in
[`Forma.Blazor.Explorer`](../../Forma.Blazor.Pro/Forma.Blazor.Explorer/README.md).

## Namespaces

| Namespace | What it holds |
| --- | --- |
| `Forma.Framework.Explorer` | `ExplorerOptions` and `ExplorerConstraints`, and `IExplorerView` and `IExplorerInteraction`, the whole of what the parts drawing an explorer read and ask. |
| `.Items` | The item model: `IExplorerItem` and `IExplorerFolderItem`, the ready `ExplorerItem` and `ExplorerFolderItem`, the capabilities, visibility and options of an item, and the actions, drag state and pending confirmations shared by every region listing items. |
| `.Content` | How the listed items are shown: the view mode, the details columns, the grouping bands, the shaped view over a held collection, and the preview. |
| `.Content.Queries` | `IExplorerItemsProvider` and the query and page it exchanges, the sort, group and infinite-scroll choices, the resource filter, and the window fetched from a provider. |
| `.Requests` | The records every change the reader asks for comes back to the host as, and the rejection a ceiling reports. |
| `.Navigation` | The location: the indexed folder tree, the history, and what the address bar and the sidebar read and ask. |
| `.Selection` | The selected items and the rules a further choice follows. |
| `.Actions` | The confirmations awaiting the reader: a rename, a new folder, a deletion. |

## What a caller implements

### Items

`IExplorerItem` is one item the explorer lists. `IExplorerFolderItem` adds `ParentKey` and
`Count`, and narrows `Resource` to a `FolderResourceInfo`; a folder is a folder by implementing
it, and needs no resource behind it. The members and what each does are tabled in the
component's README. The rules an implementation keeps:

- `Key` is unique among the explorer's items and unchanged when the item is reconstructed while
  refreshing, filtering, sorting or paging; the selection and the folder tree identify an item
  by it.
- `Capabilities` says what the item supports. What the explorer permits is the host's
  `ExplorerItemOptions`, and an operation is offered only where both agree; `GetCapabilities(options)`
  and `Permits(options, capability)`, extension methods in `ExplorerItemExtensions`, answer the
  combination, and answer nothing while the item is disabled.
- `Resource` is what the thumbnail, the preview, the kind and size columns and the byte ceilings
  read, so an item with a resource is shown as the kind its `ResourceInfo` says it is.

`ExplorerItem` and `ExplorerFolderItem` implement both over init-only members, and
`ExplorerItem.For`, `ForAll` and `ForFolder` build them from resources with the capabilities each
kind supports.

### The provider

`IExplorerItemsProvider.GetItemsAsync(ExplorerItemQuery, CancellationToken)` answers one window.
`ExplorerItemQuery` carries the state the explorer owns — `Skip`, `Take`, `Search`, `SortBy` and
`Direction` — and `ExplorerItemPage` carries the `Items` of the window and a `TotalCount`.
Whatever the query does not carry belongs to the implementation:

- The folder being browsed is the host's; the query never names it.
- The kinds accepted are applied by the provider itself. `ExplorerResourceFilter.Parse(pattern)`
  reads an `AcceptedResourceKinds` pattern, `globs:*.jpg;*.pdf` or `contentType:image;video`, and
  `Accepts(resource)` answers for one resource.
- `TotalCount` counts every item that matches, hidden ones included, or is null when the store
  cannot say without counting; a window shorter than `Take` ends the collection.
- `ExplorerItemPage.Empty` is a window of nothing with a total of zero.

## What a caller constructs

`ExplorerOptions`, `ExplorerItemOptions` and `ExplorerConstraints` are init-only records the host
hands the component, each with a preset: `ExplorerOptions.Default`, `ExplorerItemOptions.Default`
and `ExplorerItemOptions.ReadOnly`, and `ExplorerConstraints.None`. Their members are tabled in
the component's README. Each ceiling in `ExplorerConstraints` pairs with an
`ExplorerRejectionReason`, and a refusal comes back as an `ExplorerRejection` of the `Reason`, the
`Allowed` ceiling where the reason is a count, and a `Message` written in the reader's language.

## What the explorer reports

Every change the reader asks for is a record in `Forma.Framework.Explorer.Requests` carrying the
host's own items: `ExplorerRenameRequest`, `ExplorerDeleteRequest`, `ExplorerDropRequest`,
`ExplorerFolderCreateRequest`, `ExplorerFolderRenameRequest`, `ExplorerFolderDeleteRequest` and
`ExplorerFolderDropRequest`. Which callback raises which, and what each carries, is tabled in the
component's README.

## What the explorer's parts read

`IExplorerView` is everything the parts drawing an explorer read, composed of
`IExplorerConfigurationView`, `IExplorerLocationView`, `IExplorerContentView` and
`IExplorerSearchView`, with the `Selection` as an `IExplorerSelectionView` and the `Item` state as
an `IExplorerItemView`. `IExplorerInteraction` is everything they ask, composed of
`IExplorerContentInteraction`, `IExplorerLocationInteraction`, `IExplorerSelectionInteraction` and
`IExplorerSearchInteraction`. The sidebar's nodes read `IExplorerSidebarView` and
`IExplorerSidebarInteraction`, and the regions listing items read `IExplorerItemView` and
`IExplorerItemInteraction`.

The state behind those contracts:

| Type | What it is |
| --- | --- |
| `ExplorerCollectionView` | The shaped view over a held collection: filtered by kind, searched by name, sorted, and cut to the visible page. |
| `ExplorerProviderWindow` | The window fetched from a provider one page at a time, with the fetch itself in flight. |
| `ExplorerGrouping`, `ExplorerItemGroup` | The listed items gathered into the groups a field bands them by, each group a run of items sharing an `ExplorerGroupBand`. |
| `ExplorerFolderTree` | The folder items indexed by key and by parent. |
| `ExplorerHistory` | The trail of folders visited, with a cursor moving back and forward along it. |
| `ExplorerSelection` | The selected items, one kind at a time, capped by `SelectionLimit`. |
| `ExplorerItemManager` | The actions, pending confirmations and drag state shared by every region listing items. |
| `ExplorerItemAction` | One operation offered on every item: its icon and label, the capabilities it requires, and what it runs. |
| `ExplorerPendingAction` | A confirmation awaiting the reader: `ExplorerRenameAction<T>` and `ExplorerCreateFolderAction` collect a name, and `ExplorerBatchAction<T>` holds the items to delete. |
| `ExplorerPreview` | The resource shown full-size, and the siblings a viewer can step through. |

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
total is null and the explorer keeps asking while windows come back full:

```csharp
using Forma.Framework.Explorer.Content.Queries;

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

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). The assembly ships inside
`2Site.Forma.Blazor.Pro.Explorer`, which is licensed as Forma Pro under
[`Forma.Blazor.Pro/LICENSE.md`](../../Forma.Blazor.Pro/LICENSE.md).
