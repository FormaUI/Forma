namespace Forma.Blazor.Browser.WebView;

using System.Globalization;
using System.Resources;

/// <summary>The check host's own strings, looked up through the platform's resources in the UI culture the flow holds, as a host's native code looks its strings up.</summary>
internal static class ProbeStrings
{
    private static readonly ResourceManager Resources = new("Forma.Blazor.Browser.WebView.Resources.ProbeStrings", typeof(ProbeStrings).Assembly);

    /// <summary>The greeting, in the flow's UI culture.</summary>
    public static string Greeting
        => Resources.GetString("Greeting", CultureInfo.CurrentUICulture) ?? string.Empty;
}
