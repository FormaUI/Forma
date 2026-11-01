namespace Forma.Blazor.Browser.WebView;

using System.Globalization;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Starts the WebView check host: its services, shared by every window's web view, and its first window.</summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // A culture assigned to the UI thread, as MAUI assigns the device's, which a switch has to reach past.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var shell = new ProbeShell();
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddFormaUI();
        services.AddSingleton(shell);
        services.AddScoped(provider => new ProbeScope(provider.GetRequiredService<ProbeShell>()));
        shell.Services = services.BuildServiceProvider();

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Startup += (_, _) => shell.Open("/en/probe");
        application.Run();
    }
}
