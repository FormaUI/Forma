using Forma.Blazor;
using Forma.Examples.Independent.Components;
using Forma.Framework.Culture;
using Forma.Framework.Localization.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaUIServer(options => options.CultureArrangement = CultureArrangement.Independent);

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Examples.Independent.Client._Imports).Assembly);

app.Run();
