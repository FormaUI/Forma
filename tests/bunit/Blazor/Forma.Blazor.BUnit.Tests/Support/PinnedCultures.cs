namespace Forma.Blazor.BUnit.Tests.Support;

using System.Globalization;

/// <summary>Cultures whose data a test pins, so a test reads alike on every platform's culture data.</summary>
public static class PinnedCultures
{
    /// <summary>Hebrew writing its own day periods, <c>לפנה״צ</c> and <c>אחה״צ</c>.</summary>
    /// <remarks>ICU 78, CLDR 48, writes Hebrew's day periods as <c>AM</c> and <c>PM</c>, as English does; a test of text one culture reads and another refuses pins the Hebrew ones.</remarks>
    public static CultureInfo Hebrew { get; } = CreateHebrew();

    /// <summary>The culture named, pinned where this class pins it.</summary>
    /// <param name="name">The culture's name.</param>
    /// <returns>The pinned culture, or the platform's own where none is pinned.</returns>
    public static CultureInfo GetCulture(string name)
        => name == Hebrew.Name ? Hebrew : CultureInfo.GetCultureInfo(name);

    private static CultureInfo CreateHebrew()
    {
        var hebrew = (CultureInfo)CultureInfo.GetCultureInfo("he").Clone();
        hebrew.DateTimeFormat.AMDesignator = "לפנה״צ";
        hebrew.DateTimeFormat.PMDesignator = "אחה״צ";

        return CultureInfo.ReadOnly(hebrew);
    }
}
