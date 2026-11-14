namespace Forma.Tests.Localization;

using Forma.Framework.Localization;

/// <summary>A string locale factory whose locales all serve the same entries, keeping the type each locale was asked for.</summary>
/// <param name="entries">The entries every locale serves.</param>
public sealed class StubStringLocaleFactory(IReadOnlyDictionary<string, string> entries) : IStringLocaleFactory
{
    private readonly List<Type> _requestedTypes = [];

    /// <summary>The types locales were asked for, in the order asked.</summary>
    public IReadOnlyList<Type> RequestedTypes
        => _requestedTypes;

    /// <inheritdoc />
    public IStringLocale Create(Type resourceSource)
    {
        _requestedTypes.Add(resourceSource);

        return new StubStringLocale(entries);
    }

    /// <inheritdoc />
    public IStringLocale Create(string baseName, string location)
        => new StubStringLocale(entries);
}
