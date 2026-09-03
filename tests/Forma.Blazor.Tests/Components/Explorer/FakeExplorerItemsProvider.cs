namespace Forma.Blazor.Tests.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Resources;

/// <summary>An in-memory provider that records the queries it was asked, for tests about what an explorer fetches.</summary>
public sealed class FakeExplorerItemsProvider(IReadOnlyList<ResourceInfo> resources) : IExplorerItemsProvider
{
    private readonly List<ResourceInfo> _resources = [.. resources];

    /// <summary>Every query the provider was asked, in order.</summary>
    public List<ExplorerItemQuery> Queries { get; } = [];

    /// <summary>Held until released when set, so a test can observe the explorer mid-fetch.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>Reported instead of a count when set, standing in for a store that cannot total its contents.</summary>
    public bool ReportsTotal { get; set; } = true;

    /// <summary>The sources of the resources the provider returns as hidden items.</summary>
    public ISet<string> HiddenSources { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>The window the query names, filtered by search and ordered by name alone.</summary>
    public async ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);

        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        IEnumerable<ResourceInfo> matching = _resources;

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        matching = query.Direction == ExplorerSortDirection.Descending
            ? matching.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
            : matching.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase);

        var ordered = matching.ToList();

        return new ExplorerItemPage(
            [.. ordered.Skip(query.Skip).Take(query.Take).Select(ToItem)],
            ReportsTotal ? ordered.Count : null);
    }

    private IExplorerItem ToItem(ResourceInfo resource)
        => HiddenSources.Contains(resource.Source)
            ? new ExplorerItem { Key = resource.Source, Name = resource.Name, Visibility = ExplorerItemVisibility.Hidden, Resource = resource }
            : ExplorerItem.For(resource);
}
