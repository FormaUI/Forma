namespace Forma.UI.Features.Composer.Sanitization;

using System.Text;

/// <summary>
/// Allowlist sanitizer covering the markup the built-in plugins produce, with no external dependency.
/// Scans the document as spans in a single pass: an element, attribute or CSS declaration absent from
/// <see cref="HtmlAllowList"/> is dropped, a discarded element such as <c>script</c> or <c>style</c> is
/// removed together with its content, an event handler attribute and an unsafe URL scheme are rejected
/// outright, and every kept attribute value is re-escaped rather than passed through verbatim.
/// </summary>
public sealed class DefaultHtmlSanitizer : IComposerHtmlSanitizer
{
    /// <inheritdoc />
    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var source = html.AsSpan();
        var output = new StringBuilder(html.Length);
        var index = 0;

        while (index < source.Length)
        {
            var open = source[index..].IndexOf('<');

            if (open < 0)
            {
                output.Append(source[index..]);
                break;
            }

            open += index;
            output.Append(source[index..open]);
            index = SanitizeMarkup(output, source, open);
        }

        return output.ToString();
    }

    // Handles one '<' and returns where scanning resumes.
    private static int SanitizeMarkup(StringBuilder output, ReadOnlySpan<char> source, int open)
    {
        var rest = source[open..];

        if (rest.StartsWith("<!--", StringComparison.Ordinal))
        {
            var end = rest.IndexOf("-->", StringComparison.Ordinal);
            return end < 0 ? source.Length : open + end + 3;
        }

        if (rest.StartsWith("<!", StringComparison.Ordinal) || rest.StartsWith("<?", StringComparison.Ordinal))
        {
            var end = rest.IndexOf('>');
            return end < 0 ? source.Length : open + end + 1;
        }

        if (!TryReadTag(source, open, out var tag, out var afterTag))
        {
            output.Append('<');
            return open + 1;
        }

        var name = source[tag.Name];

        // Dropping only the tag would leave a script body or a style block behind as text.
        if (!tag.IsClosing && HtmlAllowList.DiscardedLookup.Contains(name))
        {
            return SkipElementContent(source, afterTag, name);
        }

        AppendTag(output, source, tag);
        return afterTag;
    }

    private static bool TryReadTag(ReadOnlySpan<char> source, int start, out HtmlTag tag, out int afterTag)
    {
        tag = default;
        afterTag = start;

        var position = start + 1;

        if (position >= source.Length)
        {
            return false;
        }

        var isClosing = source[position] == '/';

        if (isClosing)
        {
            position++;
        }

        var nameStart = position;

        while (position < source.Length && (char.IsAsciiLetterOrDigit(source[position]) || source[position] == '-'))
        {
            position++;
        }

        if (position == nameStart)
        {
            return false;
        }

        var nameRange = nameStart..position;
        var attributesStart = position;
        var quote = '\0';

        // A '>' inside a quoted value does not end the tag, so the scan tracks the quote it is in.
        while (position < source.Length)
        {
            var current = source[position];

            if (quote != '\0')
            {
                if (current == quote)
                {
                    quote = '\0';
                }
            }
            else if (current is '"' or '\'')
            {
                quote = current;
            }
            else if (current == '>')
            {
                break;
            }

            position++;
        }

        if (position >= source.Length)
        {
            return false;
        }

        var attributes = source[attributesStart..position];
        var trimmed = attributes.TrimEnd();
        var isSelfClosing = trimmed.Length > 0 && trimmed[^1] == '/';
        var attributesEnd = attributesStart + (isSelfClosing ? trimmed.Length - 1 : attributes.Length);

        tag = new HtmlTag(isClosing, nameRange, attributesStart..attributesEnd, isSelfClosing);
        afterTag = position + 1;
        return true;
    }

    private static int SkipElementContent(ReadOnlySpan<char> source, int from, ReadOnlySpan<char> name)
    {
        var index = from;

        while (index < source.Length)
        {
            var marker = source[index..].IndexOf("</", StringComparison.Ordinal);

            if (marker < 0)
            {
                return source.Length;
            }

            var nameStart = index + marker + 2;
            var cursor = nameStart;

            while (cursor < source.Length && (char.IsAsciiLetterOrDigit(source[cursor]) || source[cursor] == '-'))
            {
                cursor++;
            }

            if (source[nameStart..cursor].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                var end = source[cursor..].IndexOf('>');
                return end < 0 ? source.Length : cursor + end + 1;
            }

            index = nameStart;
        }

        return source.Length;
    }

    private static void AppendTag(StringBuilder output, ReadOnlySpan<char> source, HtmlTag tag)
    {
        if (!HtmlAllowList.TagLookup.TryGetValue(source[tag.Name], out var name))
        {
            return;
        }

        var isVoid = HtmlAllowList.VoidTagLookup.Contains(name);

        if (tag.IsClosing)
        {
            if (!isVoid)
            {
                output.Append("</").Append(name).Append('>');
            }

            return;
        }

        output.Append('<').Append(name);
        AppendAttributes(output, name, source[tag.Attributes]);
        output.Append(tag.IsSelfClosing || isVoid ? " />" : ">");
    }

    private static void AppendAttributes(StringBuilder output, string tagName, ReadOnlySpan<char> raw)
    {
        if (raw.IsWhiteSpace())
        {
            return;
        }

        HashSet<string>.AlternateLookup<ReadOnlySpan<char>>? tagSpecific = null;

        if (HtmlAllowList.TagAttributeLookup.TryGetValue(tagName, out var set))
        {
            tagSpecific = set.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        var position = 0;

        while (position < raw.Length)
        {
            while (position < raw.Length && (char.IsWhiteSpace(raw[position]) || raw[position] == '/'))
            {
                position++;
            }

            var nameStart = position;

            while (position < raw.Length && (char.IsAsciiLetterOrDigit(raw[position]) || raw[position] is '-' or '_' or ':'))
            {
                position++;
            }

            if (position == nameStart)
            {
                position++;
                continue;
            }

            var name = raw[nameStart..position];

            while (position < raw.Length && char.IsWhiteSpace(raw[position]))
            {
                position++;
            }

            var value = ReadOnlySpan<char>.Empty;

            if (position < raw.Length && raw[position] == '=')
            {
                position++;

                while (position < raw.Length && char.IsWhiteSpace(raw[position]))
                {
                    position++;
                }

                if (position < raw.Length && raw[position] is '"' or '\'')
                {
                    var quote = raw[position++];
                    var valueStart = position;

                    while (position < raw.Length && raw[position] != quote)
                    {
                        position++;
                    }

                    value = raw[valueStart..position];

                    if (position < raw.Length)
                    {
                        position++;
                    }
                }
                else
                {
                    var valueStart = position;

                    while (position < raw.Length && !char.IsWhiteSpace(raw[position]))
                    {
                        position++;
                    }

                    value = raw[valueStart..position];
                }
            }

            AppendAttribute(output, name, value, tagSpecific);
        }
    }

    private static void AppendAttribute(
        StringBuilder output,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> value,
        HashSet<string>.AlternateLookup<ReadOnlySpan<char>>? tagSpecific)
    {
        // An event handler is script by definition, whatever the element.
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var isData = false;

        if (!HtmlAllowList.GlobalAttributeLookup.TryGetValue(name, out var canonical)
            && !(tagSpecific?.TryGetValue(name, out canonical) ?? false))
        {
            if (!name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            isData = true;
        }

        if (HtmlAllowList.UrlAttributeLookup.Contains(name))
        {
            if (!IsSafeUrl(value))
            {
                return;
            }
        }
        else if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            var checkpoint = output.Length;

            output.Append(' ').Append(canonical!).Append("=\"");

            if (!TryAppendStyle(output, value))
            {
                output.Length = checkpoint;
                return;
            }

            output.Append('"');
            return;
        }

        output.Append(' ');

        if (isData)
        {
            output.Append(name);
        }
        else
        {
            output.Append(canonical!);
        }

        output.Append("=\"");
        AppendEscaped(output, value);
        output.Append('"');
    }

    private static bool IsSafeUrl(ReadOnlySpan<char> url)
    {
        var trimmed = url.Trim();

        if (trimmed.IsEmpty)
        {
            return false;
        }

        if (trimmed[0] is '#' or '/'
            || trimmed.StartsWith("./", StringComparison.Ordinal)
            || trimmed.StartsWith("../", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var scheme in HtmlAllowList.UrlSchemes)
        {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var prefix in HtmlAllowList.DataUrlPrefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryAppendStyle(StringBuilder output, ReadOnlySpan<char> style)
    {
        var wrote = false;

        foreach (var range in style.Split(';'))
        {
            var declaration = style[range].Trim();

            if (declaration.IsEmpty || IsDeniedDeclaration(declaration))
            {
                continue;
            }

            var colon = declaration.IndexOf(':');

            if (colon <= 0)
            {
                continue;
            }

            var property = declaration[..colon].TrimEnd();
            var value = declaration[(colon + 1)..].Trim();

            if (!HtmlAllowList.StylePropertyLookup.TryGetValue(property, out var canonical)
                && !property.StartsWith("--forma-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (wrote)
            {
                output.Append("; ");
            }

            if (canonical is null)
            {
                output.Append(property);
            }
            else
            {
                output.Append(canonical);
            }

            output.Append(": ");
            AppendEscaped(output, value);
            wrote = true;
        }

        return wrote;
    }

    private static bool IsDeniedDeclaration(ReadOnlySpan<char> declaration)
    {
        foreach (var denied in HtmlAllowList.StyleDenyList)
        {
            if (declaration.Contains(denied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendEscaped(StringBuilder output, ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            var next = value.IndexOfAny(HtmlAllowList.EscapeTargets);

            if (next < 0)
            {
                output.Append(value);
                return;
            }

            output.Append(value[..next]);

            output.Append(value[next] switch
            {
                '&' => "&amp;",
                '"' => "&quot;",
                '<' => "&lt;",
                _ => "&gt;"
            });

            value = value[(next + 1)..];
        }
    }

    private readonly record struct HtmlTag(bool IsClosing, Range Name, Range Attributes, bool IsSelfClosing);
}
