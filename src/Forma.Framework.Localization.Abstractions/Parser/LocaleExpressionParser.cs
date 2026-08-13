namespace Forma.Framework.Localization.Parser;

using System.Globalization;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the <c>[$ keyword {0}; name: value; $]</c> expressions inside localized text.</summary>
public static class LocaleExpressionParser
{
    /// <summary>The characters that open an expression.</summary>
    public const string Opening = "[$ ";

    /// <summary>The characters that close an expression.</summary>
    public const string Closing = " $]";

    /// <summary>The keyword naming an expression whose branches are plural categories.</summary>
    public const string PluralKeyword = "plural";

    private static readonly string[] PluralCategories = ["zero", "one", "two", "few", "many", "other"];

    /// <summary>Reads every expression in the text, reporting what is wrong rather than throwing.</summary>
    /// <param name="text">The text to read.</param>
    public static LocaleParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Most text carries no expression, and this keeps it clear of every allocation below.
        var first = text.IndexOf(Opening, StringComparison.Ordinal);
        if (first < 0)
        {
            return LocaleParseResult.Empty;
        }

        var expressions = new List<LocaleExpression>();
        var diagnostics = new List<LocaleDiagnostic>();

        for (var opening = first; opening >= 0; opening = text.IndexOf(Opening, opening, StringComparison.Ordinal))
        {
            var closing = text.IndexOf(Closing, opening + Opening.Length, StringComparison.Ordinal);
            if (closing < 0)
            {
                diagnostics.Add(new LocaleDiagnostic(
                    LocaleDiagnosticKind.UnclosedExpression,
                    new LocaleTextSpan(opening, text.Length - opening)));

                break;
            }

            var span = new LocaleTextSpan(opening, closing + Closing.Length - opening);
            var inner = new LocaleTextSpan(opening + Opening.Length, closing - opening - Opening.Length);

            var expression = ReadExpression(text, span, inner, diagnostics);
            if (expression is not null)
            {
                expressions.Add(expression);
            }

            opening = span.End;
        }

        return new LocaleParseResult(expressions, diagnostics);
    }

    private static LocaleExpression? ReadExpression(string text, LocaleTextSpan span, LocaleTextSpan inner, List<LocaleDiagnostic> diagnostics)
    {
        if (inner.IsEmpty || text[inner.End - 1] != ';')
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingTrailingSemicolon, span));

            return null;
        }

        var body = new LocaleTextSpan(inner.Start, inner.Length - 1);

        var headerEnd = body.IndexOf(text, ';');
        if (headerEnd < 0)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingBranches, span));

            return null;
        }

        var header = new LocaleTextSpan(body.Start, headerEnd - body.Start);
        if (!ReadHeader(text, header, diagnostics, out var keyword, out var hole, out var index))
        {
            return null;
        }

        var isPlural = keyword.Slice(text).SequenceEqual(PluralKeyword);
        if (!isPlural)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.UnknownKeyword, keyword));
        }

        var fieldsStart = headerEnd + 1;
        if (fieldsStart < body.End && text[fieldsStart] == ' ')
        {
            fieldsStart++;
        }
        else
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MalformedFieldSeparator, new LocaleTextSpan(headerEnd, 1)));
        }

        var reported = diagnostics.Count;
        var fields = LocaleFieldParser.Parse(
            text,
            new LocaleTextSpan(fieldsStart, body.End - fieldsStart),
            isPlural ? PluralCategories : [],
            diagnostics);

        // A branch that failed to parse already explains the empty list, so saying so again is noise.
        if (fields.Count == 0 && diagnostics.Count == reported)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingBranches, span));
        }

        return fields.Count == 0 ? null : new LocaleExpression(span, keyword, hole, index, fields);
    }

    private static bool ReadHeader(
        string text,
        LocaleTextSpan header,
        List<LocaleDiagnostic> diagnostics,
        out LocaleTextSpan keyword,
        out LocaleTextSpan hole,
        out int index)
    {
        keyword = default;
        hole = default;
        index = 0;

        if (header.IsEmpty)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingKeyword, header));

            return false;
        }

        var space = header.IndexOf(text, ' ');
        if (space < 0)
        {
            // A header holding the hole alone is missing its keyword, not its hole.
            var isHole = text[header.Start] == '{' && text[header.End - 1] == '}';

            diagnostics.Add(new LocaleDiagnostic(
                isHole ? LocaleDiagnosticKind.MissingKeyword : LocaleDiagnosticKind.MissingArgumentHole,
                header));

            return false;
        }

        keyword = new LocaleTextSpan(header.Start, space - header.Start);
        if (keyword.IsEmpty)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingKeyword, header));

            return false;
        }

        hole = new LocaleTextSpan(space + 1, header.End - space - 1);
        if (hole.Length < 3 || text[hole.Start] != '{' || text[hole.End - 1] != '}')
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingArgumentHole, hole.IsEmpty ? header : hole));

            return false;
        }

        var digits = text.AsSpan(hole.Start + 1, hole.Length - 2);
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.InvalidArgumentHole, hole));

            return false;
        }

        return true;
    }
}
