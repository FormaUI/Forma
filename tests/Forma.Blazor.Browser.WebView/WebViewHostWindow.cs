namespace Forma.Blazor.Browser.WebView;

using System.Windows;

using Forma.Blazor.Browser.WebView.Components;

using Microsoft.AspNetCore.Components.WebView.Wpf;

/// <summary>A native window of the check host: one web view rendering the check host's router, at the address it starts at, with the web view's name.</summary>
internal sealed class WebViewHostWindow : Window
{
    private readonly BlazorWebView _webView;

    /// <summary>Creates the native window of the web view <paramref name="name"/> names, starting at <paramref name="startPath"/>.</summary>
    /// <param name="shell">The check host's native side, whose services the web view resolves from.</param>
    /// <param name="name">The web view's name.</param>
    /// <param name="startPath">The address its web view starts at.</param>
    public WebViewHostWindow(ProbeShell shell, string name, string startPath)
    {
        Title = $"Forma WebView check {name}";
        Width = 1000;
        Height = 700;

        // Out of sight unless the check runs visibly; the web view lays its page out wherever its native window stands.
        if (Environment.GetEnvironmentVariable("FORMA_BROWSER_VISIBLE") is null)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -10000;
            Top = -10000;
            ShowInTaskbar = false;
        }

        _webView = new BlazorWebView { HostPage = "wwwroot/index.html", Services = shell.Services, StartPath = startPath };
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
        if (isDisposing)
        {
            await _webView.DisposeAsync().ConfigureAwait(true);
        }

        Close();
    }
}
