using System.Globalization;

using Forma.Blazor;
using Forma.Blazor.Browser.Host;
using Forma.Blazor.Browser.Host.Client.Navigation;
using Forma.Blazor.Browser.Host.Components;
using Forma.Framework.Culture;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Culture.Router;
using Forma.Framework.Culture.Router.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.AddFormaServerIntegration(options =>
{
    options.CultureArrangement = HostModes.Arrangement;
    options.CultureSwitchPolicy = HostModes.UsesCultureCookie ? new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie } : null;
});
if (HostModes.Arrangement is CultureArrangement.Independent)
{
    // The client's culture is its own, so the server hands it nothing.
    builder.Configuration.AddInMemoryCollection([new("Components:UseCultureFromServer", "false")]);
}

if (HostModes.UsesCultureAddresses)
{
    builder.Services.AddFormaCultureRouting(new CultureRouterOptions
    {
        Prefix = CulturePrefix.ExceptDefault,
        Switch = CultureSwitchMode.InPlace,

        // Short enough for the check of a switch a guard refuses, long enough for any navigation that lands on a busy machine.
        SwitchTimeout = TimeSpan.FromSeconds(10),
        DefaultCulture = "en",
        Routes =
        [
            new() { Token = "en", UICulture = new("en") },
            new() { Token = "he", UICulture = new("he") },
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

if (HostModes.UsesCultureCookie)
{
    app.UseRequestLocalization(new RequestLocalizationOptions()
        .SetDefaultCulture("en-US")
        .AddSupportedCultures("en-US", "he")
        .AddSupportedUICultures("en-US", "he"));
}

// Routed after the culture segment is removed, which an implicit routing at the pipeline's start would precede.
app.UseRouting();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/_check/delayed", (DelayedSwitches switches) => switches.Count);
app.MapGet("/_check/delayed-failures", (DelayedSwitches switches) => switches.Failures);
app.MapPost("/_check/delayed/release", (DelayedSwitches switches, CancellationToken cancellationToken) => switches.ReleaseAsync(cancellationToken));
app.MapGet("/_check/delayed/settled", (int after, DelayedSwitches switches, CancellationToken cancellationToken) => switches.WaitForSettledAsync(after, cancellationToken));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor.Browser.Host.Client.Components.InteractionPanel).Assembly);
app.Run();
