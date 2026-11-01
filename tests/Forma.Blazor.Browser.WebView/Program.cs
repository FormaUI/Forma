namespace Forma.Blazor.Browser.WebView;

using System.Globalization;
using System.Windows;

using Forma.Framework.Culture.Router;
using Forma.Framework.Localization.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

/// <summary>Starts the WebView check host: its services, shared by every web view, and its first web view.</summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // What the browser check reports where a web view never comes up: the WebView2 runtime the host found, or none.
        try
        {
            Console.WriteLine($"WebView2 runtime {CoreWebView2Environment.GetAvailableBrowserVersionString()}");
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            Console.Error.WriteLine($"No WebView2 runtime is installed: {exception.Message}");
            Environment.Exit(2);
        }

        // A culture assigned to the UI thread, as MAUI assigns the device's, which a switch has to reach past.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var shell = new ProbeShell();
        var services = new ServiceCollection();

        // Forma's registrations resolve the same before the platform's as after them, culture routing's among them.
        if (Environment.GetEnvironmentVariable("FORMA_WEBVIEW_ORDER") == "forma-first")
        {
            AddRouting(services);
            services.AddJsonLocalization();
            services.AddFormaUIWebView();
            services.AddWpfBlazorWebView();
        }
        else
        {
            services.AddWpfBlazorWebView();
            services.AddFormaUIWebView();
            AddRouting(services);
            services.AddJsonLocalization();
        }

        services.AddSingleton(shell);
        services.AddScoped(provider => new ProbeScope(provider.GetRequiredService<ProbeShell>()));
        shell.Services = services.BuildServiceProvider();

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.DispatcherUnhandledException += (_, args) => Console.Error.WriteLine(args.Exception);
        application.Startup += (_, _) => shell.Open("/en/probe");
        application.Run();
    }

    private static void AddRouting(IServiceCollection services)
        => services.AddFormaCultureRouting(new CultureRouterOptions
        {
            DefaultCulture = "en",
            Switch = CultureSwitchMode.InPlace,
            Routes =
            [
                new() { Token = "en", UICulture = CultureInfo.GetCultureInfo("en-US") },
                new() { Token = "es", UICulture = CultureInfo.GetCultureInfo("es-ES") },
                new() { Token = "he", UICulture = CultureInfo.GetCultureInfo("he") },
            ],
        });
}
