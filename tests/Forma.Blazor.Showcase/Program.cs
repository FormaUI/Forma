using Forma.Blazor.Abstractions.Components.Feedback;
using Forma.Blazor.Core.Components.Feedback;
using Forma.Blazor.Middlewares;

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
    SupportedCultures = ["en", "he", "es-419"]
});

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<Forma.Blazor.Showcase.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);

app.Run();
