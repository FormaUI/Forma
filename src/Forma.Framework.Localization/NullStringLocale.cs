namespace Forma.Framework.Localization;

using System.Globalization;

using Forma.Framework.Localization.Language;

/// <summary>A locale with no translations: every key is its own text, with its holes and plural forms resolved for the source language.</summary>
internal sealed class NullStringLocale(CultureInfo sourceCulture) : IStringLocale
{
    private readonly PluralExpressionEvaluator _pluralEvaluator = new(CommonPluralRuleSet.Default);

    public LocalizedText this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, []);
        }
    }

    public LocalizedText this[string name, params ReadOnlySpan<object?> values]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return Render(name, LocalizedFormat.Bind(name, values));
        }
    }

    public LocalizedPattern Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        LocalizedArgument[] arguments = [];

        return Resolve(name, arguments);
    }

    public LocalizedPattern Resolve(string name, params object?[] values)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(values);

        return Resolve(name, LocalizedFormat.Bind(name, values));
    }

    public IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures)
        => [];

    private LocalizedText Render(string name, LocalizedArgument[] arguments)
        => new(name, LocalizedFormat.Render(Evaluate(name, arguments), arguments, CultureInfo.CurrentCulture), ResourceNotFound: true);

    private LocalizedPattern Resolve(string name, LocalizedArgument[] arguments)
        => new(name, Evaluate(name, arguments), arguments, ResourceNotFound: true);

    // The key is source-language text; its plural forms are chosen by the source language's rules.
    private string Evaluate(string format, IReadOnlyList<LocalizedArgument> arguments)
        => _pluralEvaluator.Evaluate(format, sourceCulture, CultureInfo.CurrentUICulture, arguments);
}
