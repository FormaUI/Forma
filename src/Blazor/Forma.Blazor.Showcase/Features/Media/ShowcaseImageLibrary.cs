namespace Forma.Blazor.Showcase.Features.Media;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>The Showcase's stored images.</summary>
internal sealed class ShowcaseImageLibrary : ICatalogProvider
{
    private static readonly DateTimeOffset Seeded = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static readonly ResourceInfo[] Images =
    [
        new ImageResourceInfo("/media/sample-poster.svg?image=start-line") { Name = "start-line.svg", Size = 12400, CreatedAt = Seeded.AddDays(-40), ModifiedAt = Seeded.AddDays(-2) },
        new ImageResourceInfo("/media/sample-poster.svg?image=mountain-stage") { Name = "mountain-stage.svg", Size = 18800, CreatedAt = Seeded.AddDays(-30), ModifiedAt = Seeded.AddDays(-9) },
        new ImageResourceInfo("/media/sample-poster.svg?image=podium") { Name = "podium.svg", Size = 9700, CreatedAt = Seeded.AddDays(-21), ModifiedAt = Seeded.AddDays(-20) },
        new ImageResourceInfo("/media/sample-poster.svg?image=team-portrait") { Name = "team-portrait.svg", Size = 24100, CreatedAt = Seeded.AddDays(-14), ModifiedAt = Seeded.AddDays(-14) },
        new ImageResourceInfo("/media/sample-poster.svg?image=time-trial") { Name = "time-trial.svg", Size = 7300, CreatedAt = Seeded.AddDays(-6), ModifiedAt = Seeded.AddDays(-1) },
        new ImageResourceInfo("/media/sample-poster.svg?image=sprint-finish") { Name = "sprint-finish.svg", Size = 15600, CreatedAt = Seeded.AddDays(-2), ModifiedAt = Seeded.AddHours(-5) }
    ];

    /// <inheritdoc />
    public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        // The kinds asked for apply before the slice, so its positions and total count only those.
        IEnumerable<ResourceInfo> matching = Images.Where(image => query.AsksFor(image.GetKind()));

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(image => image.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == CatalogSortDirection.Descending;

        // The sort is stable, so the images it ties keep the order of their keys.
        matching = matching.OrderBy(image => image.Source, StringComparer.Ordinal);

        matching = (query.SortBy, descending) switch
        {
            (CatalogSortField.Size, false) => matching.OrderBy(image => image.Size ?? 0),
            (CatalogSortField.Size, true) => matching.OrderByDescending(image => image.Size ?? 0),
            (CatalogSortField.DateAdded, false) => matching.OrderBy(image => image.CreatedAt),
            (CatalogSortField.DateAdded, true) => matching.OrderByDescending(image => image.CreatedAt),
            (CatalogSortField.DateModified, false) => matching.OrderBy(image => image.ModifiedAt),
            (CatalogSortField.DateModified, true) => matching.OrderByDescending(image => image.ModifiedAt),
            (_, true) => matching.OrderByDescending(image => image.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(image => image.Name, StringComparer.OrdinalIgnoreCase)
        };

        // The groups order before the sort, which the stable sort keeps within each group.
        if (query.Grouping is { } grouping)
        {
            matching = matching.OrderBy(image => grouping.IndexOf(CatalogItem.For(image)));
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
