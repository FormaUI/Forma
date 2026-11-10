namespace Forma.Blazor.Explorer.BUnit.Tests;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>An in-memory catalog provider that records the queries it was asked.</summary>
public sealed class FakeCatalogProvider(IReadOnlyList<ResourceInfo> resources) : ICatalogProvider
{
    private readonly List<ResourceInfo> _resources = [.. resources];
    private readonly Dictionary<string, TaskCompletionSource> _askedFolders = new(StringComparer.Ordinal);

    /// <summary>Every query the provider was asked, in order.</summary>
    public List<CatalogQuery> Queries { get; } = [];

    /// <summary>The gate each fetch waits on until it is released, where null means a fetch answers at once.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>The key of the folder whose fetches alone wait on <see cref="Gate"/>, where null holds every fetch.</summary>
    public string? GatedFolderKey { get; set; }

    /// <summary>The resources each folder holds, by the folder's key, where a folder it does not name holds the resources the provider was made with.</summary>
    public Dictionary<string, IReadOnlyList<ResourceInfo>> FolderResources { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether the provider reports the total count of the resources that match.</summary>
    public bool ReportsTotal { get; set; } = true;

    /// <summary>Whether the provider guarantees how many items the whole query lists.</summary>
    public bool ReportsListedCount { get; set; }

    /// <summary>The sources of the resources the provider returns as hidden items.</summary>
    public ISet<string> HiddenSources { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Completes once the provider has been asked for the files of the folder <paramref name="folderKey"/> names.</summary>
    /// <param name="folderKey">The folder's key.</param>
    public Task WhenAskedForAsync(string folderKey)
    {
        lock (_askedFolders)
        {
            if (!_askedFolders.TryGetValue(folderKey, out var asked))
            {
                _askedFolders[folderKey] = asked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            if (Queries.Exists(query => query.FolderKey == folderKey))
            {
                asked.TrySetResult();
            }

            return asked.Task;
        }
    }

    /// <inheritdoc />
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        lock (_askedFolders)
        {
            Queries.Add(query);

            if (query.FolderKey is { } asked && _askedFolders.TryGetValue(asked, out var waiting))
            {
                waiting.TrySetResult();
            }
        }

        if (Gate is { } gate && (GatedFolderKey is null || GatedFolderKey == query.FolderKey))
        {
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        IEnumerable<ResourceInfo> matching = query.FolderKey is { } folderKey && FolderResources.TryGetValue(folderKey, out var held) ? held : _resources;

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // The sorts are stable, so the keys order what the sort ties, and the groups order before the sort.
        matching = matching.OrderBy(resource => resource.Source, StringComparer.Ordinal);

        matching = query.Direction == CatalogSortDirection.Descending
            ? matching.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
            : matching.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase);

        if (query.Grouping is { } grouping)
        {
            matching = matching.OrderBy(resource => grouping.IndexOf(ToItem(resource)));
        }

        var ordered = matching.ToList();

        return new CatalogPage(
            [.. ordered.Skip(query.Skip).Take(query.Take).Select(ToItem)],
            ReportsTotal ? ordered.Count : null)
        {
            ListedCount = ReportsListedCount ? ordered.Count(resource => !HiddenSources.Contains(resource.Source)) : null,
            GroupSizes = query.IncludesGroupSizes && query.Grouping is { } counted ? counted.CountGroups(ordered.Select(ToItem)) : null
        };
    }

    private ICatalogItem ToItem(ResourceInfo resource)
        => HiddenSources.Contains(resource.Source)
            ? new CatalogItem { Key = resource.Source, Name = resource.Name, Visibility = CatalogItemVisibility.Hidden, Resource = resource }
            : CatalogItem.For(resource);
}
