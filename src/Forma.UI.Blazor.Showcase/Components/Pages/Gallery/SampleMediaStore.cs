namespace Forma.UI.Blazor.Showcase.Components.Pages.Gallery;

using Forma.Framework.Resources;
using Forma.UI.Components.Gallery;

/// <summary>An in-memory store standing in for one the gallery would reach over IO.</summary>
public sealed class SampleMediaStore(IReadOnlyList<SampleResource> resources, TimeSpan latency)
{
    private readonly List<SampleResource> _resources = [.. resources];

    /// <summary>How many windows have been asked for, so a demo can show that only what is seen is fetched.</summary>
    public int QueryCount { get; private set; }

    /// <summary>The last window asked for, so a demo can show what the gallery requested.</summary>
    public GalleryResourceQuery? LastQuery { get; private set; }

    /// <summary>Raised once a window has been answered, so a demo can redraw its own reporting.</summary>
    public Func<Task>? Queried { get; set; }

    /// <summary>Answers for one window of the named folder, or of everything when none is named, after a delay standing in for the round trip.</summary>
    public async ValueTask<GalleryResourcePage> GetPageAsync(string? folder, GalleryResourceQuery query, CancellationToken cancellationToken)
    {
        QueryCount++;
        LastQuery = query;

        await Task.Delay(latency, cancellationToken).ConfigureAwait(false);

        // The sidebar's root is "All media" rather than an unfiled folder, so no folder means no filter.
        IEnumerable<SampleResource> matching = folder is null
            ? _resources
            : _resources.Where(resource => resource.Folder == folder);

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == GallerySortDirection.Descending;

        matching = (query.SortBy, descending) switch
        {
            (GallerySortField.Size, false) => matching.OrderBy(resource => resource.Resource.Size ?? 0),
            (GallerySortField.Size, true) => matching.OrderByDescending(resource => resource.Resource.Size ?? 0),
            (GallerySortField.Kind, false) => matching.OrderBy(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (GallerySortField.Kind, true) => matching.OrderByDescending(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (GallerySortField.DateAdded, false) => matching.OrderBy(resource => resource.Resource.CreatedAt),
            (GallerySortField.DateAdded, true) => matching.OrderByDescending(resource => resource.Resource.CreatedAt),
            (GallerySortField.DateModified, false) => matching.OrderBy(resource => resource.Resource.ModifiedAt),
            (GallerySortField.DateModified, true) => matching.OrderByDescending(resource => resource.Resource.ModifiedAt),
            (_, true) => matching.OrderByDescending(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        var ordered = matching.ToList();
        var page = new GalleryResourcePage(
            [.. ordered.Skip(query.Skip).Take(query.Take).Select(resource => resource.Resource)],
            ordered.Count);

        if (Queried is { } queried)
        {
            await queried().ConfigureAwait(false);
        }

        return page;
    }
}

/// <summary>A stored resource together with the folder holding it.</summary>
/// <param name="Resource">The resource itself.</param>
/// <param name="Folder">The source of the folder it belongs to, or null when it sits at the root.</param>
public sealed record SampleResource(FileResourceInfo Resource, string? Folder);
