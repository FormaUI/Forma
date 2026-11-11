using System.Globalization;

using Forma.Blazor;
using Forma.Blazor.Browser.Components.Checks;
using Forma.Blazor.Browser.Components.Components;

using Microsoft.AspNetCore.Localization;

// The checks read what the components render in en-US, its direction, its numbers and its strings, whatever culture the machine runs in;
// a circuit starts from the culture its thread runs in, and the page tells its WebAssembly runtime the same.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddCheckedComponents();
builder.AddFormaServerIntegration(options => options.InputRouting = CheckServices.InputRouting);

var app = builder.Build();

// A statically rendered check renders, and binds what its form posts, in the culture its address names in its culture query, so a
// check can read a page in another culture; every other request stays in en-US.
CultureInfo[] checkCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE")];
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new("en-US"),
    SupportedCultures = checkCultures,
    SupportedUICultures = checkCultures,
    RequestCultureProviders = [new QueryStringRequestCultureProvider()],
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/_check/ready", () => "ready");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);
app.Run();
