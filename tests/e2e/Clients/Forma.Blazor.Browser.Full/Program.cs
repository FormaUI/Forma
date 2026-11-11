using Forma.Blazor;
using Forma.Blazor.Browser.Full;
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.DataView;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;
using Forma.Framework.Resources.Office;
using Forma.Framework.Share.Targets;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddJsonLocalization();
builder.Services.AddFormaTwemojiPack();
builder.Services.AddFormaWebAssemblyIntegration();
builder.Services.AddFormaDataViewServices();
builder.Services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor
        .TryAddDefaultFeatures()
        .AddFeaturePlugin<ExplorerPlugin>())
    .TryAddDefaultPlugins());
builder.Services.AddFormaShareNetworks();
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
await builder.Build().RunAsync().ConfigureAwait(false);
