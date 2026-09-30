namespace Forma.Framework.Localization.Json;

using Forma.Framework.Localization;

/// <summary>A locale for the strings belonging to <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type whose locale files hold the strings.</typeparam>
public sealed class JsonStringLocale<T> : IStringLocale<T>
{
    private readonly IStringLocale _locale;

    /// <summary>Creates a locale for <typeparamref name="T"/> from the given factory.</summary>
    public JsonStringLocale(IStringLocaleFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _locale = factory.Create(typeof(T));
    }

    /// <inheritdoc />
    public LocalizedText this[string name]
        => _locale[name];

    /// <inheritdoc />
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
        => _locale[name, values];

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name)
        => _locale.Resolve(name);

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name, params object?[] values)
        => _locale.Resolve(name, values);

    /// <inheritdoc />
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => _locale.GetAllStrings(includeParentCultures);
}
