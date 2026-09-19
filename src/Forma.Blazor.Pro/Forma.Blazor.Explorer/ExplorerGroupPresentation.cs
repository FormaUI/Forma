namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Localization;

/// <summary>The labels the explorer's grouping draws a field and a group with.</summary>
internal static class ExplorerGroupPresentation
{
    /// <summary>The label of <paramref name="field"/> in a grouping menu.</summary>
    public static string GetFieldLabel(IStringLocale locale, ExplorerGroupField field)
        => field switch
        {
            ExplorerGroupField.None => locale["None"].Value,
            ExplorerGroupField.Name => locale["Name"].Value,
            ExplorerGroupField.Size => locale["Size"].Value,
            ExplorerGroupField.Kind => locale["Type"].Value,
            ExplorerGroupField.DateAdded => locale["Date Added"].Value,
            ExplorerGroupField.DateModified => locale["Date Modified"].Value,
            _ => field.ToString(),
        };

    /// <summary>The heading of <paramref name="group"/>.</summary>
    public static string GetLabel(IStringLocale locale, ExplorerItemGroup group)
        => group.Band switch
        {
            ExplorerGroupBand.Kind => group.KindLabel ?? string.Empty,
            ExplorerGroupBand.NameDigits => locale["0–9"].Value,
            ExplorerGroupBand.NameAToH => locale["A–H"].Value,
            ExplorerGroupBand.NameIToP => locale["I–P"].Value,
            ExplorerGroupBand.NameQToZ => locale["Q–Z"].Value,
            ExplorerGroupBand.NameOther => locale["Other"].Value,
            ExplorerGroupBand.SizeUnspecified or ExplorerGroupBand.DateUnspecified => locale["Unspecified"].Value,
            ExplorerGroupBand.SizeEmpty => locale["Empty"].Value,
            ExplorerGroupBand.SizeTiny => locale["Tiny"].Value,
            ExplorerGroupBand.SizeSmall => locale["Small"].Value,
            ExplorerGroupBand.SizeMedium => locale["Medium"].Value,
            ExplorerGroupBand.SizeLarge => locale["Large"].Value,
            ExplorerGroupBand.SizeHuge => locale["Huge"].Value,
            ExplorerGroupBand.SizeGigantic => locale["Gigantic"].Value,
            ExplorerGroupBand.DateToday => locale["Today"].Value,
            ExplorerGroupBand.DateYesterday => locale["Yesterday"].Value,
            ExplorerGroupBand.DateEarlierThisWeek => locale["Earlier This Week"].Value,
            ExplorerGroupBand.DateLastWeek => locale["Last Week"].Value,
            ExplorerGroupBand.DateEarlierThisMonth => locale["Earlier This Month"].Value,
            ExplorerGroupBand.DateLastMonth => locale["Last Month"].Value,
            ExplorerGroupBand.DateEarlierThisYear => locale["Earlier This Year"].Value,
            ExplorerGroupBand.DateLongAgo => locale["A Long Time Ago"].Value,
            _ => group.Band.ToString(),
        };
}
