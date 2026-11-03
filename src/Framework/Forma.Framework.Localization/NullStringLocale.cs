namespace Forma.Framework.Localization;

using System.Globalization;

using Forma.Framework.Localization.Language;

/// <summary>A locale whose every key is its own text, with its holes and plural forms resolved for the source language.</summary>
/// <param name="sourceCulture">The language the keys are written in.</param>
/// <param name="boundCulture">The culture that formats the values, or <see langword="null"/> to read the current one.</param>
/// <param name="boundUICulture">The culture whose plural rules apply, or <see langword="null"/> to read the current one.</param>
internal sealed class NullStringLocale(CultureInfo sourceCulture, CultureInfo? boundCulture = null, CultureInfo? boundUICulture = null) : IStringLocale
{
    private readonly PluralExpressionEvaluator _pluralEvaluator = new(CommonPluralRuleSet.Default);

    private CultureInfo Culture
        => boundCulture ?? CultureInfo.CurrentCulture;

    private CultureInfo UICulture
        => boundUICulture ?? CultureInfo.CurrentUICulture;

    /// <inheritdoc />
    public LocalizedText this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, []);
        }
    }

    /// <inheritdoc />
    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, LocalizedFormat.Bind(name, values));
        }
    }

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    /// <inheritdoc />
    public LocalizedPattern Resolve(string name, params object?[] values)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(values);

        return Resolve(name, LocalizedFormat.Bind(name, values));
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => [];

    /// <inheritdoc />
    public IStringLocale WithCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        return new NullStringLocale(sourceCulture, culture, uiCulture);
    }

    private LocalizedText Render(string name, LocalizedArgument[] arguments)
        => new(name, LocalizedFormat.Render(Evaluate(name, arguments), arguments, Culture, UICulture), ResourceNotFound: true);

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
        => new(name, Evaluate(name, arguments), arguments, ResourceNotFound: true);

    private string Evaluate(string format, IReadOnlyList<LocalizedArgument> arguments)
        => _pluralEvaluator.Evaluate(format, sourceCulture, UICulture, arguments);
}
