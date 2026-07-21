using Site.UI.Abstractions.Components.Feedback;
using Site.UI.Core.Components.Feedback;
using Site.UI.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseCultureRouting(new CultureRouteOptions
{
    DefaultCulture = "en",
    SupportedCultures = ["en", "he"]
});

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<Site.UI.Showcase.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Site.UI._Imports).Assembly);

app.Run();
