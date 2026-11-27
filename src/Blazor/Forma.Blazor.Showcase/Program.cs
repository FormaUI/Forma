using Forma.Blazor;
using Forma.Blazor.Components.DataView;
using Forma.Blazor.Showcase;
using Forma.Blazor.Showcase.Components.Pages.Menus;
using Forma.Blazor.Showcase.Components.Pages.Themes;
using Forma.Blazor.Showcase.Features.Media;
using Forma.Blazor.Showcase.Features.Upload;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Css;
using Forma.Framework.Culture.Router;
using Forma.Framework.Culture.Router.AspNetCore;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Feedback;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;
using Forma.Framework.Media;
using Forma.Framework.Resources;
using Forma.Framework.Themes;
using Forma.Framework.Themes.Blazor.Server;
using Forma.Framework.Resources.Markdown;
using Forma.Framework.Resources.Office;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddJsonLocalization();
builder.Services.AddFormaTwemojiPack();
builder.Services.AddSingleton(new SandboxAssets(builder.Environment.ContentRootPath));
// The showcase's own input routes: a preemptive palette shortcut and a fallback help key.
builder.AddFormaServerIntegration(options => options.InputRouting = new InputRouterOptions
{
    PreemptiveRoutes = [new InputRoute("app.palette", KeyBinding.Shortcut("k"))],
    Routes = [new InputRoute("app.help", new KeyBinding("F1"))],
});
builder.Services.AddFormaDataViewServices();
builder.Services.AddScoped<ShowcaseInputActions>();
builder.Services.AddScoped<IInputActionHandler>(services => services.GetRequiredService<ShowcaseInputActions>());
builder.Services.AddFormaShareNetworks();
builder.Services.AddScoped<IResourceContentReader, WebRootContentReader>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
builder.Services.AddScoped<IPlayerProvider, CustomPlayerProvider>();

// The visitor's share accounts and a sample cloud drive share target.
builder.Services.AddScoped<ShowcaseShareAccounts>();
builder.Services.AddScoped<IShareTokenSource>(services => services.GetRequiredService<ShowcaseShareAccounts>());
builder.Services.AddScoped<IShareServerSource>(services => services.GetRequiredService<ShowcaseShareAccounts>());
builder.Services.AddScoped<IShareConnections>(services => services.GetRequiredService<ShowcaseShareAccounts>());
builder.Services.AddFormaMastodonSharing();
builder.Services.AddScoped<ShowcaseShareTarget>();
builder.Services.AddScoped<IShareTarget>(services => services.GetRequiredService<ShowcaseShareTarget>());

// The stored images and the file store uploads are written to.
builder.Services.AddScoped<ICatalogProvider>(_ => new ShowcaseImageLibrary());
builder.Services.AddScoped<IFileStore, DefaultFileStore>();

// Harbor, a theme of the Showcase's own beside Forma's two, and a reader starting in Forma's light scheme, since the Showcase's
// wordmark draws in one scheme alone.
builder.Services.AddFormaThemes(themes =>
{
    themes.Add(new ThemeDefinition("harbor", LocalizableText.For<FormaThemeScopePage>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "themes/harbor.css"));
    themes.Default = new ThemeChoice("forma", ColorSchemePreference.Light);
});

// The statuses the Showcase defines, each on an opaque tint so it reads on the paper of every scheme.
builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromHex(0xecedef), CssColor.FromHex(0x2c384a)) { Marker = Glyph.Pen },
    new("scheduled", CssColor.FromHex(0xe2edfa), CssColor.FromHex(0x1c5fa8)) { Marker = Glyph.Clock },
    new("published", CssColor.FromHex(0xe1f3e5), CssColor.FromHex(0x1c7430)) { Marker = Glyph.CircleCheck },
    new("pending", CssColor.FromHex(0xfff3cd), CssColor.FromHex(0x8a6100)) { Marker = Glyph.Hourglass },
    new("rejected", CssColor.FromHex(0xf9e3e3), CssColor.FromHex(0xa32222)) { Marker = Glyph.CircleCross }
]));

// French comes from the .locales folder, as a language a host adds.
builder.Services.AddFormaCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    Switch = CultureSwitchMode.InPlace,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", UICulture = new("en") },
        new() { Token = "es", UICulture = new("es-419"), Aliases = ["es-419"] },
        new() { Token = "he", UICulture = new("he") },
        new() { Token = "fr", UICulture = new("fr") }
    ]
});

var app = builder.Build();

// The framework builds each page's address from the request's Host header, so a request no address can be built for is
// refused before it reaches a page.
app.UseMiddleware<HostAddressMiddleware>();
app.UseFormaCultureRouting();

app.UseFormaThemes();

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapFormaThemes();
app.MapRazorComponents<Forma.Blazor.Showcase.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(Forma.Blazor._Imports).Assembly,
        typeof(Forma.Blazor.Components.Explorer._Imports).Assembly,
        typeof(Forma.Blazor.Components.Composer._Imports).Assembly);

app.Run();
