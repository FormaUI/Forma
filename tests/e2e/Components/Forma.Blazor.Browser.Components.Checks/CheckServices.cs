namespace Forma.Blazor.Browser.Components.Checks;

using Forma.Blazor.Browser.Components.Checks.Services;
using Forma.Blazor.Browser.Components.Checks.Themes;
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.DataView;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Localization;
using Forma.Framework.Media;
using Forma.Framework.Resources;
using Forma.Framework.Share.Targets;
using Forma.Framework.Themes;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers what the checked components need before Forma's own registration, alike in every runtime, so what it registers takes the place of Forma's defaults.</summary>
public static class CheckServices
{
    /// <summary>The routes the host declares for the whole page.</summary>
    public static InputRouterOptions InputRouting { get; } = new() { Routes = [new InputRoute("check.host", new KeyBinding("F2"))] };

    /// <summary>The check theme the theme checks draw pages and parts of pages in, beside Forma's two.</summary>
    public static ThemeDefinition CheckTheme { get; } = new("check", LocalizableText.For<ThemesCheck>("Check"), ColorSchemes.Light | ColorSchemes.Dark, "_content/Forma.Blazor.Browser.Components.Checks/check-theme.css");

    /// <summary>Registers the data view, the composer with its default plugins, the emoji pack, the social networks a share menu offers, the check theme, the store uploads are written to, the catalog the composer's stored-files dialog browses, the providers resolving the checks' own documents and player, and the handler of the host's own shortcuts.</summary>
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
            .AddFormaThemes(themes => themes.Add(CheckTheme))
            .AddScoped<IFileStore, MemoryFileStore>()
            .AddScoped<ICatalogProvider, CheckCatalog>()
            .AddScoped<IResourceProvider, CheckResourceProvider>()
            .AddScoped<IPlayerProvider, CheckPlayerProvider>()
            .AddScoped<CheckInputActions>()
            .AddScoped<IInputActionHandler>(provider => provider.GetRequiredService<CheckInputActions>());
}
