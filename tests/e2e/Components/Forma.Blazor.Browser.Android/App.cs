namespace Forma.Blazor.Browser.Android;

using Forma.Blazor.Browser.Components.Checks;

using Microsoft.AspNetCore.Components.WebView.Maui;

/// <summary>The application, its one window a web view rendering the check pages from the first of them.</summary>
public sealed class App : Application
{
    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var webView = new BlazorWebView { HostPage = "wwwroot/index.html", StartPath = "/android/field" };
        webView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Routes) });

        return new Window(new ContentPage { Content = webView });
    }
}
