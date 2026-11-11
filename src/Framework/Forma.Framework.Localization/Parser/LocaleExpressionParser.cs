namespace Forma.Framework.Localization.Parser;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the <c>[$ [name] keyword; name: value; $]</c> expressions inside localized text.</summary>
public static class LocaleExpressionParser
{
    /// <summary>The characters that open an expression.</summary>
    public const string OpeningDelimiter = "[$ ";

    /// <summary>The characters that close an expression.</summary>
    public const string ClosingDelimiter = " $]";

    private const string InterpolationDelimiter = "[{";

    /// <summary>Rewrites the interpolation holes an interpolated key carries into plain argument names.</summary>
    /// <param name="text">The text to examine.</param>
    /// <param name="normalizedText">The rewritten text, where <see langword="null"/> means the text already reads as written.</param>
    /// <remarks>
    /// Only an expression's header is rewritten, from the <c>[{files}]</c> an interpolated call site writes to the
    /// <c>[files]</c> a locale file carries; prose such as <c>See [{files}] for details</c> keeps its hole.
    /// </remarks>
    public static bool TryNormalize(ReadOnlySpan<char> text, [NotNullWhen(true)] out string? normalizedText)
    {
        normalizedText = null;

        if (!text.Contains(InterpolationDelimiter, StringComparison.Ordinal))
        {
            return false;
        }

        var builder = new StringBuilder(text.Length);
        var position = 0;

        while (true)
        {
            var openingIndex = text[position..].IndexOf(OpeningDelimiter, StringComparison.Ordinal);
            if (openingIndex < 0)
            {
                break;
            }

            var start = position + openingIndex + OpeningDelimiter.Length;
            builder.Append(text[position..start]);
            position = start;

            if (!text[start..].StartsWith(InterpolationDelimiter, StringComparison.Ordinal))
            {
                continue;
            }

            var name = start + InterpolationDelimiter.Length;
            var end = name;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                end++;
            }

            if (!LocaleName.IsValid(text[name..end]) || end + 1 >= text.Length || text[end] != '}' || text[end + 1] != ']')
            {
                continue;
            }

            builder.Append('[').Append(text[name..end]).Append(']');
            position = end + 2;
        }

        builder.Append(text[position..]);
        normalizedText = builder.ToString();

        return true;
    }

    /// <summary>Reads every expression in the text, with a diagnostic for each problem found.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="keywords">The keywords to recognize, which decide what a field name may be.</param>
    public static LocaleParseResult Parse(string text, LocaleKeywords keywords)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(keywords);

        var firstOpeningIndex = text.IndexOf(OpeningDelimiter, StringComparison.Ordinal);
        if (firstOpeningIndex < 0)
        {
            return LocaleParseResult.Empty;
        }

        var expressions = new List<LocaleExpression>();
        var diagnostics = new List<LocaleDiagnostic>();

        for (var openingIndex = firstOpeningIndex; openingIndex >= 0; openingIndex = text.IndexOf(OpeningDelimiter, openingIndex, StringComparison.Ordinal))
        {
            var closingIndex = text.IndexOf(ClosingDelimiter, openingIndex + OpeningDelimiter.Length, StringComparison.Ordinal);
            if (closingIndex < 0)
            {
                diagnostics.Add(new LocaleDiagnostic(
                    LocaleDiagnosticKind.UnclosedExpression,
                    new LocaleTextSpan(openingIndex, text.Length - openingIndex)));

                break;
            }

            var span = new LocaleTextSpan(openingIndex, closingIndex + ClosingDelimiter.Length - openingIndex);
            var innerSpan = new LocaleTextSpan(openingIndex + OpeningDelimiter.Length, closingIndex - openingIndex - OpeningDelimiter.Length);

            var expression = ReadExpression(text, span, innerSpan, keywords, diagnostics);
            if (expression is not null)
            {
                expressions.Add(expression);
            }

            openingIndex = span.End;
        }

        return new LocaleParseResult(expressions, diagnostics);
    }

    private static LocaleExpression? ReadExpression(string text, LocaleTextSpan span, LocaleTextSpan innerSpan, LocaleKeywords keywords, List<LocaleDiagnostic> diagnostics)
    {
        if (innerSpan.IsEmpty || text[innerSpan.End - 1] != ';')
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingTrailingSemicolon, span));

            return null;
        }

        var body = new LocaleTextSpan(innerSpan.Start, innerSpan.Length - 1);

        var headerEnd = body.IndexOf(text, ';');
        if (headerEnd < 0)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingFields, span));

            return null;
        }

        var header = new LocaleTextSpan(body.Start, headerEnd - body.Start);
        if (!ReadHeader(text, header, diagnostics, out var keyword, out var argument, out var argumentName))
        {
            return null;
        }

        var definedKeyword = keywords.Find(keyword.Slice(text));
        if (definedKeyword is null)
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

        var reportedCount = diagnostics.Count;
        var fields = LocaleFieldParser.Parse(
            text,
            new LocaleTextSpan(fieldsStart, body.End - fieldsStart),
            definedKeyword is null ? default : definedKeyword.FieldNames,
            diagnostics);

        // A missing-fields diagnostic is added only where no field diagnostic explains the empty list.
        if (fields.Count == 0 && diagnostics.Count == reportedCount)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingFields, span));
        }

        return fields.Count == 0 ? null : new LocaleExpression(span, keyword, argument, argumentName, fields);
    }

    private static bool ReadHeader(
        string text,
        LocaleTextSpan header,
        List<LocaleDiagnostic> diagnostics,
        out LocaleTextSpan keyword,
        out LocaleTextSpan argument,
        out LocaleTextSpan argumentName)
    {
        keyword = default;
        argument = default;
        argumentName = default;

        if (header.IsEmpty)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingKeyword, header));

            return false;
        }

        // An expression naming no argument reads its value from the sole argument the caller supplied.
        if (text[header.Start] != '[')
        {
            keyword = header;

            return true;
        }

        var close = header.IndexOf(text, ']');
        if (close < 0)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingArgumentName, header));

            return false;
        }

        argument = new LocaleTextSpan(header.Start, close - header.Start + 1);
        argumentName = new LocaleTextSpan(argument.Start + 1, argument.Length - 2);

        if (!LocaleName.IsValid(text.AsSpan(argumentName.Start, argumentName.Length)))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.InvalidArgumentName, argument));

            return false;
        }

        // A span is only well formed once a space and at least one more character are known to follow the name.
        if (argument.End + 1 >= header.End || text[argument.End] != ' ')
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingKeyword, header));

            return false;
        }

        keyword = new LocaleTextSpan(argument.End + 1, header.End - argument.End - 1);

        return true;
    }
}
