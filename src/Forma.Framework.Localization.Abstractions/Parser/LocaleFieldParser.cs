namespace Forma.Framework.Localization.Parser;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the <c>name: value;</c> pairs that make up an expression body or a metadata entry.</summary>
internal static class LocaleFieldParser
{
    private const string Separator = "; ";

    /// <summary>Splits a body into its pairs, reporting what is wrong rather than throwing.</summary>
    /// <param name="text">The whole text the offsets index into.</param>
    /// <param name="body">The range holding the pairs, without the trailing semicolon.</param>
    /// <param name="knownNames">The names this body may use, or empty to accept any name.</param>
    /// <param name="diagnostics">Collects what was found wrong.</param>
    public static List<LocaleField> Parse(
        string text,
        LocaleTextSpan body,
        ReadOnlySpan<string> knownNames,
        List<LocaleDiagnostic> diagnostics)
    {
        var fields = new List<LocaleField>();
        var start = body.Start;

        for (var position = body.Start; position < body.End; position++)
        {
            if (!IsSplit(text, position, body.End, knownNames, diagnostics, out var separator))
            {
                continue;
            }

            if (ReadField(text, new LocaleTextSpan(start, position - start), knownNames, diagnostics) is { } field)
            {
                fields.Add(field);
            }

            position += separator - 1;
            start = position + 1;
        }

        if (ReadField(text, new LocaleTextSpan(start, body.End - start), knownNames, diagnostics) is { } last)
        {
            fields.Add(last);
        }

        ReportDuplicates(text, fields, diagnostics);

        return fields;
    }

    // A semicolon only ends a pair when a name this body knows follows it, so prose keeps its own punctuation.
    private static bool IsSplit(
        string text,
        int position,
        int end,
        ReadOnlySpan<string> knownNames,
        List<LocaleDiagnostic> diagnostics,
        out int separator)
    {
        separator = 0;

        if (text[position] != ';')
        {
            return false;
        }

        var nameStart = position + 1;
        var spaced = nameStart < end && text[nameStart] == ' ';
        if (spaced)
        {
            nameStart++;
        }

        var nameEnd = nameStart;
        while (nameEnd < end && char.IsLetter(text[nameEnd]))
        {
            nameEnd++;
        }

        var colon = nameEnd;
        while (colon < end && text[colon] == ' ')
        {
            colon++;
        }

        if (nameEnd == nameStart || colon >= end || text[colon] != ':')
        {
            return false;
        }

        // Prose cannot be told from a typo without names to check against, so the exact separator stays required.
        if (knownNames.IsEmpty)
        {
            separator = Separator.Length;

            return spaced;
        }

        var name = new LocaleTextSpan(nameStart, nameEnd - nameStart);
        if (!Knows(text, name, knownNames))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.UnknownFieldName, name));

            return false;
        }

        if (!spaced)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MalformedFieldSeparator, new LocaleTextSpan(position, 1)));
        }

        separator = nameStart - position;

        return true;
    }

    private static LocaleField? ReadField(string text, LocaleTextSpan span, ReadOnlySpan<string> knownNames, List<LocaleDiagnostic> diagnostics)
    {
        var colon = span.IndexOf(text, ':');
        if (colon < 0)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingNameSeparator, span));

            return null;
        }

        var written = new LocaleTextSpan(span.Start, colon - span.Start);
        var name = Trim(text, written);
        if (name.IsEmpty)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.EmptyFieldName, span));

            return null;
        }

        if (!knownNames.IsEmpty && !Knows(text, name, knownNames))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.UnknownFieldName, name));
        }

        var raw = new LocaleTextSpan(colon + 1, span.End - colon - 1);
        var value = Trim(text, raw);

        // An empty value leaves nothing for the separator to introduce, so reporting its shape as well would only mislead.
        if (value.IsEmpty)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.EmptyFieldValue, span));
        }
        else if (name != written || value.Start != raw.Start + 1 || value.End != raw.End)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MalformedNameSeparator, written));
        }

        return new LocaleField(name, value);
    }

    private static void ReportDuplicates(string text, List<LocaleField> fields, List<LocaleDiagnostic> diagnostics)
    {
        for (var later = 1; later < fields.Count; later++)
        {
            for (var earlier = 0; earlier < later; earlier++)
            {
                if (fields[earlier].NameSpan.Slice(text).SequenceEqual(fields[later].NameSpan.Slice(text)))
                {
                    diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.DuplicateFieldName, fields[later].NameSpan));

                    break;
                }
            }
        }
    }

    private static bool Knows(string text, LocaleTextSpan name, ReadOnlySpan<string> knownNames)
    {
        foreach (var known in knownNames)
        {
            if (name.Slice(text).SequenceEqual(known))
            {
                return true;
            }
        }

        return false;
    }

    private static LocaleTextSpan Trim(string text, LocaleTextSpan span)
    {
        var start = span.Start;
        var end = span.End;

        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return new LocaleTextSpan(start, end - start);
    }
}
