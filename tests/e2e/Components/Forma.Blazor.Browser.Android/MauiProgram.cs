namespace Forma.Blazor.Browser.Android;

using Forma.Blazor;
using Forma.Blazor.Browser.Components.Checks;
using Forma.Framework.Localization.Json;

/// <summary>Builds the Android check host: what the checked components need, ahead of Forma's own registration, and its web view.</summary>
public static class MauiProgram
{
    /// <summary>Creates the application.</summary>
    /// <returns>The application.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddCheckedComponents();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFormaUIWebView(options => options.InputRouting = CheckServices.InputRouting);
        builder.Services.AddJsonLocalization();

        return builder.Build();
    }
}
