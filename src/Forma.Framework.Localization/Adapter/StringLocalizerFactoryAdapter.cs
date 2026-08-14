namespace Forma.Framework.Localization.Adapter;

using Forma.Framework.Localization;

using Microsoft.Extensions.Localization;

/// <summary>Presents an <see cref="IStringLocaleFactory"/> as the framework's <see cref="IStringLocalizerFactory"/>.</summary>
/// <param name="factory">The factory the locales come from.</param>
public sealed class StringLocalizerFactoryAdapter(IStringLocaleFactory factory) : IStringLocalizerFactory
{
    private readonly IStringLocaleFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <summary>A localizer for the strings belonging to a type.</summary>
    /// <param name="resourceSource">The type whose name and namespace locate the resource.</param>
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return new StringLocalizerAdapter(_factory.Create(resourceSource));
    }

    /// <summary>A localizer for a base name inside the named assembly.</summary>
    /// <param name="baseName">The resource base name.</param>
    /// <param name="location">The assembly holding the resource.</param>
    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(location);

        return new StringLocalizerAdapter(_factory.Create(baseName, location));
    }
}
