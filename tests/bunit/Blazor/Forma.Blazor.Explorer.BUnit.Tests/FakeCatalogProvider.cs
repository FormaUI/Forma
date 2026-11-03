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

    /// <summary>Every query the provider was asked, in order.</summary>
    public List<CatalogQuery> Queries { get; } = [];

    /// <summary>The gate each fetch waits on until it is released, where null means a fetch answers at once.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>Whether the provider reports the total count of the resources that match.</summary>
    public bool ReportsTotal { get; set; } = true;

    /// <summary>The sources of the resources the provider returns as hidden items.</summary>
    public ISet<string> HiddenSources { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <inheritdoc />
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
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

        matching = query.Direction == CatalogSortDirection.Descending
            ? matching.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
            : matching.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase);

        var ordered = matching.ToList();

        return new CatalogPage(
            [.. ordered.Skip(query.Skip).Take(query.Take).Select(ToItem)],
            ReportsTotal ? ordered.Count : null);
    }

    private ICatalogItem ToItem(ResourceInfo resource)
        => HiddenSources.Contains(resource.Source)
            ? new CatalogItem { Key = resource.Source, Name = resource.Name, Visibility = CatalogItemVisibility.Hidden, Resource = resource }
            : CatalogItem.For(resource);
}
