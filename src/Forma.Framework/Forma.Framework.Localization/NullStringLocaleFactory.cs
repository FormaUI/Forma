namespace Forma.Framework.Localization;

using System.Globalization;

/// <summary>A factory whose locales speak the source language, each key being its own text.</summary>
public sealed class NullStringLocaleFactory : IStringLocaleFactory
{
    private readonly NullStringLocale _locale = new(CultureInfo.GetCultureInfo(new LocalizationOptions().SourceLanguage));

    private NullStringLocaleFactory()
    {
    }

    /// <summary>The shared instance.</summary>
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
