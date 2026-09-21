# Forma.Framework.Explorer

The state behind `FormaExplorer`'s parts: the options and item options that shape one explorer,
the view and interaction contracts the parts drawing it read and ask, the item manager holding
the actions, the pending confirmations and the drag state those parts share, the grouping of the
listed items, and the preview.

The items the explorer lists, the provider that answers it, the requests it hands back, the
ceilings it keeps and the bookkeeping every browsing surface repeats are
[`Forma.Framework.Catalog`](../Forma.Framework.Catalog/README.md). **That is the
package to read to write items or a provider**; this assembly is how the explorer draws them.

It ships inside `Forma.Blazor.Explorer` rather than as a package of its own, because
nothing outside the explorer has reason to reach it. It references nothing from ASP.NET Core, so
every piece of it is testable from a plain class library. How the component itself is consumed —
its parameters, its callbacks and the registrations it reads — is in
[`Forma.Blazor.Explorer`](../../Forma.Blazor.Explorer/README.md).

## Namespaces

| Namespace | What it holds |
| --- | --- |
| `Forma.Framework.Explorer` | `ExplorerOptions`, and `IExplorerView` and `IExplorerInteraction`, the whole of what the parts drawing an explorer read and ask. |
| `.Items` | `ExplorerItemOptions`, the operations the explorer offers on the items it lists; the actions, drag state and pending confirmations shared by every region listing items; and what those regions read and ask. |
| `.Content` | How the listed items are shown: the view mode, the details columns, the grouping bands and the preview, and what the content area reads and asks. |
| `.Content.Queries` | The group and infinite-scroll choices, the paging record, and what the search reads and asks. |
| `.Navigation` | What the address bar and the sidebar read and ask. |
| `.Selection` | What the parts ask of the selection. |
| `.Actions` | The confirmations awaiting the reader: a rename, a new folder, a deletion. |

## What a caller constructs

`ExplorerOptions` and `ExplorerItemOptions` are init-only records the host hands the component,
each with a preset: `ExplorerOptions.Default`, `ExplorerItemOptions.Default` and
`ExplorerItemOptions.ReadOnly`. Their members are tabled in the component's README.
`ExplorerItemOptions` is what an item's own `Capabilities` are intersected with:
`GetCapabilities(options)` and `Permits(options, capability)`, extension methods in
`ExplorerItemExtensions`, answer the combination, and answer nothing while the item is disabled.

## What the explorer's parts read

`IExplorerView` is everything the parts drawing an explorer read, composed of
`IExplorerConfigurationView`, `IExplorerLocationView`, `IExplorerContentView` and
`IExplorerSearchView`, with the `Selection` as an `ICatalogSelectionView` and the `Item` state as
an `IExplorerItemView`. `IExplorerInteraction` is everything they ask, composed of
`IExplorerContentInteraction`, `IExplorerLocationInteraction`, `IExplorerSelectionInteraction` and
`IExplorerSearchInteraction`. The sidebar's nodes read `IExplorerSidebarView` and
`IExplorerSidebarInteraction`, and the regions listing items read `IExplorerItemView` and
`IExplorerItemInteraction`.

The state behind those contracts that is the explorer's own:

| Type | What it is |
| --- | --- |
| `ExplorerGrouping`, `ExplorerItemGroup` | The listed items gathered into the groups a field bands them by, each group a run of items sharing an `ExplorerGroupBand`. |
| `ExplorerItemManager` | The actions, pending confirmations and drag state shared by every region listing items. |
| `ExplorerItemAction` | One operation offered on every item: its icon and label, the capabilities it requires, and what it runs. |
| `ExplorerPendingAction` | A confirmation awaiting the reader: `ExplorerRenameAction<T>` and `ExplorerCreateFolderAction` collect a name, and `ExplorerBatchAction<T>` holds the items to delete. |
| `ExplorerPreview` | The resource shown full-size, and the siblings a viewer can step through. |

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). The assembly ships inside
`Forma.Blazor.Explorer`.
