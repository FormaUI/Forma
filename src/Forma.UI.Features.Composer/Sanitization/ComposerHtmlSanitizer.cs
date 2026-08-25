namespace Forma.UI.Features.Composer.Sanitization;

using System.Buffers;
using System.Text;

/// <summary>
/// Allowlist sanitizer covering the markup the built-in plugins produce, with no external dependency.
/// Scans the document as spans in a single pass: an element, attribute or CSS declaration the
/// <see cref="IHtmlMarkupPolicy"/> refuses is dropped, an element such as <c>script</c> or <c>style</c> is
/// removed together with its content, an event handler attribute and an unsafe URL scheme are rejected
/// outright, and every kept attribute value is re-escaped rather than passed through verbatim.
/// </summary>
public sealed class ComposerHtmlSanitizer : IComposerHtmlSanitizer
{
    // These are facts about HTML rather than choices a policy makes, so no host redefines them.

    /// <summary>Elements that carry no closing tag.</summary>
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "hr", "br", "source", "col", "wbr"
    };

    /// <summary>Attributes whose value is an address, checked before the attribute is kept.</summary>
    private static readonly HashSet<string> UrlAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "src", "poster"
    };

    /// <summary>The characters an attribute value may not carry unescaped.</summary>
    private static readonly SearchValues<char> EscapeTargets = SearchValues.Create("&\"<>");

    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> VoidTagLookup = VoidTags.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> UrlAttributeLookup = UrlAttributes.GetAlternateLookup<ReadOnlySpan<char>>();

    private readonly IHtmlMarkupPolicy _policy;

    /// <summary>Creates a sanitizer applying the markup policy the built-in plugins produce.</summary>
    public ComposerHtmlSanitizer()
        : this(ComposerHtmlMarkupPolicy.Default)
    {
    }

    /// <summary>Creates a sanitizer applying the given markup policy.</summary>
    public ComposerHtmlSanitizer(IHtmlMarkupPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;
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
    private int SanitizeMarkup(StringBuilder output, ReadOnlySpan<char> source, int open)
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
        if (!tag.IsClosing && !_policy.Allows(HtmlSubject.ElementContent, name, default, out _))
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

    private void AppendTag(StringBuilder output, ReadOnlySpan<char> source, HtmlTag tag)
    {
        var raw = source[tag.Name];

        if (!_policy.Allows(HtmlSubject.Element, raw, default, out var canonical))
        {
            return;
        }

        // A policy that normalizes nothing leaves the element written the way the document spelled it.
        var name = canonical is null ? raw : canonical.AsSpan();
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

    private void AppendAttributes(StringBuilder output, ReadOnlySpan<char> tagName, ReadOnlySpan<char> raw)
    {
        if (raw.IsWhiteSpace())
        {
            return;
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

            AppendAttribute(output, tagName, name, value);
        }
    }

    private void AppendAttribute(
        StringBuilder output,
        ReadOnlySpan<char> tagName,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> value)
    {
        // An event handler is script by definition, whatever the element and whatever a policy would say.
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_policy.Allows(HtmlSubject.Attribute, name, tagName, out var canonical))
        {
            return;
        }

        if (UrlAttributeLookup.Contains(name))
        {
            if (!_policy.Allows(HtmlSubject.Url, value, default, out _))
            {
                return;
            }
        }
        else if (name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            var checkpoint = output.Length;

            output.Append(' ');
            AppendName(output, name, canonical);
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
        AppendName(output, name, canonical);
        output.Append("=\"");
        AppendEscaped(output, value);
        output.Append('"');
    }

    // A policy that normalizes nothing leaves the name written the way the document spelled it.
    private static void AppendName(StringBuilder output, ReadOnlySpan<char> name, string? canonical)
    {
        if (canonical is null)
        {
            output.Append(name);
        }
        else
        {
            output.Append(canonical);
        }
    }

    private bool TryAppendStyle(StringBuilder output, ReadOnlySpan<char> style)
    {
        var wrote = false;

        foreach (var range in style.Split(';'))
        {
            var declaration = style[range].Trim();

            if (declaration.IsEmpty || !_policy.Allows(HtmlSubject.StyleDeclaration, declaration, default, out _))
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

            if (!_policy.Allows(HtmlSubject.StyleProperty, property, default, out var canonical))
            {
                continue;
            }

            if (wrote)
            {
                output.Append("; ");
            }

            AppendName(output, property, canonical);
            output.Append(": ");
            AppendEscaped(output, value);
            wrote = true;
        }

        return wrote;
    }

    private static void AppendEscaped(StringBuilder output, ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            var next = value.IndexOfAny(EscapeTargets);

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
