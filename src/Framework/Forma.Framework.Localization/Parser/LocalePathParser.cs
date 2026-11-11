namespace Forma.Framework.Localization.Parser;

using System.Text;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the paths that say where a locale file sits, written with tokens, such as <c>$RootDir/$AnchorName.$Culture$Extension</c>.</summary>
public static class LocalePathParser
{
    /// <summary>The character that starts a token, and escapes itself when doubled.</summary>
    public const char TokenMarker = '$';

    private static readonly Dictionary<string, LocalePathToken>.AlternateLookup<ReadOnlySpan<char>> TokensByName =
        Enum.GetValues<LocalePathToken>().ToDictionary(token => token.ToString(), StringComparer.Ordinal).GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>Splits a path into its runs, with a diagnostic for each problem found.</summary>
    /// <param name="text">The path as written.</param>
    public static LocalePathResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parts = new List<LocalePathPart>();
        var diagnostics = new List<LocaleDiagnostic>();

        if (string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.EmptyPath, new LocaleTextSpan(0, text.Length)));

            return new LocalePathResult(parts, diagnostics);
        }

        var literal = new StringBuilder();
        var literalStart = 0;
        var position = 0;

        while (position < text.Length)
        {
            if (text[position] != TokenMarker)
            {
                literal.Append(text[position]);
                position++;
                continue;
            }

            if (position + 1 < text.Length && text[position + 1] == TokenMarker)
            {
                literal.Append(TokenMarker);
                position += 2;
                continue;
            }

            AddLiteral(parts, literal, literalStart, position);

            var nameEnd = position + 1;
            while (nameEnd < text.Length && char.IsAsciiLetter(text[nameEnd]))
            {
                nameEnd++;
            }

            var span = new LocaleTextSpan(position, nameEnd - position);
            if (TokensByName.TryGetValue(text.AsSpan(position + 1, nameEnd - position - 1), out var token))
            {
                parts.Add(new LocalePathPart(token, string.Empty, span));
            }
            else
            {
                diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.UnknownPathToken, span));
            }

            position = nameEnd;
            literalStart = nameEnd;
        }

        AddLiteral(parts, literal, literalStart, position);
        CheckTokens(text, parts, diagnostics);

        return new LocalePathResult(parts, diagnostics);
    }

    private static void AddLiteral(List<LocalePathPart> parts, StringBuilder literal, int start, int end)
    {
        if (literal.Length == 0)
        {
            return;
        }

        parts.Add(new LocalePathPart(null, literal.ToString(), new LocaleTextSpan(start, end - start)));
        literal.Clear();
    }

    // A path names each token once, names its culture and extension, and spells its whole anchor or none of it.
    private static void CheckTokens(string text, List<LocalePathPart> parts, List<LocaleDiagnostic> diagnostics)
    {
        var seenTokens = new HashSet<LocalePathToken>();
        foreach (var part in parts)
        {
            if (part.Token is { } token && !seenTokens.Add(token))
            {
                diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.DuplicatePathToken, part.Span));
            }
        }

        var wholeSpan = new LocaleTextSpan(0, text.Length);

        if (!seenTokens.Contains(LocalePathToken.Culture) || !seenTokens.Contains(LocalePathToken.Extension))
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.MissingPathToken, wholeSpan));
        }

        var hasAnchorName = seenTokens.Contains(LocalePathToken.AnchorName);
        var hasParentName = seenTokens.Contains(LocalePathToken.ParentName);
        var hasCurrentName = seenTokens.Contains(LocalePathToken.CurrentName);

        if (hasAnchorName && hasParentName)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.ConflictingPathAnchor, wholeSpan));
        }
        else if (!hasAnchorName && hasParentName != hasCurrentName)
        {
            diagnostics.Add(new LocaleDiagnostic(LocaleDiagnosticKind.IncompletePathAnchor, wholeSpan));
        }
    }
}
