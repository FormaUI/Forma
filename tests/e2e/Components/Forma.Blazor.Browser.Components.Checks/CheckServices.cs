namespace Forma.Blazor.Browser.Components.Checks;

using Forma.Blazor.Browser.Components.Checks.Services;
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.DataView;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Media;
using Forma.Framework.Resources;
using Forma.Framework.Share.Targets;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers what the checked components need before Forma's own registration, alike in every runtime, so what it registers takes the place of Forma's defaults.</summary>
public static class CheckServices
{
    /// <summary>The routes the host declares for the whole page.</summary>
    public static InputRouterOptions InputRouting { get; } = new() { Routes = [new InputRoute("check.host", new KeyBinding("F2"))] };

    /// <summary>Registers the data view, the composer with its default plugins, the emoji pack, the social networks a share menu offers, the store uploads are written to, the providers resolving the checks' own documents and player, and the handler of the host's own shortcuts.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddCheckedComponents(this IServiceCollection services)
        => services
            .AddFormaDataViewServices()
            .AddFormaComposer(composer => composer
                .AddRichTextEditor(editor => editor.TryAddDefaultFeatures())
                .TryAddDefaultPlugins())
            .AddFormaTwemojiPack()
            .AddFormaShareNetworks()
            .AddScoped<IFileStore, MemoryFileStore>()
            .AddScoped<IResourceProvider, CheckResourceProvider>()
            .AddScoped<IPlayerProvider, CheckPlayerProvider>()
            .AddScoped<CheckInputActions>()
            .AddScoped<IInputActionHandler>(provider => provider.GetRequiredService<CheckInputActions>());
}
