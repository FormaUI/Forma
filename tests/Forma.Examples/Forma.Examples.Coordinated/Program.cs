using Forma.Blazor;
using Forma.Examples.Coordinated.Components;
using Forma.Framework.Localization.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddJsonLocalization();
builder.AddFormaUIServer();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Examples.Coordinated.Client._Imports).Assembly);

app.Run();
