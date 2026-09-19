namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Plugins;
using Forma.Framework.Localization;

/// <summary>Reads a plugin's titles in the reader's language, from the locale the plugin names and then from the composer's own.</summary>
/// <param name="localeFactory">Creates the locale for a localization source a plugin names.</param>
/// <param name="composerLocale">The composer's own strings, which answer whatever a plugin's locale does not.</param>
internal sealed class ComposerPluginLocalizer(IStringLocaleFactory localeFactory, IStringLocale composerLocale)
{
    private readonly Dictionary<Type, IStringLocale> _pluginLocales = [];

    /// <summary>The title in the reader's language.</summary>
    /// <param name="plugin">The plugin that declared the title, or null for one of the composer's own.</param>
    /// <param name="title">The English title, which is the localization key.</param>
    public string Localize(IComposerPlugin? plugin, string title)
    {
        // A plugin shipped elsewhere may still use a word the composer already localizes, such as a shared group's name.
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
