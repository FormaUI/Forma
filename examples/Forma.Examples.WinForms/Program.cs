namespace Forma.Examples.WinForms;

using Forma.Blazor;
using Forma.Framework.Localization.Json;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Starts the application: one set of services every native window's web view resolves from, so every web view shares its culture.</summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        CultureSettings.Apply();

        var services = new ServiceCollection();
        services.AddFormaUIWebView();
        services.AddJsonLocalization();
        services.AddWindowsFormsBlazorWebView();
        services.AddSingleton<NativeWindows>();
        var provider = services.BuildServiceProvider();

        CultureSettings.SaveOnSwitch(provider);

        using var mainForm = new MainForm(provider);
        Application.Run(mainForm);
    }
}
