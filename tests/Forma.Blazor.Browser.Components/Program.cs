using Forma.Blazor;
using Forma.Blazor.Browser.Components.Client;
using Forma.Blazor.Browser.Components.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.AddFormaUIServer();
builder.Services.AddCheckedComponents();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/_check/ready", () => "ready");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);
app.Run();
