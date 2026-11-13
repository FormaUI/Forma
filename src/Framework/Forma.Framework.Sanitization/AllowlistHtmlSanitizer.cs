namespace Forma.Framework.Sanitization;

using System.Buffers;
using System.Net;
using System.Text;

/// <summary>An allowlist HTML sanitizer that applies an <see cref="IMarkupPolicy"/>.</summary>
/// <remarks>
/// Scans the document as spans in a single pass: an element, attribute or CSS declaration the policy
/// refuses is dropped, an element whose content the policy refuses, such as <c>script</c> or <c>style</c>,
/// is removed together with its content, an event handler attribute is always dropped, an address keeps
/// only a URL the policy permits, and every attribute value is read with its character references resolved,
/// as the browser reads it, judged as read and escaped once as it is kept.
/// </remarks>
public sealed class AllowlistHtmlSanitizer : IHtmlSanitizer
{
    /// <summary>The HTML void elements.</summary>
    private static readonly HashSet<string> VoidTags =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "img", "hr", "br", "source", "col", "wbr"
    ];

    /// <summary>Attributes whose value is an address.</summary>
    private static readonly HashSet<string> UrlAttributes =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "href", "src", "poster"
    ];

    /// <summary>The characters escaped in an attribute value.</summary>
    private static readonly SearchValues<char> EscapeTargets = SearchValues.Create("&\"<>");

    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> VoidTagLookup = VoidTags.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> UrlAttributeLookup = UrlAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <inheritdoc />
    public IMarkupPolicy MarkupPolicy { get; }

    /// <summary>Creates a sanitizer applying <see cref="RichContentMarkupPolicy.Default"/>.</summary>
    public AllowlistHtmlSanitizer()
        : this(RichContentMarkupPolicy.Default)
    {
    }

    /// <summary>Creates a sanitizer applying the given markup policy.</summary>
    public AllowlistHtmlSanitizer(IMarkupPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        MarkupPolicy = policy;
    }

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
            var openIndex = source[index..].IndexOf('<');

            if (openIndex < 0)
            {
                output.Append(source[index..]);
                break;
            }

            openIndex += index;
            output.Append(source[index..openIndex]);
            index = SanitizeMarkup(output, source, openIndex);
        }

        return output.ToString();
    }

    // Handles one '<' and returns where scanning resumes.
    private int SanitizeMarkup(StringBuilder output, ReadOnlySpan<char> source, int openIndex)
    {
        var remainingSource = source[openIndex..];

        if (remainingSource.StartsWith("<!--", StringComparison.Ordinal))
        {
            var end = remainingSource.IndexOf("-->", StringComparison.Ordinal);
            return end < 0 ? source.Length : openIndex + end + 3;
        }

        if (remainingSource.StartsWith("<!", StringComparison.Ordinal) || remainingSource.StartsWith("<?", StringComparison.Ordinal))
        {
            var end = remainingSource.IndexOf('>');
            return end < 0 ? source.Length : openIndex + end + 1;
        }

        if (!TryReadTag(source, openIndex, out var tag, out var afterTag))
        {
            output.Append('<');
            return openIndex + 1;
        }

        var name = source[tag.Name];

        if (!tag.IsClosing && !MarkupPolicy.Allows(HtmlSubject.ElementContent, name, default, out _))
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

        // A '>' inside a quoted value belongs to the value.
        while (position < source.Length)
        {
            var currentChar = source[position];

            if (quote != '\0')
            {
                if (currentChar == quote)
                {
                    quote = '\0';
                }
            }
            else if (currentChar is '"' or '\'')
            {
                quote = currentChar;
            }
            else if (currentChar == '>')
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
        var trimmedAttributes = attributes.TrimEnd();
        var isSelfClosing = trimmedAttributes.Length > 0 && trimmedAttributes[^1] == '/';
        var attributesEnd = attributesStart + (isSelfClosing ? trimmedAttributes.Length - 1 : attributes.Length);

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

    private void AppendTag(StringBuilder output, ReadOnlySpan<char> source, HtmlTag tag)
    {
        var rawName = source[tag.Name];

        if (!MarkupPolicy.Allows(HtmlSubject.Element, rawName, default, out var canonicalSpelling))
        {
            return;
        }

        var name = canonicalSpelling is null ? rawName : canonicalSpelling.AsSpan();
        var isVoid = VoidTagLookup.Contains(name);

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

    private void AppendAttributes(StringBuilder output, ReadOnlySpan<char> tagName, ReadOnlySpan<char> rawAttributes)
    {
        if (rawAttributes.IsWhiteSpace())
        {
            return;
        }

        var position = 0;

        while (position < rawAttributes.Length)
        {
            while (position < rawAttributes.Length && (char.IsWhiteSpace(rawAttributes[position]) || rawAttributes[position] == '/'))
            {
                position++;
            }

            var nameStart = position;

            while (position < rawAttributes.Length && (char.IsAsciiLetterOrDigit(rawAttributes[position]) || rawAttributes[position] is '-' or '_' or ':'))
            {
                position++;
            }

            if (position == nameStart)
            {
                position++;
                continue;
            }

            var name = rawAttributes[nameStart..position];

            while (position < rawAttributes.Length && char.IsWhiteSpace(rawAttributes[position]))
            {
                position++;
            }

            var value = ReadOnlySpan<char>.Empty;

            if (position < rawAttributes.Length && rawAttributes[position] == '=')
            {
                position++;

                while (position < rawAttributes.Length && char.IsWhiteSpace(rawAttributes[position]))
                {
                    position++;
                }

                if (position < rawAttributes.Length && rawAttributes[position] is '"' or '\'')
                {
                    var quote = rawAttributes[position++];
                    var valueStart = position;

                    while (position < rawAttributes.Length && rawAttributes[position] != quote)
                    {
                        position++;
                    }

                    value = rawAttributes[valueStart..position];

                    if (position < rawAttributes.Length)
                    {
                        position++;
                    }
                }
                else
                {
                    var valueStart = position;

                    while (position < rawAttributes.Length && !char.IsWhiteSpace(rawAttributes[position]))
                    {
                        position++;
                    }

                    value = rawAttributes[valueStart..position];
                }
            }

            AppendAttribute(output, tagName, name, value);
        }
    }

    private void AppendAttribute(
        StringBuilder output,
        ReadOnlySpan<char> tagName,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> value)
    {
        // An event handler attribute is dropped whatever the policy permits.
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!MarkupPolicy.Allows(HtmlSubject.Attribute, name, tagName, out var canonicalSpelling))
        {
            return;
        }

        // The policy judges the value the browser acts on, its character references resolved, and it is escaped once as it is kept, so
        // a second pass over what the first kept changes nothing.
        if (value.Contains('&'))
        {
            value = WebUtility.HtmlDecode(value.ToString());
        }

        if (UrlAttributeLookup.Contains(name))
        {
            if (!MarkupPolicy.Allows(HtmlSubject.Url, value, default, out _))
            {
                return;
            }
        }
        else if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            var checkpoint = output.Length;

            output.Append(' ');
            AppendName(output, name, canonicalSpelling);
            output.Append("=\"");

            if (!TryAppendStyle(output, value))
            {
                output.Length = checkpoint;
                return;
            }

            output.Append('"');
            return;
        }

        output.Append(' ');
        AppendName(output, name, canonicalSpelling);
        output.Append("=\"");
        AppendEscaped(output, value);
        output.Append('"');
    }

    // A null canonical name keeps the document's spelling.
    private static void AppendName(StringBuilder output, ReadOnlySpan<char> name, string? canonicalSpelling)
    {
        if (canonicalSpelling is null)
        {
            output.Append(name);
        }
        else
        {
            output.Append(canonicalSpelling);
        }
    }

    private bool TryAppendStyle(StringBuilder output, ReadOnlySpan<char> style)
    {
        var hasWritten = false;

        foreach (var range in style.Split(';'))
        {
            var declaration = style[range].Trim();

            if (declaration.IsEmpty || !MarkupPolicy.Allows(HtmlSubject.StyleDeclaration, declaration, default, out _))
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

            if (!MarkupPolicy.Allows(HtmlSubject.StyleProperty, property, default, out var canonicalSpelling))
            {
                continue;
            }

            if (hasWritten)
            {
                output.Append("; ");
            }

            // A property kept in the document's spelling is escaped as its value is, since a resolved reference can put a quote in it.
            if (canonicalSpelling is null)
            {
                AppendEscaped(output, property);
            }
            else
            {
                output.Append(canonicalSpelling);
            }

            output.Append(": ");
            AppendEscaped(output, value);
            hasWritten = true;
        }

        return hasWritten;
    }

    private static void AppendEscaped(StringBuilder output, ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            var nextEscapeIndex = value.IndexOfAny(EscapeTargets);

            if (nextEscapeIndex < 0)
            {
                output.Append(value);
                return;
            }

            output.Append(value[..nextEscapeIndex]);

            output.Append(value[nextEscapeIndex] switch
            {
                '&' => "&amp;",
                '"' => "&quot;",
                '<' => "&lt;",
                _ => "&gt;"
            });

            value = value[(nextEscapeIndex + 1)..];
        }
    }

    private readonly record struct HtmlTag(bool IsClosing, Range Name, Range Attributes, bool IsSelfClosing);
}
