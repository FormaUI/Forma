namespace Forma.Blazor.Browser.Android;

using System.Globalization;

using Forma.Blazor;
using Forma.Blazor.Browser.Components.Checks;
using Forma.Framework.Localization.Json;

/// <summary>Builds the Android check host: the culture its checks read the components in, what the checked components need, ahead of Forma's own registration, and its web view.</summary>
public static class MauiProgram
{
    /// <summary>Creates the application.</summary>
    /// <returns>The application.</returns>
    public static MauiApp CreateMauiApp()
    {
        // The checks read what the components render in en-US, its direction, its numbers and its strings, whatever the device's
        // language: the web view's components run on the main thread, which MAUI gives the device's culture, and the culture state
        // starts from the culture of the thread it is first asked on.
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddCheckedComponents();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFormaUIWebView(options => options.InputRouting = CheckServices.InputRouting);
        builder.Services.AddJsonLocalization();

        return builder.Build();
    }
}
