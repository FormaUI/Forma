namespace Forma.Framework.Localization.Json;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Language;

/// <summary>The state shared by every locale a factory creates.</summary>
/// <param name="Cache">The cache the locales read their translations through.</param>
/// <param name="SourceCulture">The culture the keys are written in.</param>
/// <param name="PluralEvaluator">The evaluator resolving a plural expression.</param>
/// <param name="Options">The settings the factory was created with.</param>
internal sealed record JsonLocaleContext(
    JsonResourceCache Cache,
    CultureInfo SourceCulture,
    PluralExpressionEvaluator PluralEvaluator,
    LocalizationOptions Options);
