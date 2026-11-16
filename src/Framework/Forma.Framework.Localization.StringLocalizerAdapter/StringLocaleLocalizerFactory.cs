namespace Forma.Framework.Localization.StringLocalizerAdapter;

using System.Collections.Concurrent;

using Microsoft.Extensions.Localization;

/// <summary>Forma's locale factory read through Microsoft's string localizer factory, one localizer for each source.</summary>
/// <param name="localeFactory">The factory the locales come from.</param>
internal sealed class StringLocaleLocalizerFactory(IStringLocaleFactory localeFactory) : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<Type, StringLocaleLocalizer> _localizersByType = new();
    private readonly ConcurrentDictionary<(string BaseName, string Location), StringLocaleLocalizer> _localizersByName = new();

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return _localizersByType.GetOrAdd(resourceSource, static (type, factory) => new StringLocaleLocalizer(factory.Create(type)), localeFactory);
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        return _localizersByName.GetOrAdd((baseName, location), static (name, factory) => new StringLocaleLocalizer(factory.Create(name.BaseName, name.Location)), localeFactory);
    }
}
