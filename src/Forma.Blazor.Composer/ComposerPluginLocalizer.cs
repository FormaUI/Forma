namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Localization;

/// <summary>Localizes the titles plugins declare.</summary>
/// <remarks>A title is looked up in the locale the plugin names first, and then in the composer's own.</remarks>
/// <param name="localeFactory">The factory of the locales plugins name.</param>
/// <param name="composerLocale">The composer's own locale.</param>
internal sealed class ComposerPluginLocalizer(IStringLocaleFactory localeFactory, IStringLocale composerLocale)
{
    private readonly Dictionary<Type, IStringLocale> _pluginLocales = [];

    /// <summary>The title in the reader's language.</summary>
    /// <param name="plugin">The plugin that declared the title, where null is the composer itself.</param>
    /// <param name="title">The English title, which is the localization key.</param>
    public string Localize(IComposerPlugin? plugin, string title)
    {
        if (plugin?.LocalizationSourceType is { } source && GetPluginLocale(source)[title] is { ResourceNotFound: false } localized)
        {
            return localized.Value;
        }

        return composerLocale[title].Value;
    }

    private IStringLocale GetPluginLocale(Type source)
    {
        if (!_pluginLocales.TryGetValue(source, out var locale))
        {
            locale = localeFactory.Create(source);
            _pluginLocales[source] = locale;
        }

        return locale;
    }
}
