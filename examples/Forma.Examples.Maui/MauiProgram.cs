namespace Forma.Examples.Maui;

using Forma.Blazor;
using Forma.Framework.Browser.Interop.Operations.Maui;
using Forma.Framework.Localization.Json;

/// <summary>Builds the application: one set of services every native window's web view resolves from, so every web view shares its culture.</summary>
public static class MauiProgram
{
    /// <summary>Creates the application.</summary>
    /// <returns>The application.</returns>
    public static MauiApp CreateMauiApp()
    {
        CultureSettings.Apply();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFormaUIWebView();
        builder.Services.AddFormaMauiOperations();
        builder.Services.AddJsonLocalization();
        builder.Services.AddSingleton<NativeWindows>();
        var app = builder.Build();

        CultureSettings.SaveOnSwitch(app.Services);

        return app;
    }
}
