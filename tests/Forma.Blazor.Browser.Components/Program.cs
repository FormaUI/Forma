using Forma.Blazor;
using Forma.Blazor.Browser.Components.Checks;
using Forma.Blazor.Browser.Components.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddCheckedComponents();
builder.AddFormaUIServer(options => options.InputRouting = CheckServices.InputRouting);

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/_check/ready", () => "ready");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);
app.Run();
