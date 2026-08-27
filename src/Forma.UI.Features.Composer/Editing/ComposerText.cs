namespace Forma.UI.Features.Composer.Editing;

/// <summary>The text operations every editor needs against a document's raw content.</summary>
public static class ComposerText
{
    /// <summary>Returns the run of word characters touching <paramref name="offset"/>, or an empty span when none surrounds it.</summary>
    public static ComposerTextSpan WordAt(string content, int offset)
    {
        ArgumentNullException.ThrowIfNull(content);

        var caret = Math.Clamp(offset, 0, content.Length);
        var start = caret;
        var end = caret;

        while (start > 0 && IsWordChar(content[start - 1]))
        {
            start--;
        }

        while (end < content.Length && IsWordChar(content[end]))
        {
            end++;
        }

        return new ComposerTextSpan(start, end, content[start..end]);
    }

    /// <summary>Returns the text a selection covers, ordered regardless of which end is the anchor.</summary>
    public static ComposerTextSpan Selected(string content, ComposerSelection selection)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(selection);

        var (start, end) = OrderedRange(content, selection.Start, selection.End);

        return new ComposerTextSpan(start, end, content[start..end]);
    }

    /// <summary>Returns the span of every line a selection touches, including the line a collapsed caret sits on.</summary>
    public static IReadOnlyList<ComposerTextSpan> Lines(string content, ComposerSelection selection)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(selection);

        var (start, end) = OrderedRange(content, selection.Start, selection.End);
        var spans = new List<ComposerTextSpan>();
        var lineStart = 0;

        for (var i = 0; i <= content.Length; i++)
        {
            if (i != content.Length && content[i] != '\n')
            {
                continue;
            }

            if (lineStart <= end && i >= start)
            {
                spans.Add(new ComposerTextSpan(lineStart, i, content[lineStart..i]));
            }

            lineStart = i + 1;
        }

        return spans;
    }

    /// <summary>Replaces the text between two offsets, placing the caret immediately after the replacement.</summary>
    public static ComposerEdit Replace(string content, int start, int end, string replacement)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(replacement);

        var (from, to) = OrderedRange(content, start, end);
        var newContent = string.Concat(content.AsSpan(0, from), replacement, content.AsSpan(to));
        var caret = from + replacement.Length;

        return new ComposerEdit(newContent, new ComposerSelection { Start = caret, End = caret });
    }

    /// <summary>Replaces every occurrence of a value, leaving the caret at the end of the document.</summary>
    public static ComposerEdit ReplaceAll(string content, string oldValue, string newValue, StringComparison comparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(oldValue);
        ArgumentNullException.ThrowIfNull(newValue);

        var newContent = content.Replace(oldValue, newValue, comparison);
        var caret = newContent.Length;

        return new ComposerEdit(newContent, new ComposerSelection { Start = caret, End = caret });
    }

    private static bool IsWordChar(char c)
        => char.IsLetterOrDigit(c) || c == '_';

    private static (int Start, int End) OrderedRange(string content, int start, int end)
    {
        var from = Math.Clamp(Math.Min(start, end), 0, content.Length);
        var to = Math.Clamp(Math.Max(start, end), from, content.Length);

        return (from, to);
    }
}
