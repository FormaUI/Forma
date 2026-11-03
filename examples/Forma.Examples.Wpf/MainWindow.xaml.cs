namespace Forma.Examples.Wpf;

using System.Windows;

using Forma.Framework.Culture;
using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;

/// <summary>A native window of the application, its web view rendering the home page and its title in the application's culture.</summary>
public partial class MainWindow : Window
{
    private readonly ICultureState _cultureState;
    private readonly IStringLocale _locale;

    /// <summary>Creates the native window, its web view resolving from <paramref name="services"/>.</summary>
    /// <param name="services">The application's services, which every web view shares.</param>
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        WebView.Services = services;
        _cultureState = services.GetRequiredService<ICultureState>();
        _locale = services.GetRequiredService<IStringLocale<MainWindow>>();
        ShowTitle();
        _cultureState.Subscribe(OnCultureChangedAsync);
    }

    // The state notifies on the thread that switched it, and the title is the native window's own thread's to change.
    private Task OnCultureChangedAsync()
        => Dispatcher.InvokeAsync(ShowTitle).Task;

    private void ShowTitle()
        => Title = _locale.WithCulture(_cultureState.Culture, _cultureState.UICulture)["Forma Example"];

    // WPF disposes no control on its own, so the web view is disposed as its native window closes, which releases its scope. The
    // WebView2 control's own disposal can throw moments after a keyboard shortcut, once the web view's scope was released already.
    private async void OnClosed(object? sender, EventArgs e)
    {
        _cultureState.Unsubscribe(OnCultureChangedAsync);

        try
        {
            await WebView.DisposeAsync();
        }
        catch (InvalidCastException)
        {
        }
    }
}
