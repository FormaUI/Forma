namespace Forma.Blazor.Browser.WebView;

using System.Windows;

using Forma.Blazor.Browser.WebView.Components;

using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2.Core;

/// <summary>A native window of the check host: one web view rendering the check host's router, at the address it starts at, with the web view's name.</summary>
internal sealed class WebViewHostWindow : Window
{
    private readonly BlazorWebView _webView;
    private readonly string _name;

    /// <summary>Creates the native window of the web view <paramref name="name"/> names, starting at <paramref name="startPath"/>.</summary>
    /// <param name="shell">The check host's native side, whose services the web view resolves from.</param>
    /// <param name="name">The web view's name.</param>
    /// <param name="startPath">The address its web view starts at.</param>
    public WebViewHostWindow(ProbeShell shell, string name, string startPath)
    {
        _name = name;
        Title = $"Forma WebView check {name}";

        // The web view is the size a check's page has in a browser, so a page lays itself out there as it does in one.
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;

        // Out of sight unless the check runs visibly; the web view lays its page out wherever its native window stands.
        if (Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -10000;
            Top = -10000;
            ShowInTaskbar = false;
        }

        _webView = new BlazorWebView { HostPage = "wwwroot/index.html", Services = shell.Services, StartPath = startPath, Width = 1280, Height = 720 };
        _webView.BlazorWebViewInitializing += (_, args) =>
        {
            Console.WriteLine($"Web view {name} initializing");

            // The browser's arguments and data folder the check names reach WebView2 as the environment's options too, beside its variables.
            if (Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS") is { Length: > 0 } arguments)
            {
                args.EnvironmentOptions = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = arguments };
            }

            if (Environment.GetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER") is { Length: > 0 } folder)
            {
                args.UserDataFolder = folder;
            }
        };
        _webView.BlazorWebViewInitialized += (_, _) => Console.WriteLine($"Web view {name} initialized");
        _webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(ProbeRoot),
            Parameters = new Dictionary<string, object?> { [nameof(ProbeRoot.WebView)] = name },
        });
        Content = _webView;
    }

    /// <summary>Closes the native window, disposing its web view first where <paramref name="isDisposing"/> says so.</summary>
    /// <param name="isDisposing">Whether the web view is disposed before the window closes.</param>
    /// <returns>A task that completes once the native window closed.</returns>
    public async Task CloseAsync(bool isDisposing)
    {
        try
        {
            if (isDisposing)
            {
                Console.WriteLine($"Web view {_name} disposing");
                await _webView.DisposeAsync().ConfigureAwait(true);
                Console.WriteLine($"Web view {_name} disposed");
            }
        }
        catch (Exception exception) when (Report(exception))
        {
            // Reported; the native window closes all the same.
        }
        finally
        {
            Close();
        }
    }

    // A disposal that fails is written for the browser check, since the dispatcher the close runs on keeps its task unobserved.
    private bool Report(Exception exception)
    {
        Console.Error.WriteLine($"Web view {_name} failed to dispose: {exception}");

        return true;
    }
}
