namespace Forma.Framework.Localization.Parser;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the <c>[$ [0] keyword; name: value; $]</c> expressions inside localized text.</summary>
public static class LocaleExpressionParser
{
    /// <summary>The characters that open an expression.</summary>
    public const string Opening = "[$ ";

    /// <summary>The characters that close an expression.</summary>
    public const string Closing = " $]";

    private const string Interpolated = "[{";

    /// <summary>Rewrites the interpolation holes an interpolated key carries into plain argument indices.</summary>
    /// <param name="text">The text to examine.</param>
    /// <param name="normalized">The rewritten text, or <see langword="null"/> when the text already reads as written.</param>
    /// <remarks>
    /// An interpolated call site writes <c>[{files}]</c>, which the handler turns into <c>[{0}]</c>, while a
    /// translation file carries the canonical <c>[0]</c>. Rewriting is confined to an expression's header so
    /// that prose such as <c>See [{0}] for details</c> keeps the hole it means to fill.
    /// </remarks>
    public static bool TryNormalize(ReadOnlySpan<char> text, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (!text.Contains(Interpolated, StringComparison.Ordinal))
        {
            return false;
        }

        var builder = new StringBuilder(text.Length);
        var position = 0;

        while (true)
        {
            var opening = text[position..].IndexOf(Opening, StringComparison.Ordinal);
            if (opening < 0)
            {
                break;
            }

            var start = position + opening + Opening.Length;
            builder.Append(text[position..start]);
            position = start;

            if (!text[start..].StartsWith(Interpolated, StringComparison.Ordinal))
            {
                continue;
            }

            var digits = start + Interpolated.Length;
            var end = digits;
            while (end < text.Length && char.IsAsciiDigit(text[end]))
            {
                end++;
            }

            // Only what the handler itself wrote is rewritten, so anything other than [{ digits }] is left alone.
            if (end == digits || end + 1 >= text.Length || text[end] != '}' || text[end + 1] != ']')
            {
                continue;
            }

            builder.Append('[').Append(text[digits..end]).Append(']');
            position = end + 2;
        }

        builder.Append(text[position..]);
        normalized = builder.ToString();

        return true;
    }

    /// <summary>Reads every expression in the text, reporting what is wrong rather than throwing.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="keywords">The keywords to recognise, which decide what a field name may be.</param>
    public static LocaleParseResult Parse(string text, LocaleKeywords keywords)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(keywords);

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

            var expression = ReadExpression(text, span, inner, keywords, diagnostics);
            if (expression is not null)
            {
                expressions.Add(expression);
            }

            opening = span.End;
        }

        return new LocaleParseResult(expressions, diagnostics);
    }

    private static LocaleExpression? ReadExpression(string text, LocaleTextSpan span, LocaleTextSpan inner, LocaleKeywords keywords, List<LocaleDiagnostic> diagnostics)
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
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingFields, span));

            return null;
        }

        var header = new LocaleTextSpan(body.Start, headerEnd - body.Start);
        if (!ReadHeader(text, header, diagnostics, out var keyword, out var argument, out var index))
        {
            return null;
        }

        var defined = keywords.Find(keyword.Slice(text));
        if (defined is null)
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
            defined is null ? default : defined.FieldNames,
            diagnostics);

        // A field that failed to parse already explains the empty list, so saying so again is noise.
        if (fields.Count == 0 && diagnostics.Count == reported)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingFields, span));
        }

        return fields.Count == 0 ? null : new LocaleExpression(span, keyword, argument, index, fields);
    }

    private static bool ReadHeader(
        string text,
        LocaleTextSpan header,
        List<LocaleDiagnostic> diagnostics,
        out LocaleTextSpan keyword,
        out LocaleTextSpan argument,
        out int index)
    {
        keyword = default;
        argument = default;
        index = 0;

        var close = header.IndexOf(text, ']');
        if (header.IsEmpty || text[header.Start] != '[' || close < 0)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingArgumentIndex, header));

            return false;
        }

        argument = new LocaleTextSpan(header.Start, close - header.Start + 1);

        var digits = text.AsSpan(argument.Start + 1, argument.Length - 2);
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.InvalidArgumentIndex, argument));

            return false;
        }

        // A span is only well formed once a space and at least one more character are known to follow the index.
        if (argument.End + 1 >= header.End || text[argument.End] != ' ')
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingKeyword, header));

            return false;
        }

        keyword = new LocaleTextSpan(argument.End + 1, header.End - argument.End - 1);

        return true;
    }
}
