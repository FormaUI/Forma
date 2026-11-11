namespace Forma.Framework.Composer.Documents;

using System.Text;

/// <summary>Lays HTML out one block per line, indented by nesting.</summary>
public static class HtmlSourceFormatter
{
    private static readonly HashSet<string> BlockTags =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "ul", "ol", "li", "hr", "div",
        "table", "thead", "tbody", "tfoot", "tr", "td", "th", "caption", "colgroup", "col",
        "figure", "figcaption", "details", "summary", "video", "audio", "source", "article", "section", "header", "footer", "nav", "aside",
    ];

    /// <summary>Tags whose content is kept exactly as written.</summary>
    private static readonly HashSet<string> LiteralTags = [with(StringComparer.OrdinalIgnoreCase), "pre", "textarea", "script", "style"];

    private const string Indent = "  ";

    /// <summary>Formats the markup one block per line, indented by nesting.</summary>
    /// <remarks>Inline content stays on its block's line, and the content of a literal tag such as <c>pre</c> is kept as written.</remarks>
    public static string Format(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var output = new StringBuilder(html.Length + html.Length / 4);
        var blocks = new Stack<bool>();
        var depth = 0;
        var atLineStart = true;
        var position = 0;

        while (position < html.Length)
        {
            var openIndex = html.IndexOf('<', position, StringComparison.Ordinal);

            if (openIndex < 0)
            {
                Write(html[position..]);
                break;
            }

            if (openIndex > position)
            {
                Write(html[position..openIndex]);
            }

            var close = html.IndexOf('>', openIndex, StringComparison.Ordinal);
            if (close < 0)
            {
                Write(html[openIndex..]);
                break;
            }

            var tag = html[openIndex..(close + 1)];
            var name = GetTagName(tag, out var isClosing, out var isSelfClosing);
            position = close + 1;

            if (!BlockTags.Contains(name))
            {
                Write(tag);
                continue;
            }

            if (isClosing)
            {
                depth = Math.Max(0, depth - 1);

                // A block that held blocks of its own closes on a line of its own; one that held only inline content closes on its line.
                if (blocks.Count > 0 && blocks.Pop())
                {
                    BreakLine();
                }

                Write(tag);
                BreakLine();
                continue;
            }

            BreakLine();
            Write(tag);
            MarkParent();

            if (LiteralTags.Contains(name))
            {
                var end = html.IndexOf($"</{name}", position, StringComparison.OrdinalIgnoreCase);
                var literal = end < 0 ? html[position..] : html[position..end];
                output.Append(literal);
                position += literal.Length;
                depth++;
                blocks.Push(false);
                atLineStart = false;
                continue;
            }

            if (isSelfClosing || name.Equals("hr", StringComparison.OrdinalIgnoreCase) || name.Equals("col", StringComparison.OrdinalIgnoreCase) || name.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                BreakLine();
                continue;
            }

            depth++;
            blocks.Push(false);
        }

        return output.ToString().TrimEnd();

        void MarkParent()
        {
            if (blocks.Count > 0)
            {
                blocks.Pop();
                blocks.Push(true);
            }
        }

        void Write(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            if (atLineStart)
            {
                var trimmedText = text.TrimStart();
                if (trimmedText.Length == 0)
                {
                    return;
                }

                for (var level = 0; level < depth; level++)
                {
                    output.Append(Indent);
                }

                text = trimmedText;
                atLineStart = false;
            }

            output.Append(text);
        }

        void BreakLine()
        {
            if (atLineStart)
            {
                return;
            }

            output.Append('\n');
            atLineStart = true;
        }
    }

    private static string GetTagName(string tag, out bool isClosing, out bool isSelfClosing)
    {
        var innerTag = tag.AsSpan(1, tag.Length - 2).Trim();
        isClosing = innerTag.Length > 0 && innerTag[0] == '/';
        isSelfClosing = innerTag.Length > 0 && innerTag[^1] == '/';

        if (isClosing)
        {
            innerTag = innerTag[1..].TrimStart();
        }

        var end = 0;
        while (end < innerTag.Length && !char.IsWhiteSpace(innerTag[end]) && innerTag[end] != '/')
        {
            end++;
        }

        return innerTag[..end].ToString();
    }
}
