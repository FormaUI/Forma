namespace Forma.Blazor.Browser.Components.Client;

using Forma.Blazor.Browser.Components.Client.Services;
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.DataView;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Media;
using Forma.Framework.Resources;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers what the checked components need beside Forma's own registration, alike in every runtime.</summary>
public static class CheckServices
{
    /// <summary>Registers the data view, the composer with its default plugins, the emoji catalog, the store uploads are written to, and the providers resolving the checks' own documents and player.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddCheckedComponents(this IServiceCollection services)
        => services
            .AddFormaDataView()
            .AddFormaComposer(composer => composer
                .AddRichTextEditor(editor => editor.TryAddDefaultFeatures())
                .TryAddDefaultPlugins())
            .AddFormaTwemojiPack()
            .AddScoped<IFileStore, MemoryFileStore>()
            .AddScoped<IResourceProvider, CheckResourceProvider>()
            .AddScoped<IPlayerProvider, CheckPlayerProvider>();
}
