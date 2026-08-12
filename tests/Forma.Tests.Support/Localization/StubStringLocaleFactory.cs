namespace Forma.Tests.Localization;

using Forma.Framework.Localization;

/// <summary>A factory handing out stub locales, all serving the same translations.</summary>
/// <param name="translations">The translations every locale it creates will serve.</param>
public sealed class StubStringLocaleFactory(IReadOnlyDictionary<string, string> translations) : IStringLocaleFactory
{
    /// <summary>A locale for the resource that belongs to a type.</summary>
    public IStringLocale Create(Type resourceSource)
        => new StubStringLocale(translations);

    /// <summary>A locale for the resource named by a base name under a location.</summary>
    public IStringLocale Create(string baseName, string location)
        => new StubStringLocale(translations);
}
