namespace Forma.Examples.Wpf;

using System.Globalization;
using System.IO;

using Forma.Framework.Culture;

using Microsoft.Extensions.DependencyInjection;

/// <summary>The culture the reader chose, kept in the application's settings from one run to the next.</summary>
internal static class CultureSettings
{
    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Forma.Examples.Wpf", "culture.txt");

    /// <summary>Applies the saved culture, before the first web view opens, since the culture state starts from the culture in force as it is first resolved.</summary>
    public static void Apply()
    {
        if (File.Exists(SettingsFile) && File.ReadAllLines(SettingsFile) is [var culture, var uiCulture])
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
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllLines(SettingsFile, [cultureState.Culture.Name, cultureState.UICulture.Name]);

            return Task.CompletedTask;
        });
    }
}
