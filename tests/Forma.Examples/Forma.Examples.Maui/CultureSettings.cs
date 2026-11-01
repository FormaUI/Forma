namespace Forma.Examples.Maui;

using System.Globalization;

using Forma.Framework.Culture;

using Microsoft.Extensions.DependencyInjection;

/// <summary>The culture the reader chose, kept in the application's preferences from one run to the next.</summary>
internal static class CultureSettings
{
    private const string CultureKey = "culture";
    private const string UICultureKey = "ui-culture";

    /// <summary>Applies the saved culture, before the first web view opens, since the culture state starts from the culture in force as it is first resolved.</summary>
    public static void Apply()
    {
        if (Preferences.Default.Get(CultureKey, string.Empty) is { Length: > 0 } culture && Preferences.Default.Get(UICultureKey, string.Empty) is { Length: > 0 } uiCulture)
        {
            CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        }
    }

    /// <summary>Saves the culture each time the culture state the application's web views share changes.</summary>
    /// <param name="services">The application's services.</param>
    public static void SaveOnSwitch(IServiceProvider services)
    {
        var cultureState = services.GetRequiredService<ICultureState>();
        cultureState.Subscribe(() =>
        {
            Preferences.Default.Set(CultureKey, cultureState.Culture.Name);
            Preferences.Default.Set(UICultureKey, cultureState.UICulture.Name);

            return Task.CompletedTask;
        });
    }
}
