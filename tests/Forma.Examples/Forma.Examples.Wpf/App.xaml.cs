namespace Forma.Examples.Wpf;

using System.Windows;

using Forma.Blazor;
using Forma.Framework.Localization.Json;

using Microsoft.Extensions.DependencyInjection;

/// <summary>The application: one set of services every native window's web view resolves from, so every web view shares its culture.</summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureSettings.Apply();

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddFormaUIWebView();
        services.AddJsonLocalization();
        services.AddSingleton<NativeWindows>();
        var provider = services.BuildServiceProvider();

        CultureSettings.SaveOnSwitch(provider);
        provider.GetRequiredService<NativeWindows>().Open();
    }
}
