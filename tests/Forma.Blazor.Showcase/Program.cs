using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Files;
using Forma.Blazor.Components.Indicators;
using Forma.Blazor.Components.Media;
using Forma.Blazor.Css;
using Forma.Blazor.Globalization;
using Forma.Blazor.Showcase;
using Forma.Localization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddJsonLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IResourceContentReader, WebRootContentReader>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
builder.Services.AddScoped<IPlayerProvider, CustomPlayerProvider>();

// The statuses this application defines for itself — the library ships none.
builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)),
    new("scheduled", CssColor.FromRgba(48, 127, 221, 0.14), CssColor.FromHex(0x1c5fa8)),
    new("published", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430)),
    new("pending", CssColor.FromRgba(255, 193, 7, 0.20), CssColor.FromHex(0x8a6100)),
    new("rejected", CssColor.FromRgba(214, 57, 57, 0.14), CssColor.FromHex(0xa32222))
]));

builder.Services.AddCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", Culture = new("en") },
        new() { Token = "he", Culture = new("he") },
        new() { Token = "es", Culture = new("es-419"), Aliases = ["es-419"] }
    ]
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseCultureRouting();

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<Forma.Blazor.Showcase.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);

app.Run();
