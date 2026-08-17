namespace Forma.UI.Blazor.Showcase.Components.Pages.Gallery;

using Forma.Framework.Resources;
using Forma.UI.Components.Gallery;

/// <summary>An in-memory store standing in for one the gallery would reach over IO.</summary>
public sealed class SampleMediaStore(IReadOnlyList<GalleryResourceEntry> resources, TimeSpan latency)
{
    private readonly List<GalleryResourceEntry> _resources = [.. resources];

    /// <summary>How many windows have been asked for, so a demo can show that only what is seen is fetched.</summary>
    public int QueryCount { get; private set; }

    /// <summary>The last window asked for, so a demo can show what the gallery requested.</summary>
    public GalleryResourceQuery? LastQuery { get; private set; }

    /// <summary>Raised once a window has been answered, so a demo can redraw its own reporting.</summary>
    public Func<Task>? Queried { get; set; }

    /// <summary>Answers for one window of the named folder, or of everything when none is named, after a delay standing in for the round trip.</summary>
    public async ValueTask<GalleryResourcePage> GetPageAsync(string? folderId, GalleryResourceQuery query, CancellationToken cancellationToken)
    {
        QueryCount++;
        LastQuery = query;

        await Task.Delay(latency, cancellationToken).ConfigureAwait(false);

        // The sidebar's root is "All media" rather than an unfiled folder, so no folder means no filter.
        IEnumerable<GalleryResourceEntry> matching = folderId is null
            ? _resources
            : _resources.Where(entry => entry.FolderId == folderId);

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(entry => entry.Resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == GallerySortDirection.Descending;

        matching = (query.SortBy, descending) switch
        {
            (GallerySortField.Size, false) => matching.OrderBy(entry => entry.Resource.Size ?? 0),
            (GallerySortField.Size, true) => matching.OrderByDescending(entry => entry.Resource.Size ?? 0),
            (GallerySortField.Kind, false) => matching.OrderBy(entry => entry.Resource.Kind),
            (GallerySortField.Kind, true) => matching.OrderByDescending(entry => entry.Resource.Kind),
            (GallerySortField.DateAdded, false) => matching.OrderBy(entry => entry.Resource.AddedAt),
            (GallerySortField.DateAdded, true) => matching.OrderByDescending(entry => entry.Resource.AddedAt),
            (GallerySortField.DateModified, false) => matching.OrderBy(entry => entry.Resource.ModifiedAt),
            (GallerySortField.DateModified, true) => matching.OrderByDescending(entry => entry.Resource.ModifiedAt),
            (_, true) => matching.OrderByDescending(entry => entry.Resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(entry => entry.Resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        var ordered = matching.ToList();
        var page = new GalleryResourcePage(
            [.. ordered.Skip(query.Skip).Take(query.Take).Select(entry => entry.Resource)],
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
/// <param name="FolderId">The folder it belongs to, or null when it sits at the root.</param>
public sealed record GalleryResourceEntry(GalleryResourceInfo Resource, string? FolderId);
