namespace Forma.Examples.WindowsForms;

using Forma.Examples.WindowsForms.Components;
using Forma.Framework.Culture;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

/// <summary>A native window of the application, its web view rendering the home page and its title in the application's culture; Windows Forms disposes the web view with the form.</summary>
public sealed class MainForm : Form
{
    private readonly ICultureState _cultureState;
    private readonly IStringLocale _locale;

    /// <summary>Creates the native window, its web view resolving from <paramref name="services"/>.</summary>
    /// <param name="services">The application's services, which every web view shares.</param>
    public MainForm(IServiceProvider services)
    {
        ClientSize = new Size(960, 640);

        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = @"wwwroot\index.html",
            Services = services,
        };
        webView.RootComponents.Add<Home>("#app");
        Controls.Add(webView);

        _cultureState = services.GetRequiredService<ICultureState>();
        _locale = services.GetRequiredService<IStringLocale<MainForm>>();
        ShowTitle();
        _cultureState.Subscribe(OnCultureChangedAsync);
        FormClosed += (_, _) => _cultureState.Unsubscribe(OnCultureChangedAsync);
    }

    // The state notifies on the thread that switched it, and the title is the form's own thread's to change.
    private Task OnCultureChangedAsync()
        => InvokeAsync(ShowTitle);

    private void ShowTitle()
        => Text = _locale.WithCulture(_cultureState.Culture, _cultureState.UICulture)["Forma Example"];
}
