namespace Forma.Tests.Localization;

using Forma.Framework.Localization;

/// <summary>A string locale for <typeparamref name="T"/> over translations held in memory.</summary>
/// <typeparam name="T">The type the locale is for.</typeparam>
/// <param name="translations">The translations to serve, keyed by the English source text.</param>
public sealed class StubStringLocale<T>(IReadOnlyDictionary<string, string> translations) : IStringLocale<T>
{
    private readonly StubStringLocale _locale = new(translations);

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
