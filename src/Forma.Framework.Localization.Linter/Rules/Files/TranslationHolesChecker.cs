namespace Forma.Framework.Localization.Linter.Rules.Files;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;
using Forma.Framework.Localization.Language;
using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Documents;
using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Checks that a translation fills only the holes its key names.</summary>
public sealed class TranslationHolesChecker : ILocalizationFileRuleChecker
{
    private static readonly LocaleKeywords Known = new(PluralExpressionEvaluator.Keyword);

    /// <inheritdoc />
    public LintCode Code
        => LintCodes.UnknownTranslationHole;

    /// <inheritdoc />
    public void Check(LocalizationFileContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entry in context.Document.Entries)
        {
            // The metadata entry is not a translation, and a null value is the key left as written.
            if (!entry.IsText || entry.Value is not { } translation || entry.Name == LocalizationSchema.Marker)
            {
                continue;
            }

            Compare(context, entry, translation);
        }
    }

    private static void Add(List<string> names, string name)
    {
        if (!names.Contains(name, StringComparer.Ordinal))
        {
            names.Add(name);
        }
    }

    private static void AddArguments(List<string> names, string format)
    {
        // A plural expression names its argument in brackets rather than braces.
        foreach (var expression in LocaleExpressionParser.Parse(format, Known).Expressions)
        {
            if (!expression.NameSpan.IsEmpty)
            {
                Add(names, expression.NameSpan.Slice(format).ToString());
            }
        }
    }

    private static List<string> Names(string format)
    {
        var names = new List<string>();

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            if (segment.IsHole)
            {
                Add(names, segment.Name.ToString());
            }
        }

        AddArguments(names, format);

        return names;
    }

    private void Compare(LocalizationFileContext context, LocalizationEntry entry, string translation)
    {
        var filled = Names(translation);

        if (filled.Count == 0)
        {
            return;
        }

        var named = Names(entry.Name);

        foreach (var hole in filled)
        {
            if (!named.Contains(hole, StringComparer.Ordinal))
            {
                context.Report(
                    Code,
                    entry.ValueOffset,
                    $"The translation of '{entry.Name}' fills '{hole}', which the key does not name, so it renders as written.");
            }
        }
    }
}
