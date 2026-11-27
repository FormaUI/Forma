using System.Globalization;

using Forma.Blazor;
using Forma.Blazor.Browser.Components;
using Forma.Blazor.Browser.Components.Checks;
using Forma.Blazor.Browser.Components.Components;
using Forma.Framework.Localization;
using Forma.Framework.Themes;
using Forma.Framework.Themes.Blazor.Server;

using Microsoft.AspNetCore.Localization;

// The checks read what the components render in en-US, its direction, its numbers and its strings, whatever culture the machine runs in;
// a circuit starts from the culture its thread runs in, and the page tells its WebAssembly runtime the same.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

// Started from its build output, as a filtered browser run starts it, the host serves every library's static files from where
// the build left them, which the manifest beside it maps; a published host has no such manifest, and this does nothing there.
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddCheckedComponents();
builder.AddFormaServerIntegration(options =>
{
    options.InputRouting = CheckServices.InputRouting;

    // A request whose address names a theme in its given-theme query is given that theme in its dark scheme, keeping nothing.
    options.GivenThemeChoice = context => context.Request.Query["given-theme"] is { Count: > 0 } given ? new ThemeChoice(given.ToString(), ColorSchemePreference.Dark) : null;
});

// The check theme the theme checks draw pages and parts of pages in, beside Forma's two.
builder.Services.AddFormaThemes(themes => themes.Add(new ThemeDefinition("check", LocalizableText.For<App>("Check"), ColorSchemes.Light | ColorSchemes.Dark, "_content/Forma.Blazor.Browser.Components.Checks/check-theme.css")));

var app = builder.Build();

// Every check page is served under a strict Content Security Policy, so what Forma emits that it would block fails a check.
app.UseCheckContentSecurityPolicy();

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
app.UseFormaThemes();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapFormaThemes();
app.MapGet("/_check/ready", () => "ready");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);
app.Run();
