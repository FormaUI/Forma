namespace Forma.Framework.Localization.Json;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Language;

/// <summary>What every locale a factory creates reads from and abides by.</summary>
/// <param name="Cache">The files read so far, shared so two locales never parse one twice.</param>
/// <param name="SourceCulture">The culture the keys are written in, resolved from the options.</param>
/// <param name="PluralEvaluator">The evaluator resolving a plural expression.</param>
/// <param name="Options">The settings the factory was created with.</param>
internal sealed record JsonLocaleContext(
    JsonResourceCache Cache,
    CultureInfo SourceCulture,
    PluralExpressionEvaluator PluralEvaluator,
    LocalizationOptions Options);
