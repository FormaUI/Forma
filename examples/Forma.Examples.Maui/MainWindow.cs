namespace Forma.Examples.Maui;

using Forma.Framework.Culture;
using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;

/// <summary>A native window of the application, its web view rendering the home page and its title in the application's culture.</summary>
public sealed class MainWindow : Window
{
    private readonly ICultureState _cultureState;
    private readonly IStringLocale _locale;

    /// <summary>Creates the native window, its web view resolving from the application's services.</summary>
    /// <param name="services">The application's services, which every web view shares.</param>
    public MainWindow(IServiceProvider services)
        : base(new MainPage())
    {
        _cultureState = services.GetRequiredService<ICultureState>();
        _locale = services.GetRequiredService<IStringLocale<MainWindow>>();
        ShowTitle();
        _cultureState.Subscribe(OnCultureChangedAsync);
        Destroying += (_, _) => _cultureState.Unsubscribe(OnCultureChangedAsync);
    }

    // The state notifies on the thread that switched it, and the title is the native window's own thread's to change.
    private Task OnCultureChangedAsync()
        => Dispatcher.DispatchAsync(ShowTitle);

    private void ShowTitle()
        => Title = _locale.WithCulture(_cultureState.Culture, _cultureState.UICulture)["Forma Example"];
}
