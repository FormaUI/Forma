using System.Globalization;

using Forma.Blazor;
using Forma.Blazor.Browser.Host;
using Forma.Blazor.Browser.Host.Client.Navigation;
using Forma.Blazor.Browser.Host.Components;
using Forma.Framework.Culture.Router.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.AddFormaUIServer();
if (HostModes.UsesCultureAddresses)
{
    builder.Services.AddFormaCultureRouting(new CultureRouterOptions
    {
        Prefix = CulturePrefix.ExceptDefault,
        Switch = CultureSwitchMode.InPlace,
        DefaultCulture = "en",
        Routes =
        [
            new() { Token = "en", Culture = new("en") },
            new() { Token = "he", Culture = new("he") },
        ],
    });
}

builder.Services.AddSingleton<DelayedSwitches>();
builder.Services.AddScoped<NavigationRecorder>();

var app = builder.Build();

if (HostModes.RequestCulture is { } requestCulture)
{
    CultureInfo.DefaultThreadCurrentCulture = requestCulture;
    CultureInfo.DefaultThreadCurrentUICulture = requestCulture;

    // Every request, the circuit's own connection among them, runs in the host's one culture.
    app.Use((context, next) =>
    {
        CultureInfo.CurrentCulture = requestCulture;
        CultureInfo.CurrentUICulture = requestCulture;

        return next(context);
    });
}

if (HostModes.UsesCultureAddresses)
{
    app.UseFormaCultureRouting();
}

// Routed after the culture segment is removed, which an implicit routing at the pipeline's start would precede.
app.UseRouting();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/_check/delayed", (DelayedSwitches switches) => switches.Count);
app.MapGet("/_check/delayed-failures", (DelayedSwitches switches) => switches.Failures);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor.Browser.Host.Client.Components.InteractionPanel).Assembly);
app.Run();
