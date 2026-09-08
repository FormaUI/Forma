namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Icons;
using Forma.Framework.Localization;

/// <summary>The glyphs and labels the explorer's sort controls draw a field and a direction with.</summary>
internal static class ExplorerSortPresentation
{
    /// <summary>The glyph marking <paramref name="direction"/> on <paramref name="field"/>: names and kinds sort alphabetically, sizes and dates numerically.</summary>
    public static Glyph GetGlyph(ExplorerSortField field, ExplorerSortDirection direction)
        => (field is ExplorerSortField.Name or ExplorerSortField.Kind, direction is ExplorerSortDirection.Ascending) switch
        {
            (true, true) => Glyph.SortAlphaAscending,
            (true, false) => Glyph.SortAlphaDescending,
            (false, true) => Glyph.SortNumericAscending,
            (false, false) => Glyph.SortNumericDescending,
        };

    /// <summary>The label of <paramref name="field"/>.</summary>
    public static string GetFieldLabel(IStringLocale<FormaExplorer> locale, ExplorerSortField field)
        => field switch
        {
            ExplorerSortField.Name => locale["Name"].Value,
            ExplorerSortField.Size => locale["Size"].Value,
            ExplorerSortField.Kind => locale["Type"].Value,
            ExplorerSortField.DateAdded => locale["Date Added"].Value,
            ExplorerSortField.DateModified => locale["Date Modified"].Value,
            _ => field.ToString(),
        };

    /// <summary>The label of <paramref name="direction"/>.</summary>
    public static string GetDirectionLabel(IStringLocale<FormaExplorer> locale, ExplorerSortDirection direction)
        => direction == ExplorerSortDirection.Ascending ? locale["Ascending"].Value : locale["Descending"].Value;
}
