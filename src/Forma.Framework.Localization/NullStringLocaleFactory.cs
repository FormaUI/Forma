namespace Forma.Framework.Localization;

using System.Globalization;

/// <summary>The factory a component gets where none is registered: every locale it creates speaks the source language, each key being its own text.</summary>
public sealed class NullStringLocaleFactory : IStringLocaleFactory
{
    private readonly NullStringLocale _locale = new(CultureInfo.GetCultureInfo(new LocalizationOptions().SourceLanguage));

    private NullStringLocaleFactory()
    {
    }

    /// <summary>The one instance, since the factory holds no files.</summary>
    public static NullStringLocaleFactory Instance { get; } = new();

    /// <inheritdoc />
    public IStringLocale Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return _locale;
    }

    /// <inheritdoc />
    public IStringLocale Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        return _locale;
    }
}
