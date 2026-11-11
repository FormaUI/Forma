namespace Forma.Framework.Explorer.Content;

using System.Collections.Frozen;
using System.Globalization;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;

/// <summary>The content of a location, its child folders before its files, answered a slice at a time.</summary>
/// <remarks>
/// The folders are the location's children in the folder tree, the top-level folders for every folder's items, searched
/// by name with hidden folders left out and sorted by the query's sort, ahead of every file in either direction. A
/// slice's positions split at how many folders match: those before it are folders, and the rest ask the file provider
/// from its first position, so a slice of folders alone asks the file provider nothing. Under a grouping the folders fall
/// in the grouping's leading group, whose size the folders give. The file provider's total, listed count and group sizes
/// are kept for the query they answered, so a slice of folders alone reports them while its query stays the same. The
/// query's kinds decide what it lists: folders where it asks for them, and from the file provider the kinds it asks for
/// among those a file can be, which the file provider applies before it pages; an item of another kind in its answer is
/// left out of the slice.
/// </remarks>
/// <param name="folders">The folder tree the locations stand in.</param>
/// <param name="files">The provider of a location's files.</param>
public sealed class ExplorerContentProvider(CatalogFolderTree folders, ICatalogProvider files) : ICatalogProvider
{
    private readonly CatalogFolderTree _folders = folders ?? throw new ArgumentNullException(nameof(folders));
    private readonly ICatalogProvider _files = files ?? throw new ArgumentNullException(nameof(files));
    private readonly CatalogCollectionView _folderView = new(static _ => string.Empty, static () => CultureInfo.InvariantCulture);

    private CatalogQuery? _filesQuery;
    private CatalogPage? _filesPage;

    // The kinds a file can be, every kind but a folder, named one by one so the file provider is asked for the kinds known here.
    private static readonly FrozenSet<CatalogItemKind> FileKinds = new[]
    {
        CatalogItemKind.Image, CatalogItemKind.Video, CatalogItemKind.Audio, CatalogItemKind.Archive,
        CatalogItemKind.Document, CatalogItemKind.File, CatalogItemKind.Unknown,
    }.ToFrozenSet();

    // The file kinds last derived, kept for the kinds they were derived from, so one query's file queries carry the same set.
    private (IReadOnlySet<CatalogItemKind> Asked, FrozenSet<CatalogItemKind> Files)? _fileKinds;

    /// <inheritdoc />
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ICatalogItem> matchingFolders = query.AsksFor(CatalogItemKind.Folder) ? GetFolders(query) : [];
        var folderCount = matchingFolders.Count;
        List<ICatalogItem> folderSlice = [.. matchingFolders.Skip(query.Skip).Take(query.Take)];
        var fileTake = query.Take - folderSlice.Count;
        var folderSizes = query.IncludesGroupSizes && query.Grouping is { } grouping ? grouping.CountGroups(matchingFolders) : null;
        var fileKinds = GetFileKinds(query.Kinds);

        // A query asking for no kind a file can be lists the folders alone, the file provider never asked.
        if (fileKinds.Count == 0)
        {
            return new CatalogPage(folderSlice, folderCount)
            {
                ReadCount = folderSlice.Count,
                HasMore = query.Skip + folderSlice.Count < folderCount,
                ListedCount = folderCount,
                GroupSizes = folderSizes,
            };
        }

        var filesQuery = query with { Skip = Math.Max(0, query.Skip - folderCount), Take = Math.Max(0, fileTake), Kinds = fileKinds };

        if (fileTake <= 0)
        {
            var known = IsSameSequence(_filesQuery, filesQuery) ? _filesPage : null;
            var end = query.Skip + folderSlice.Count;

            return new CatalogPage(folderSlice, known?.TotalCount + folderCount)
            {
                ReadCount = folderSlice.Count,
                HasMore = end < folderCount || known is not { TotalCount: 0 },
                ListedCount = known?.ListedCount + folderCount,
                GroupSizes = Merge(folderSizes, known?.GroupSizes)
            };
        }

        var page = await _files.GetItemsAsync(filesQuery, cancellationToken).ConfigureAwait(false);
        _filesQuery = filesQuery;
        _filesPage = page;

        return new CatalogPage([.. folderSlice, .. page.Items.Where(item => fileKinds.Contains(item.GetKind()))], page.TotalCount + folderCount)
        {
            ReadCount = folderSlice.Count + (page.ReadCount ?? page.Items.Count),
            HasMore = page.HasMore,
            ListedCount = page.ListedCount + folderCount,
            GroupSizes = Merge(folderSizes, page.GroupSizes)
        };
    }

    // The kinds a file can be that the query asks for.
    private FrozenSet<CatalogItemKind> GetFileKinds(IReadOnlySet<CatalogItemKind>? asked)
    {
        if (asked is null)
        {
            return FileKinds;
        }

        if (_fileKinds is not { } known || !ReferenceEquals(known.Asked, asked))
        {
            known = (asked, asked.Where(FileKinds.Contains).ToFrozenSet());
            _fileKinds = known;
        }

        return known.Files;
    }

    // The location's listed folders that match the query's search, in its sort.
    private IReadOnlyList<ICatalogItem> GetFolders(CatalogQuery query)
    {
        _folderView.Source = _folders.GetListedChildren(query.FolderKey);
        _folderView.SearchQuery = query.Search ?? string.Empty;
        _folderView.SortField = query.SortBy;
        _folderView.SortDirection = query.Direction;

        return _folderView.MatchingItems;
    }

    // Whether two file queries read the same sequence, apart from where their slices begin and end.
    private static bool IsSameSequence(CatalogQuery? known, CatalogQuery query)
        => known is { } last && last with { Skip = 0, Take = 0 } == query with { Skip = 0, Take = 0 };

    // The folders' groups lead, so their sizes come before the files'.
    private static IReadOnlyList<CatalogGroupSize>? Merge(IReadOnlyList<CatalogGroupSize>? folderSizes, IReadOnlyList<CatalogGroupSize>? fileSizes)
        => folderSizes is null || fileSizes is null ? folderSizes ?? fileSizes : [.. folderSizes, .. fileSizes];
}
