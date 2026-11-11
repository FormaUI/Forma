namespace Forma.Blazor.Showcase.Features.Media;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>The Showcase's stored images, in folders.</summary>
internal sealed class ShowcaseImageLibrary : ICatalogProvider
{
    private static readonly DateTimeOffset Seeded = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static readonly FolderResourceInfo Stages = new("showcase-media:stages") { Name = "Stages", Count = 3, CreatedAt = Seeded.AddDays(-45), ModifiedAt = Seeded.AddDays(-1) };

    private static readonly FolderResourceInfo Finishes = new("showcase-media:finishes") { Name = "Finishes", Parent = Stages.Source, Count = 1, CreatedAt = Seeded.AddDays(-10), ModifiedAt = Seeded.AddHours(-5) };

    private static readonly FolderResourceInfo Ceremonies = new("showcase-media:ceremonies") { Name = "Ceremonies", Count = 2, CreatedAt = Seeded.AddDays(-25), ModifiedAt = Seeded.AddDays(-14) };

    // Each resource with the source of the folder holding it, where null is the top level.
    private static readonly (ResourceInfo Resource, string? Folder)[] Resources =
    [
        (Stages, null),
        (Finishes, Stages.Source),
        (Ceremonies, null),
        (new ImageResourceInfo("/media/sample-poster.svg?image=start-line") { Name = "start-line.svg", Size = 12400, CreatedAt = Seeded.AddDays(-40), ModifiedAt = Seeded.AddDays(-2) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=mountain-stage") { Name = "mountain-stage.svg", Size = 18800, CreatedAt = Seeded.AddDays(-30), ModifiedAt = Seeded.AddDays(-9) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=podium") { Name = "podium.svg", Size = 9700, CreatedAt = Seeded.AddDays(-21), ModifiedAt = Seeded.AddDays(-20) }, Ceremonies.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=team-portrait") { Name = "team-portrait.svg", Size = 24100, CreatedAt = Seeded.AddDays(-14), ModifiedAt = Seeded.AddDays(-14) }, Ceremonies.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=time-trial") { Name = "time-trial.svg", Size = 7300, CreatedAt = Seeded.AddDays(-6), ModifiedAt = Seeded.AddDays(-1) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=sprint-finish") { Name = "sprint-finish.svg", Size = 15600, CreatedAt = Seeded.AddDays(-2), ModifiedAt = Seeded.AddHours(-5) }, Finishes.Source)
    ];

    /// <inheritdoc />
    public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        // A folder's own items, or every folder's; the kinds asked for apply before the slice, so its positions and total count only those.
        var matching = Resources
            .Where(entry => query.FolderKey is not { } folder || entry.Folder == folder)
            .Select(entry => entry.Resource)
            .Where(resource => query.AsksFor(resource.GetKind()));

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == CatalogSortDirection.Descending;

        // The sort is stable, so the resources it ties keep the order of their keys.
        matching = matching.OrderBy(resource => resource.Source, StringComparer.Ordinal);

        matching = (query.SortBy, descending) switch
        {
            (CatalogSortField.Size, false) => matching.OrderBy(resource => resource.Size ?? 0),
            (CatalogSortField.Size, true) => matching.OrderByDescending(resource => resource.Size ?? 0),
            (CatalogSortField.DateAdded, false) => matching.OrderBy(resource => resource.CreatedAt),
            (CatalogSortField.DateAdded, true) => matching.OrderByDescending(resource => resource.CreatedAt),
            (CatalogSortField.DateModified, false) => matching.OrderBy(resource => resource.ModifiedAt),
            (CatalogSortField.DateModified, true) => matching.OrderByDescending(resource => resource.ModifiedAt),
            (_, true) => matching.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        // The groups order before the sort, which the stable sort keeps within each group.
        if (query.Grouping is { } grouping)
        {
            matching = matching.OrderBy(resource => grouping.IndexOf(CatalogItem.For(resource)));
        }

        var ordered = matching.ToList();

        return ValueTask.FromResult(new CatalogPage(
            CatalogItem.ForAll(ordered.Skip(query.Skip).Take(query.Take)),
            ordered.Count)
        {
            GroupSizes = query.IncludesGroupSizes ? query.Grouping?.CountGroups(CatalogItem.ForAll(ordered)) : null
        });
    }
}
