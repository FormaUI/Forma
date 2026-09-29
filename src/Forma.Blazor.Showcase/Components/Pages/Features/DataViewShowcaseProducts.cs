namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Icons;

/// <summary>A product the data view's pages browse: its name, its category, its price and the glyph standing in for its picture.</summary>
internal sealed record ShowcaseProduct(int Id, string Name, string Category, decimal Price, Glyph Glyph);

/// <summary>The products every page and tab of the data view's showcase shows, and the grouping they share.</summary>
internal static class DataViewShowcaseProducts
{
    /// <summary>The products, as the set a data view browses.</summary>
    public static IQueryable<ShowcaseProduct> All { get; } = new[]
    {
        new ShowcaseProduct(1, "Camera", "Electronics", 549, Glyph.Camera),
        new ShowcaseProduct(2, "Headphones", "Electronics", 199, Glyph.Audio),
        new ShowcaseProduct(3, "Notebook", "Stationery", 6, Glyph.FileText),
        new ShowcaseProduct(4, "Pen set", "Stationery", 24, Glyph.Pen),
        new ShowcaseProduct(5, "Backpack", "Travel", 89, Glyph.Package),
        new ShowcaseProduct(6, "Umbrella", "Travel", 32, Glyph.Cloud),
        new ShowcaseProduct(7, "Speaker", "Electronics", 129, Glyph.Speaker),
        new ShowcaseProduct(8, "Planner", "Stationery", 18, Glyph.Clock),
        new ShowcaseProduct(9, "Luggage tag", "Travel", 9, Glyph.Bookmark),
    }.AsQueryable();

    /// <summary>A hundred thousand products numbered after the nine, the set the tabs show the data view virtualizing at its scale.</summary>
    public static IQueryable<ShowcaseProduct> Many
        => field ??= CreateMany();

    /// <summary>The products folded under their categories.</summary>
    public static DataViewGrouping<ShowcaseProduct> ByCategory { get; } = DataViewGrouping<ShowcaseProduct>.By(product => product.Category);

    private static IQueryable<ShowcaseProduct> CreateMany()
    {
        var models = All.ToArray();

        return Enumerable.Range(0, 100_000)
            .Select(index => models[index % models.Length] with { Id = index + 1, Name = $"{models[index % models.Length].Name} {index + 1}" })
            .ToArray()
            .AsQueryable();
    }
}
