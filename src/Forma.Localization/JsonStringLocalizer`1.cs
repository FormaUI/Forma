namespace Forma.Localization;

using Microsoft.Extensions.Localization;

/// <summary>Reads the strings belonging to <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type whose translation files hold the strings.</typeparam>
public sealed class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly IStringLocalizer _strings;

    /// <summary>Creates a localizer for <typeparamref name="T"/> from the given factory.</summary>
    public JsonStringLocalizer(IStringLocalizerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _strings = factory.Create(typeof(T));
    }

    /// <summary>The string registered under a key, or the key itself when none is registered.</summary>
    public LocalizedString this[string name]
        => _strings[name];

    /// <summary>The string registered under a key, formatted with the given arguments.</summary>
    public LocalizedString this[string name, params object[] arguments]
        => _strings[name, arguments];

    /// <summary>Every string reachable for the current culture.</summary>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _strings.GetAllStrings(includeParentCultures);
}
