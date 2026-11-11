namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;

/// <summary>What an explorer's data view reads its items from: a location's content, its folders before its files, and a folder's own folders for the tree.</summary>
/// <param name="folders">The folders the explorer holds, as they stand when a request is made.</param>
/// <param name="files">The provider of the files, the host's or one over the items the explorer holds.</param>
/// <param name="createQuery">Creates the query for a location's content, from the folder's key, the first position and how many positions.</param>
/// <param name="filter">The kinds the explorer lists, where null lists every kind.</param>
internal sealed class ExplorerDataViewSource(Func<CatalogFolderTree> folders, Func<ICatalogProvider> files, Func<string?, int, int, CatalogQuery> createQuery, Func<CatalogResourceFilter?> filter)
{
    private ExplorerContentProvider? _content;
    private CatalogFolderTree? _contentFolders;
    private ICatalogProvider? _contentFiles;

    // Whether a slice of the run under way held an item the explorer leaves out, which the store's count of what it lists never knew.
    private bool _hasLeftItemsOut;

    /// <summary>How many items each group of the last content answered holds, across the whole query.</summary>
    public IReadOnlyList<CatalogGroupSize>? GroupSizes { get; private set; }

    /// <summary>Answers a request: a folder's own folders for the tree, and a location's content otherwise.</summary>
    /// <param name="request">The request the data view made.</param>
    public async ValueTask<DataViewPage<ICatalogItem>> ProvideAsync(DataViewRequest<ICatalogItem> request)
    {
        var tree = folders();

        // The location is read as the request is made, so an answer for one folder never fills another's pane.
        var folderKey = (request.Parent as ICatalogFolderItem)?.Key;

        if (request.Kind == DataViewRequestKind.Children)
        {
            var children = tree.GetListedChildren(folderKey);

            return new DataViewPage<ICatalogItem>([.. children], children.Count);
        }

        var query = createQuery(folderKey, request.StartIndex, request.Count ?? int.MaxValue);
        var page = await GetContent(tree).GetItemsAsync(query, request.CancellationToken);

        // A hidden item, or a file of a kind the explorer does not list, leaves the page while the position it stands in still counts.
        var accepted = filter();
        List<ICatalogItem> listed = [.. page.Items.Where(item => item.Visibility != CatalogItemVisibility.Hidden
            && (accepted is null || item is ICatalogFolderItem || item.Resource is not { } resource || accepted.Accepts(resource)))];

        if (!request.CancellationToken.IsCancellationRequested)
        {
            GroupSizes = page.GroupSizes;
            _hasLeftItemsOut = (request.StartIndex > 0 && _hasLeftItemsOut) || listed.Count < page.Items.Count;
        }

        // A count the store guarantees of what the whole query lists is the total shown, while no slice of the run left an item out;
        // the store's positions are what pages are cut in.
        return new DataViewPage<ICatalogItem>(listed, _hasLeftItemsOut ? null : page.ListedCount)
        {
            ReadCount = page.ReadCount ?? page.Items.Count,
            HasMore = page.HasMore,
            PositionCount = page.TotalCount,
            GroupSizes = page.GroupSizes is { } sizes ? [.. sizes.Select(size => new DataViewGroupSize(size.Key, size.Count))] : null,
        };
    }

    // The content provider keeps the files it read for the folders the slices pass, so it lasts while the folders and the files do.
    private ExplorerContentProvider GetContent(CatalogFolderTree tree)
    {
        var provider = files();

        if (_content is null || !ReferenceEquals(_contentFolders, tree) || !ReferenceEquals(_contentFiles, provider))
        {
            _content = new ExplorerContentProvider(tree, provider);
            _contentFolders = tree;
            _contentFiles = provider;
        }

        return _content;
    }
}
