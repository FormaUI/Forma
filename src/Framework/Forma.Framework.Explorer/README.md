# Forma.Framework.Explorer

The options and state behind `FormaExplorer`: the records a host configures an explorer with, and
the view and interaction contracts through which the explorer's parts, its toolbar, sidebar, address
bar and content views, read its state and ask it for changes.

The package comes with `formaui-net.Blazor.Explorer`, so a host installs that
package and uses these types from it. The items, providers and requests an explorer works with are
[`Forma.Framework.Catalog`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Catalog/README.md)'s,
and the component's parameters and callbacks are documented in
[`Forma.Blazor.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Explorer/README.md).

## Configuring an explorer

A host hands `FormaExplorer` two init-only records:

| Type | Namespace | What it configures |
| --- | --- | --- |
| `ExplorerOptions` | `Forma.Framework.Explorer` | The presentation: the sidebar, the folder menus, resizing, dialog presentation, selection, infinite scrolling, the details columns and the search delay. `ExplorerOptions.Default` is the standard presentation. |
| `ExplorerItemOptions` | `Forma.Framework.Explorer.Items` | The operations permitted on items and on folders, and upload. `ExplorerItemOptions.Default` permits every operation, and `ExplorerItemOptions.ReadOnly` permits browsing and selection. |

The enums the component's parameters take live beside them: `ExplorerViewMode` and
`ExplorerDetailsColumns` in `Forma.Framework.Explorer.Content`, and `ExplorerGroupField` and
`ExplorerInfiniteScrollMode` in `Forma.Framework.Explorer.Content.Queries`.

```csharp
using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;

var options = ExplorerOptions.Default with
{
    ShowSidebar = false,
    InfiniteScroll = ExplorerInfiniteScrollMode.Automatic,
    DetailsColumns = ExplorerDetailsColumns.Size | ExplorerDetailsColumns.Modified,
};

var itemOptions = ExplorerItemOptions.Default with
{
    AllowedCapabilities = CatalogItemCapabilities.Open | CatalogItemCapabilities.View | CatalogItemCapabilities.Rename,
    AllowUpload = false,
};
```

An item offers an operation when the item's own `Capabilities` support it and the item options
permit it. `GetCapabilities(options)` and `Permits(options, capability)`, extension methods on
`ICatalogItem` in `ExplorerItemExtensions`, answer that combination, with nothing permitted on a
disabled item.

## How the explorer's parts use it

`FormaExplorer` implements `IExplorerView` and `IExplorerInteraction` and cascades both to its
parts, which stand inside the data view the explorer browses through, its sidebar being that data
view's tree. Each part takes the narrower contract it needs as a cascading parameter:

| Contract | What a part reads or asks |
| --- | --- |
| `IExplorerConfigurationView` | The options in force, and whether the sidebar shows. |
| `IExplorerLocationView`, `IExplorerLocationInteraction` | The current folder, the breadcrumbs, the folder tree and whether back, forward and up are open; moving between folders. |
| `IExplorerContentView`, `IExplorerContentInteraction` | The items and groups shown, the view mode, sort, grouping and details columns; changing them and refreshing. |
| `IExplorerSearchView`, `IExplorerSearchInteraction` | The search text and placeholder; changing the search text. |
| `IExplorerSelectionInteraction` | Toggling an item, toggling selection mode and clearing; parts read the selection as an `ICatalogSelectionView`. |
| `IExplorerItemView`, `IExplorerItemInteraction` | The item actions, item options and pending confirmation; activating, dragging, renaming, deleting and confirming. |

The state behind those contracts is held in:

| Type | What it is |
| --- | --- |
| `ExplorerItemManager` | The item actions, pending confirmations and drag state shared by every region that lists items. |
| `ExplorerItemAction` | One operation offered on an item: its icon, label, required capabilities and what it runs. |
| `ExplorerPendingAction` | A confirmation awaiting the reader: `ExplorerRenameAction<T>`, `ExplorerCreateFolderAction` or `ExplorerBatchAction<T>`. |
| `ExplorerContentProvider` | The content of a location, its child folders from the folder tree before its files from a provider, sliced by position across both, the folders filling the grouping's leading group. |
| `ExplorerGrouping`, `ExplorerItemGroup` | The groups an `ExplorerGroupField` bands the items into, defined once for a browsing sequence as a `CatalogGrouping` a store applies, led by the folders' group, and the shown items gathered into them, each group the items sharing an `ExplorerGroupBand`, such as names from A to H or sizes up to 1 MB, counted across the whole result. |
| `ExplorerPreview` | The resource shown full-size, and the siblings a viewer steps through. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
