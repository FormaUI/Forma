using Forma.Blazor;
using Forma.Blazor.Globalization;
using Forma.Blazor.Showcase;
using Forma.Blazor.Showcase.Components.Pages.Menus;
using Forma.Blazor.Showcase.Features.Media;
using Forma.Blazor.Showcase.Features.Upload;
using Forma.Features.Explorer;
using Forma.Features.Feedback;
using Forma.Features.Media;
using Forma.Framework.Css;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Icons;
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;
using Forma.Framework.Resources.Office;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddJsonLocalization();
builder.Services.AddFormaTwemojiPack();
builder.Services.AddSingleton(new SandboxAssets(builder.Environment.ContentRootPath));
builder.Services.AddFormaUI();
builder.Services.AddFormaShareNetworks();
builder.Services.AddScoped<IResourceContentReader, WebRootContentReader>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
builder.Services.AddScoped<IPlayerProvider, CustomPlayerProvider>();

// A stand-in cloud drive, so the share menu page can show a host-registered destination.
builder.Services.AddScoped<ShowcaseShareTarget>();
builder.Services.AddScoped<IShareTarget>(services => services.GetRequiredService<ShowcaseShareTarget>());

// What the composer's image dialog browses and uploads into; without these it offers an address alone.
builder.Services.AddScoped<IExplorerResourcesProvider>(_ => new ShowcaseImageLibrary());
builder.Services.AddScoped<IFileStore, DefaultFileStore>();

// The statuses this application defines for itself — the library ships none.
builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)) { Marker = Glyph.Pen },
    new("scheduled", CssColor.FromRgba(48, 127, 221, 0.14), CssColor.FromHex(0x1c5fa8)) { Marker = Glyph.Clock },
    new("published", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430)) { Marker = Glyph.CircleCheck },
    new("pending", CssColor.FromRgba(255, 193, 7, 0.20), CssColor.FromHex(0x8a6100)) { Marker = Glyph.Hourglass },
    new("rejected", CssColor.FromRgba(214, 57, 57, 0.14), CssColor.FromHex(0xa32222)) { Marker = Glyph.CircleCross }
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
