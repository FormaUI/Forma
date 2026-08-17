namespace Forma.UI.Blazor.Tests.Components.Gallery;

using Forma.Framework.Resources;
using Forma.UI.Components.Gallery;

/// <summary>An in-memory provider that records the queries it was asked, for tests about what a gallery fetches.</summary>
public sealed class FakeGalleryResourcesProvider(IReadOnlyList<ResourceInfo> resources) : IGalleryResourcesProvider
{
    private readonly List<ResourceInfo> _resources = [.. resources];

    /// <summary>Every query the provider was asked, in order.</summary>
    public List<GalleryResourceQuery> Queries { get; } = [];

    /// <summary>Held until released when set, so a test can observe the gallery mid-fetch.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>Reported instead of a count when set, standing in for a store that cannot total its contents.</summary>
    public bool ReportsTotal { get; set; } = true;

    /// <summary>The window the query names, filtered by search and ordered by name alone.</summary>
    public async ValueTask<GalleryResourcePage> GetResourcesAsync(GalleryResourceQuery query, CancellationToken cancellationToken)
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

        matching = query.Direction == GallerySortDirection.Descending
            ? matching.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
            : matching.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase);

        var ordered = matching.ToList();

        return new GalleryResourcePage(
            [.. ordered.Skip(query.Skip).Take(query.Take)],
            ReportsTotal ? ordered.Count : null);
    }
}
