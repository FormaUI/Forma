namespace Forma.Blazor.Showcase.Features.Media;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>The Showcase's stored images, in folders.</summary>
internal sealed class ShowcaseImageLibrary : ICatalogProvider
{
    private static readonly DateTimeOffset SeedDate = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static readonly FolderResourceInfo Stages = new("showcase-media:stages") { Name = "Stages", Count = 3, CreatedAt = SeedDate.AddDays(-45), ModifiedAt = SeedDate.AddDays(-1) };

    private static readonly FolderResourceInfo Finishes = new("showcase-media:finishes") { Name = "Finishes", Parent = Stages.Source, Count = 1, CreatedAt = SeedDate.AddDays(-10), ModifiedAt = SeedDate.AddHours(-5) };

    private static readonly FolderResourceInfo Ceremonies = new("showcase-media:ceremonies") { Name = "Ceremonies", Count = 2, CreatedAt = SeedDate.AddDays(-25), ModifiedAt = SeedDate.AddDays(-14) };

    // Each resource with the source of the folder holding it, where null is the top level.
    private static readonly (ResourceInfo Resource, string? Folder)[] Resources =
    [
        (Stages, null),
        (Finishes, Stages.Source),
        (Ceremonies, null),
        (new ImageResourceInfo("/media/sample-poster.svg?image=start-line") { Name = "start-line.svg", Size = 12400, CreatedAt = SeedDate.AddDays(-40), ModifiedAt = SeedDate.AddDays(-2) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=mountain-stage") { Name = "mountain-stage.svg", Size = 18800, CreatedAt = SeedDate.AddDays(-30), ModifiedAt = SeedDate.AddDays(-9) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=podium") { Name = "podium.svg", Size = 9700, CreatedAt = SeedDate.AddDays(-21), ModifiedAt = SeedDate.AddDays(-20) }, Ceremonies.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=team-portrait") { Name = "team-portrait.svg", Size = 24100, CreatedAt = SeedDate.AddDays(-14), ModifiedAt = SeedDate.AddDays(-14) }, Ceremonies.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=time-trial") { Name = "time-trial.svg", Size = 7300, CreatedAt = SeedDate.AddDays(-6), ModifiedAt = SeedDate.AddDays(-1) }, Stages.Source),
        (new ImageResourceInfo("/media/sample-poster.svg?image=sprint-finish") { Name = "sprint-finish.svg", Size = 15600, CreatedAt = SeedDate.AddDays(-2), ModifiedAt = SeedDate.AddHours(-5) }, Finishes.Source)
    ];

    /// <inheritdoc />
    public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        // A folder's own items, or every folder's; the kinds asked for apply before the slice, so its positions and total count only those.
        var matchingImages = Resources
            .Where(entry => query.FolderKey is not { } folder || entry.Folder == folder)
            .Select(entry => entry.Resource)
            .Where(resource => query.AsksFor(resource.GetKind()));

        if (query.Search is { Length: > 0 } search)
        {
            matchingImages = matchingImages.Where(resource => resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var isDescending = query.Direction == CatalogSortDirection.Descending;

        // The sort is stable, so the resources it ties keep the order of their keys.
        matchingImages = matchingImages.OrderBy(resource => resource.Source, StringComparer.Ordinal);

        matchingImages = (query.SortBy, isDescending) switch
        {
            (CatalogSortField.Size, false) => matchingImages.OrderBy(resource => resource.Size ?? 0),
            (CatalogSortField.Size, true) => matchingImages.OrderByDescending(resource => resource.Size ?? 0),
            (CatalogSortField.DateAdded, false) => matchingImages.OrderBy(resource => resource.CreatedAt),
            (CatalogSortField.DateAdded, true) => matchingImages.OrderByDescending(resource => resource.CreatedAt),
            (CatalogSortField.DateModified, false) => matchingImages.OrderBy(resource => resource.ModifiedAt),
            (CatalogSortField.DateModified, true) => matchingImages.OrderByDescending(resource => resource.ModifiedAt),
            (_, true) => matchingImages.OrderByDescending(resource => resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matchingImages.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        // The groups order before the sort, which the stable sort keeps within each group.
        if (query.Grouping is { } grouping)
        {
            matchingImages = matchingImages.OrderBy(resource => grouping.IndexOf(CatalogItem.For(resource)));
        }

        var orderedImages = matchingImages.ToList();

        return ValueTask.FromResult(new CatalogPage(
            CatalogItem.ForAll(orderedImages.Skip(query.Skip).Take(query.Take)),
            orderedImages.Count)
        {
            GroupSizes = query.IncludesGroupSizes ? query.Grouping?.CountGroups(CatalogItem.ForAll(orderedImages)) : null
        });
    }
}
