namespace Forma.Framework.Localization.Json;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Language;

/// <summary>The state shared by every locale a factory creates.</summary>
/// <param name="Cache">The cache the locales read their entries through.</param>
/// <param name="SourceCulture">The culture the keys are written in.</param>
/// <param name="ExpressionEvaluator">The evaluator resolving a plural or gender expression.</param>
/// <param name="Options">The settings the factory was created with.</param>
/// <param name="Diagnostics">The checks the locales run on every lookup, or null where the options leave lookups unchecked.</param>
internal sealed record JsonLocaleContext(
    JsonResourceCache Cache,
    CultureInfo SourceCulture,
    LocaleExpressionEvaluator ExpressionEvaluator,
    LocalizationOptions Options,
    JsonLocaleDiagnostics? Diagnostics)
{
    private readonly string _sourceLanguageName = GetLanguageName(SourceCulture);

    /// <summary>Whether a culture shows the keys themselves as its text: a culture of the source language, or the invariant culture, which locale files aren't written for.</summary>
    public bool IsSourceLanguage(CultureInfo culture)
        => culture.Name.Length == 0 || string.Equals(GetLanguageName(culture), _sourceLanguageName, StringComparison.Ordinal);

    // A culture's language is the one among it and its parents that the invariant culture parents.
    private static string GetLanguageName(CultureInfo culture)
    {
        var language = culture;
        while (language.Parent.Name.Length != 0)
        {
            language = language.Parent;
        }

        return language.Name;
    }
}
