namespace Forma.Blazor.Showcase.Features.Media;

using Forma.Features.Explorer;
using Forma.Framework.Resources;

/// <summary>The stored images the composer's insert dialog browses in this application.</summary>
internal sealed class ShowcaseImageLibrary : IExplorerResourcesProvider
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
    public ValueTask<ExplorerResourcePage> GetResourcesAsync(ExplorerResourceQuery query, CancellationToken cancellationToken)
    {
        IEnumerable<ResourceInfo> matching = Images;

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(image => image.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == ExplorerSortDirection.Descending;

        matching = (query.SortBy, descending) switch
        {
            (ExplorerSortField.Size, false) => matching.OrderBy(image => image.Size ?? 0),
            (ExplorerSortField.Size, true) => matching.OrderByDescending(image => image.Size ?? 0),
            (ExplorerSortField.DateAdded, false) => matching.OrderBy(image => image.CreatedAt),
            (ExplorerSortField.DateAdded, true) => matching.OrderByDescending(image => image.CreatedAt),
            (ExplorerSortField.DateModified, false) => matching.OrderBy(image => image.ModifiedAt),
            (ExplorerSortField.DateModified, true) => matching.OrderByDescending(image => image.ModifiedAt),
            (_, true) => matching.OrderByDescending(image => image.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(image => image.Name, StringComparer.OrdinalIgnoreCase)
        };

        var ordered = matching.ToList();

        return ValueTask.FromResult(new ExplorerResourcePage(
            [.. ordered.Skip(query.Skip).Take(query.Take)],
            ordered.Count));
    }
}
