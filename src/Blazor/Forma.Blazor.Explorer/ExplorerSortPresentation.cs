namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Icons;
using Forma.Framework.Localization;

/// <summary>The glyphs and labels the explorer's sort controls draw a field and a direction with.</summary>
internal static class ExplorerSortPresentation
{
    /// <summary>The glyph marking <paramref name="direction"/> on <paramref name="field"/>.</summary>
    public static Glyph GetGlyph(CatalogSortField field, CatalogSortDirection direction)
        => (field is CatalogSortField.Name or CatalogSortField.Kind, direction is CatalogSortDirection.Ascending) switch
        {
            (true, true) => Glyph.SortAlphaAscending,
            (true, false) => Glyph.SortAlphaDescending,
            (false, true) => Glyph.SortNumericAscending,
            (false, false) => Glyph.SortNumericDescending,
        };

    /// <summary>The label of <paramref name="field"/>.</summary>
    public static string GetFieldLabel(IStringLocale locale, CatalogSortField field)
        => field switch
        {
            CatalogSortField.Name => locale["Name"].Value,
            CatalogSortField.Size => locale["Size"].Value,
            CatalogSortField.Kind => locale["Type"].Value,
            CatalogSortField.DateAdded => locale["Date Added"].Value,
            CatalogSortField.DateModified => locale["Date Modified"].Value,
            _ => field.ToString(),
        };

    /// <summary>The label of <paramref name="direction"/>.</summary>
    public static string GetDirectionLabel(IStringLocale locale, CatalogSortDirection direction)
        => direction == CatalogSortDirection.Ascending ? locale["Ascending"].Value : locale["Descending"].Value;
}
